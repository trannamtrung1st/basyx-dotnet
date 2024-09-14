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
using BaSyx.Servers.Http;
using System.Collections.Generic;
using BaSyx.Registry.Client.Http;
using System.Linq;
using BaSyx.Models.AdminShell;
using BaSyx.API.ServiceProvider;
using Microsoft.Extensions.DependencyInjection;
using BaSyx.Aas.Server.Services.Abstracts;
using BaSyx.Aas.Server.Services;

namespace BaSyx.Aas.Server;

class Program
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private static AppDataService AppDataService { get; set; }
    private static AasHttpServer httpServer;
    private static AssetAdministrationShellRepositoryServiceProvider aasRepository;
    private static SubmodelRepositoryServiceProvider submodelRepository;
    private static ServerSettings serverSettings;
    private static RegistryClientSettings registryClientSettings;
    private static RegistryHttpClient registryHttpClient;

    static void Main(string[] args)
    {
        _logger.Info("Starting AAS Server...");

        AppDataService = AppDataService.Create("aas", "appsettings.json", args);

        //Loading server configurations settings from ServerSettings.xml;
        serverSettings = AppDataService.GetSettings<ServerSettings>();
        registryClientSettings = AppDataService.GetSettings<RegistryClientSettings>();

        registryHttpClient = new RegistryHttpClient(registryClientSettings);
        httpServer = new AasHttpServer(serverSettings, args);
        httpServer.WebHostBuilder.UseNLog();
        httpServer.ConfigureServices(services =>
        {
            services.AddMemoryCache();
            services.AddSingleton<ICachedFileService, CachedFileService>();
        });

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
        httpServer.AddSwagger(Interface.All, scanAssemblies: [typeof(Program).Assembly]);

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
        };

        httpServer.ConfigureAllServices(
            aasServiceProvider: null,
            aasRepoServiceProvider: aasRepository,
            submodelServiceProvider: null,
            submodelRepoServiceProvider: submodelRepository,
            aasRegistryProvider: registryImpl,
            smRegistryProvider: registryImpl
        );

        var host = httpServer.Build();

        CachedFileService.Global = host.Services.GetRequiredService<ICachedFileService>();

        host.Run();
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

            aas.Submodels.Create(new Submodel("TestSubmodel", new BaSyxSubmodelIdentifier($"TestSubmodel {i}", "1.0.0"))
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
}
