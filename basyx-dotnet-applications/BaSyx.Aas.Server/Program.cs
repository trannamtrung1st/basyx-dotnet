/*******************************************************************************
* Copyright (c) 2024 Bosch Rexroth AG
* Author: Constantin Ziesche (constantin.ziesche@bosch.com)
*
* This program and the accompanying materials are made available under the
* terms of the MIT License which is available at
* https://github.com/eclipse-basyx/basyx-dotnet/blob/main/LICENSE
*
* SPDX-License-Identifier: MIT
*******************************************************************************/
using BaSyx.Discovery.mDNS;
using BaSyx.Utils.Settings;
using NLog;
using BaSyx.Common.UI;
using BaSyx.Common.UI.Swagger;
using NLog.Web;
using Microsoft.AspNetCore.Hosting;
using System.Security.Cryptography.X509Certificates;
using BaSyx.Deployment.AppDataService;
using BaSyx.Registry.ReferenceImpl.InMemory;
using System.IO;
using BaSyx.Servers.Http;
using System.Collections.Generic;
using BaSyx.Models.Connectivity;
using BaSyx.Registry.Client.Http;
using CommandLine;
using System;
using System.Linq;
using System.Threading.Tasks;
using BaSyx.Models.Export;
using System.IO.Packaging;
using BaSyx.Models.AdminShell;
using BaSyx.API.ServiceProvider;
using System.Collections.Concurrent;

namespace BaSyx.Aas.Server;

class Program
{
    private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

    private static AppDataService AppDataService { get; set; }
    private static FileSystemWatcher watcher;
    private static AasHttpServer httpServer;
    private static AssetAdministrationShellRepositoryServiceProvider aasRepository;
    private static SubmodelRepositoryServiceProvider submodelRepository;
    private static ServerSettings serverSettings;
    private static RegistryClientSettings registryClientSettings;
    private static RegistryHttpClient registryHttpClient;

    public class Options
    {
        [Option('i', "input", Required = false, HelpText = "Path to AASX-File or Folder")]
        public string InputPath { get; set; }
    }

    static void Main(string[] args)
    {
        logger.Info("Starting AAS Server...");

        string[] inputFiles = null;

        Parser.Default.ParseArguments<Options>(args)
            .WithParsed(o =>
            {
                if (!string.IsNullOrEmpty(o.InputPath))
                {
                    if (Directory.Exists(o.InputPath))
                    {
                        inputFiles = Directory.GetFiles(o.InputPath, "*.aasx");

                        watcher = new FileSystemWatcher(o.InputPath, "*.aasx");
                        watcher.EnableRaisingEvents = true;
                        watcher.Changed += Watcher_Changed;
                    }
                    else if (File.Exists(o.InputPath))
                    {
                        inputFiles = new string[] { o.InputPath };
                    }
                    else
                        throw new FileNotFoundException(o.InputPath);
                }
                else if (args.Length > 0)
                {
                    if (File.Exists(args[0]))
                        inputFiles = new string[] { args[0] };
                    else if (Directory.Exists(args[0]))
                        inputFiles = Directory.GetFiles(args[0]);
                }

            });

        if (args.Contains("--help") || args.Contains("--version"))
            return;

        AppDataService = AppDataService.Create("aas", "appsettings.json", args);

        //Loading server configurations settings from ServerSettings.xml;
        serverSettings = AppDataService.GetSettings<ServerSettings>();
        registryClientSettings = AppDataService.GetSettings<RegistryClientSettings>();

        registryHttpClient = new RegistryHttpClient(registryClientSettings);
        httpServer = new AasHttpServer(serverSettings, args);
        httpServer.WebHostBuilder.UseNLog();

        //Configure the pathbase as default prefix for all routes
        if (!string.IsNullOrEmpty(serverSettings.ServerConfig.PathBase))
            httpServer.UsePathBase(serverSettings.ServerConfig.PathBase);

        //Check if ServerCertificate is present
        if (!string.IsNullOrEmpty(serverSettings.ServerConfig.Security.ServerCertificatePath))
        {
            httpServer.WebHostBuilder.ConfigureKestrel(serverOptions =>
            {
                serverOptions.ConfigureHttpsDefaults(listenOptions =>
                {
                    X509Certificate2 certificate = new X509Certificate2(
                        serverSettings.ServerConfig.Security.ServerCertificatePath,
                        serverSettings.ServerConfig.Security.ServerCertificatePassword);
                    listenOptions.ServerCertificate = certificate;
                });
            });
        }

        InitializeData(out var shells);
        aasRepository = new AssetAdministrationShellRepositoryServiceProvider();
        aasRepository.BindTo(shells);
        aasRepository.UseAutoEndpointRegistration(serverSettings.ServerConfig);

        submodelRepository = new SubmodelRepositoryServiceProvider();
        var submodelProviders = aasRepository.GetAssetAdministrationShellServiceProviders()
            .Entity.OfType<ISubmodelServiceProviderRegistry>()
            .SelectMany(a => a.GetSubmodelServiceProviders().Entity)
            .ToList();
        submodelProviders.ForEach(p => submodelRepository.RegisterSubmodelServiceProvider(p.ServiceDescriptor.Id, p));
        submodelRepository.UseAutoEndpointRegistration(serverSettings.ServerConfig);

        httpServer.AddBaSyxUI(PageNames.AssetAdministrationShellRepositoryServer);
        httpServer.AddSwagger(Interface.All);

        for (int i = 0; i < inputFiles?.Length; i++)
        {
            LoadAASX(inputFiles[i]);
        }

        var shellDesrciptors = aasRepository.ServiceDescriptor.AssetAdministrationShellDescriptors;
        var registryImpl = new InMemoryRegistry(shellDesrciptors);
        httpServer.ApplicationStarted = () =>
        {
            if (serverSettings.DiscoveryConfig.AutoDiscovery)
            {
                registryImpl.StartDiscovery();
            }
        };

        httpServer.ApplicationStopping = () =>
        {
            if (serverSettings.DiscoveryConfig.AutoDiscovery)
            {
                registryImpl.StopDiscovery();
            }

            if (serverSettings.Miscellaneous.TryGetValue("AutoRegister", out string value) && value == "true")
            {
                var providers = aasRepository.GetAssetAdministrationShellServiceProviders().Entity;
                foreach (var shellProvider in providers)
                {
                    var result = registryHttpClient
                        .DeleteAssetAdministrationShellRegistration(shellProvider.ServiceDescriptor.Id.Id);

                    logger.Info($"Success: {result.Success} | Messages: {result.Messages.ToString()}");
                }
            }
        };

        httpServer.ConfigureAllServices(
            aasServiceProvider: null,
            aasRepoServiceProvider: aasRepository,
            submodelServiceProvider: null,
            submodelRepoServiceProvider: submodelRepository,
            aasRegistryProvider: registryImpl,
            smRegistryProvider: registryImpl
        );

        httpServer.Run();
    }

    private static void InitializeData(out List<AssetAdministrationShell> shells)
    {
        shells = [];
        for (int i = 0; i < 3; i++)
        {
            AssetAdministrationShell aas = new AssetAdministrationShell("MultiAAS_" + i, new BaSyxShellIdentifier("MultiAAS_" + i, "1.0.0"))
            {
                Description = new LangStringSet()
                    {
                       new LangString("de", i + ". VWS"),
                       new LangString("en", i + ". AAS")
                    },
                Administration = new AdministrativeInformation()
                {
                    Version = "1.0",
                    Revision = "120"
                },
                AssetInformation = new AssetInformation()
                {
                    AssetKind = AssetKind.Instance,
                    GlobalAssetId = new BaSyxAssetIdentifier("Asset_" + i, "1.0.0")
                }
            };

            aas.Submodels.Create(new Submodel("TestSubmodel", new BaSyxSubmodelIdentifier("TestSubmodel", "1.0.0"))
            {
                SubmodelElements =
                    {
                        new Property<string>("Property_" + i, "TestValue_" + i ),
                        new SubmodelElementCollection("Coll_" + i)
                        {
                            Value =
                            {
                                Value =
                                {
                                    new Property<string>("SubProperty_" + i, "TestSubValue_" + i)
                                }
                            }
                        }
                    }
            });
            shells.Add(aas);
        }
    }

    private static async void Watcher_Changed(object sender, FileSystemEventArgs e)
    {
        await Task.Delay(1000);
        LoadAASX(e.FullPath);
    }

    private static void LoadAASX(string aasxFilePath)
    {
        using (AASX_V2_0 aasx = new AASX_V2_0(aasxFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            AssetAdministrationShellEnvironment_V2_0 environment = aasx.GetEnvironment_V2_0();
            if (environment == null)
            {
                logger.Error("Asset Administration Shell Environment cannot be obtained from AASX-Package " + aasxFilePath);
                return;
            }

            logger.Info("AASX-Package successfully loaded");

            if (environment.AssetAdministrationShells.Count != 0)
            {
                PackagePart thumbnailPart = aasx.GetThumbnailAsPackagePart();
                AddToAssetAdministrationShellRepository(environment.AssetAdministrationShells, aasx.SupplementaryFiles, thumbnailPart);
            }
            else
            {
                logger.Error("No Asset Administration Shells found AASX-Package " + aasxFilePath);
                return;
            }
        }
    }

    private static void AddToAssetAdministrationShellRepository(List<IAssetAdministrationShell> assetAdministrationShells, List<PackagePart> supplementaryFiles, PackagePart thumbnailPart)
    {
        foreach (var inputShell in assetAdministrationShells)
        {
            var shell = aasRepository.CreateAssetAdministrationShell(inputShell).Entity;
            var aasProvider = aasRepository.GetAssetAdministrationShellServiceProvider(shell.Id).Entity;

            if (serverSettings.Miscellaneous.TryGetValue("AutoRegister", out string value) && value == "true")
            {
                var result = registryHttpClient.CreateAssetAdministrationShellRegistration(aasProvider.ServiceDescriptor);

                logger.Info($"Success: {result.Success} | Messages: {result.Messages.ToString()}");
            }
        }

        string aasIdName = assetAdministrationShells.First().Id;
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            aasIdName = aasIdName.Replace(invalidChar, '_');

        foreach (var file in supplementaryFiles)
        {
            using (Stream stream = file.GetStream())
            {
                Uri fileUri = new Uri(aasIdName + "/" + file.Uri.ToString().TrimStart('/'), UriKind.Relative);
                logger.Info("Providing content on server: " + fileUri);
                httpServer.ProvideContent(fileUri, stream);
            }
        }
        if (thumbnailPart != null)
            using (Stream thumbnailStream = thumbnailPart.GetStream())
            {
                Uri thumbnailUri = new Uri(aasIdName + "/" + thumbnailPart.Uri.ToString().TrimStart('/'), UriKind.Relative);
                httpServer.ProvideContent(thumbnailUri, thumbnailStream);
            }
    }
}
