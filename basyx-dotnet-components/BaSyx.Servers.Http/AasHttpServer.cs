using BaSyx.API.Http.Controllers;
using BaSyx.API.Interfaces;
using BaSyx.API.ServiceProvider;
using BaSyx.Components.Common;
using BaSyx.Models.Connectivity;
using BaSyx.Utils.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BaSyx.Servers.Http
{
    public class AasHttpServer : ServerApplication
    {
        public AasHttpServer() : this(null, null) { }
        public AasHttpServer(ServerSettings serverSettings) : this(serverSettings, null) { }
        public AasHttpServer(ServerSettings serverSettings, string[] webHostBuilderArgs)
            : base(serverSettings, webHostBuilderArgs)
        {
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            WebHostBuilder.UseSetting(WebHostDefaults.ApplicationKey, entryAssembly.FullName);
        }

        public void ConfigureAllServices(
            IAssetAdministrationShellServiceProvider aasServiceProvider = null,
            IAssetAdministrationShellRepositoryServiceProvider aasRepoServiceProvider = null,
            ISubmodelServiceProvider submodelServiceProvider = null,
            ISubmodelRepositoryServiceProvider submodelRepoServiceProvider = null,
            IAssetAdministrationShellRegistryInterface aasRegistryProvider = null,
            ISubmodelRegistryInterface smRegistryProvider = null)
        {
            WebHostBuilder.ConfigureServices(services =>
            {
                var controllerConvention = new ControllerConvention(this)
                        .Include(typeof(DescriptionController));

                if (aasServiceProvider != null)
                {
                    services.AddSingleton<IServiceProvider>(aasServiceProvider)
                            .AddSingleton<IServiceDescriptor>(aasServiceProvider.ServiceDescriptor)
                            .AddSingleton(aasServiceProvider);
                    controllerConvention = controllerConvention.Include(typeof(AssetAdministrationShellController));
                }

                if (submodelServiceProvider != null)
                {
                    services.AddSingleton<IServiceProvider>(submodelServiceProvider)
                            .AddSingleton<IServiceDescriptor>(submodelServiceProvider.ServiceDescriptor)
                            .AddSingleton(submodelServiceProvider);
                    controllerConvention = controllerConvention.Include(typeof(SubmodelController));
                }

                if (submodelRepoServiceProvider != null)
                {
                    services.AddSingleton<IServiceProvider>(submodelRepoServiceProvider)
                            .AddSingleton<IServiceDescriptor>(submodelRepoServiceProvider.ServiceDescriptor)
                            .AddSingleton(submodelRepoServiceProvider);
                    controllerConvention = controllerConvention.Include(typeof(SubmodelRepositoryController));
                }

                if (aasRepoServiceProvider != null)
                {
                    services.AddSingleton<IServiceProvider>(aasRepoServiceProvider)
                            .AddSingleton<IServiceDescriptor>(aasRepoServiceProvider.ServiceDescriptor)
                            .AddSingleton(aasRepoServiceProvider);
                    controllerConvention = controllerConvention.Include(typeof(AssetAdministrationShellRepositoryController));
                }

                if (aasRegistryProvider != null)
                {
                    services.AddSingleton(aasRegistryProvider);
                    controllerConvention = controllerConvention.Include(typeof(AssetAdministrationShellRegistryController));
                }

                if (smRegistryProvider != null)
                {
                    services.AddSingleton(smRegistryProvider);
                    controllerConvention = controllerConvention.Include(typeof(SubmodelRegistryController));
                }

                services.AddMvc((options) =>
                {
                    options.Conventions.Add(controllerConvention);
                });
            });
        }
    }
}