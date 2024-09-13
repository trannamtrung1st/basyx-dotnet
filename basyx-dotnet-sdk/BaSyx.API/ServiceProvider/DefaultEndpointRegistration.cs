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
using BaSyx.API.Http;
using BaSyx.Models.Connectivity;
using BaSyx.Utils.Extensions;
using BaSyx.Utils.Network;
using BaSyx.Utils.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace BaSyx.API.ServiceProvider
{
    public static class DefaultEndpointRegistration
    {
        private static readonly ILogger logger = LoggingExtentions.CreateLogger("DefaultEndpointRegistration");
        public static void UseAutoEndpointRegistration(this IAssetAdministrationShellRepositoryServiceProvider serviceProvider, ServerConfiguration serverConfiguration)
        {
            List<IEndpoint> endpoints = ExtractEndpoints(serverConfiguration, InterfaceName.AssetAdministrationShellRepositoryInterface);
            serviceProvider.UseDefaultEndpointRegistration(endpoints);
        }

        public static void UseAutoEndpointRegistration(this ISubmodelRepositoryServiceProvider serviceProvider, ServerConfiguration serverConfiguration)
        {
            List<IEndpoint> endpoints = ExtractEndpoints(serverConfiguration, InterfaceName.SubmodelRepositoryInterface);
            serviceProvider.UseDefaultEndpointRegistration(endpoints);
        }

        public static void UseAutoEndpointRegistration(this IAssetAdministrationShellServiceProvider serviceProvider, ServerConfiguration serverConfiguration)
        {
            List<IEndpoint> endpoints = ExtractEndpoints(serverConfiguration, InterfaceName.AssetAdministrationShellInterface);
            serviceProvider.UseDefaultEndpointRegistration(endpoints);
        }

        public static void UseAutoEndpointRegistration(this ISubmodelServiceProvider serviceProvider, ServerConfiguration serverConfiguration)
        {
            List<IEndpoint> endpoints = ExtractEndpoints(serverConfiguration, InterfaceName.SubmodelInterface);
            serviceProvider.UseDefaultEndpointRegistration(endpoints);
        }

        private static List<IEndpoint> ConvertEndpoints(List<string> urls, InterfaceName interfaceName, string pathBase = null)
        {
            try
            {
                List<IEndpoint> endpoints = new List<IEndpoint>();
                foreach (var url in urls)
                {
                    Uri uri = new Uri(url);
                    if (!string.IsNullOrEmpty(pathBase))
                        uri = new Uri(uri, pathBase);
                    endpoints.Add(new Endpoint(uri, interfaceName));
                }
                return endpoints;
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Error converting urls");
                return null;
            }
        }

        public static List<IEndpoint> ExtractEndpoints(ServerConfiguration serverConfiguration, InterfaceName interfaceName)
        {
            string multiUrl = serverConfiguration.Hosting.Urls.Find(u => u.Contains("+"));
            List<IEndpoint> endpoints;

            if (!string.IsNullOrEmpty(multiUrl))
            {
                Uri uri = new Uri(multiUrl.Replace("+", "localhost"));
                bool includeIPv6 = serverConfiguration.Hosting.EnableIPv6 ?? false;
                endpoints = GetNetworkInterfaceBasedEndpoints(uri.Scheme, uri.Port, includeIPv6, interfaceName, serverConfiguration.PathBase);
            }
            else
            {
                endpoints = ConvertEndpoints(serverConfiguration.Hosting.Urls, interfaceName, serverConfiguration.PathBase);
            }

            return endpoints;
        }

        public static List<IEndpoint> GetNetworkInterfaceBasedEndpoints(string endpointType, int port, bool includeIPv6, InterfaceName interfaceName, string pathBase)
        {
            IEnumerable<IPAddress> IPAddresses = NetworkUtils.GetIPAddresses(includeIPv6);
            List<IEndpoint> endpoints = new List<IEndpoint>();
            foreach (IPAddress address in IPAddresses)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    string endpoint = endpointType + "://" + address.ToString() + ":" + port + pathBase;
                    endpoints.Add(new Endpoint(endpoint, interfaceName));
                    logger.LogInformation("Using " + endpoint + " as endpoint");
                }
                else if (includeIPv6 && address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    string endpoint = endpointType + "://[" + address.ToString() + "]:" + port + pathBase;
                    endpoints.Add(new Endpoint(endpoint, interfaceName));
                    logger.LogInformation("Using " + endpoint + " as endpoint");
                }
                else
                    continue;
            }
            return endpoints;
        }

        public static void UseDefaultEndpointRegistration(this IAssetAdministrationShellRepositoryServiceProvider serviceProvider, IEnumerable<IEndpoint> endpoints)
        {
            List<IEndpoint> repositoryEndpoints = new List<IEndpoint>();
            foreach (var endpoint in endpoints)
            {
                string epAddress = endpoint.ProtocolInformation.Href;
                if (!epAddress.EndsWith(AssetAdministrationShellRepositoryRoutes.SHELLS))
                    epAddress = epAddress.TrimEnd('/') + AssetAdministrationShellRepositoryRoutes.SHELLS;

                repositoryEndpoints.Add(new Endpoint(epAddress, InterfaceName.AssetAdministrationShellRepositoryInterface));
            }

            serviceProvider.ServiceDescriptor.AddEndpoints(repositoryEndpoints);
            var aasRepositoryDescriptor = serviceProvider.ServiceDescriptor;
            foreach (var aasDescriptor in aasRepositoryDescriptor.AssetAdministrationShellDescriptors)
            {
                aasDescriptor.UseRepoEndpointRegistration(repositoryEndpoints);

                foreach (var submodelDescriptor in aasDescriptor.SubmodelDescriptors)
                    submodelDescriptor.UseRepoEndpointRegistration(repoEndpoints: aasDescriptor.Endpoints);
            }
        }

        public static void UseRepoEndpointRegistration(this IAssetAdministrationShellServiceProvider aasProvider, IEnumerable<IEndpoint> repoEndpoints)
        {
            aasProvider.ServiceDescriptor.UseRepoEndpointRegistration(repoEndpoints);
        }

        public static void UseRepoEndpointRegistration(this IAssetAdministrationShellDescriptor aasDescriptor, IEnumerable<IEndpoint> repoEndpoints)
        {
            List<IEndpoint> aasEndpoints = new List<IEndpoint>();
            foreach (var endpoint in repoEndpoints)
            {
                var ep = new Endpoint(GetAssetAdministrationShellEndpoint(endpoint, aasDescriptor.Id.Id), InterfaceName.AssetAdministrationShellInterface);
                aasEndpoints.Add(ep);
            }
            aasDescriptor.AddEndpoints(aasEndpoints);
        }

        public static void UseRepoEndpointRegistration(this ISubmodelServiceProvider submodelProvider, IEnumerable<IEndpoint> repoEndpoints)
        {
            submodelProvider.ServiceDescriptor.UseRepoEndpointRegistration(repoEndpoints);
        }

        public static void UseRepoEndpointRegistration(this ISubmodelDescriptor submodelDescriptor, IEnumerable<IEndpoint> repoEndpoints)
        {
            List<IEndpoint> submodelEndpoints = new List<IEndpoint>();
            foreach (var endpoint in repoEndpoints)
            {
                var ep = new Endpoint(GetSubmodelInRepositoryEndpoint(endpoint, submodelDescriptor.Id.Id), InterfaceName.SubmodelInterface);
                submodelEndpoints.Add(ep);
            }
            submodelDescriptor.AddEndpoints(submodelEndpoints);
        }

        public static void UseDefaultEndpointRegistration(this ISubmodelRepositoryServiceProvider serviceProvider, IEnumerable<IEndpoint> endpoints)
        {
            List<IEndpoint> repositoryEndpoints = new List<IEndpoint>();
            foreach (var endpoint in endpoints)
            {
                string epAddress = endpoint.ProtocolInformation.Href;
                if (!epAddress.EndsWith(SubmodelRepositoryRoutes.SUBMODELS))
                    epAddress = epAddress.TrimEnd('/') + SubmodelRepositoryRoutes.SUBMODELS;

                repositoryEndpoints.Add(new Endpoint(epAddress, InterfaceName.SubmodelRepositoryInterface));
            }

            serviceProvider.ServiceDescriptor.AddEndpoints(repositoryEndpoints);
            var submodelRepositoryDescriptor = serviceProvider.ServiceDescriptor;
            foreach (var submodelDescriptor in submodelRepositoryDescriptor.SubmodelDescriptors)
                submodelDescriptor.UseRepoEndpointRegistration(repositoryEndpoints);
        }

        public static void UseDefaultEndpointRegistration(this IAssetAdministrationShellServiceProvider serviceProvider, IEnumerable<IEndpoint> endpoints)
        {
            List<IEndpoint> aasEndpoints = new List<IEndpoint>();
            foreach (var endpoint in endpoints)
            {
                string epAddress = endpoint.ProtocolInformation.Href;
                if (!epAddress.EndsWith(AssetAdministrationShellRoutes.AAS))
                    epAddress = epAddress.TrimEnd('/') + AssetAdministrationShellRoutes.AAS;

                aasEndpoints.Add(new Endpoint(epAddress, InterfaceName.AssetAdministrationShellInterface));
            }

            serviceProvider.ServiceDescriptor.AddEndpoints(aasEndpoints);
            var aasDescriptor = serviceProvider.ServiceDescriptor;
            foreach (var submodel in aasDescriptor.SubmodelDescriptors)
            {
                List<IEndpoint> spEndpoints = new List<IEndpoint>();
                foreach (var endpoint in aasEndpoints)
                {
                    var ep = new Endpoint(GetSubmodelEndpoint(endpoint, submodel.Id.Id), InterfaceName.SubmodelInterface);
                    spEndpoints.Add(ep);
                }
                submodel.AddEndpoints(spEndpoints);
            }
        }

        public static void UseDefaultEndpointRegistration(this ISubmodelServiceProvider serviceProvider, IEnumerable<IEndpoint> endpoints)
        {
            List<IEndpoint> submodelEndpoints = new List<IEndpoint>();
            foreach (var endpoint in endpoints)
            {
                string epAddress = endpoint.ProtocolInformation.Href;
                if (!epAddress.EndsWith(SubmodelRoutes.SUBMODEL))
                    epAddress = epAddress.TrimEnd('/') + SubmodelRoutes.SUBMODEL;

                submodelEndpoints.Add(new Endpoint(epAddress, InterfaceName.SubmodelInterface));
            }

            serviceProvider.ServiceDescriptor.AddEndpoints(submodelEndpoints);
        }

        public static string GetSubmodelInRepositoryEndpoint(IEndpoint endpoint, string submodelId)
        {
            string epAddress = endpoint.ProtocolInformation.Href;
            if (!epAddress.EndsWith(SubmodelRepositoryRoutes.SUBMODELS))
                epAddress = epAddress.TrimEnd('/') + SubmodelRepositoryRoutes.SUBMODELS;

            submodelId = StringOperations.Base64UrlEncode(submodelId);

            return epAddress + "/" + submodelId;
        }

        public static string GetSubmodelEndpoint(IEndpoint endpoint, string submodelId)
        {
            string epAddress = endpoint.ProtocolInformation.Href;
            if (!epAddress.EndsWith(AssetAdministrationShellRoutes.AAS))
                epAddress = epAddress.TrimEnd('/') + AssetAdministrationShellRoutes.AAS;

            submodelId = StringOperations.Base64UrlEncode(submodelId);

            return epAddress + SubmodelRepositoryRoutes.SUBMODELS + "/" + submodelId;
        }

        public static string GetAssetAdministrationShellEndpoint(IEndpoint endpoint, string aasId)
        {
            string epAddress = endpoint.ProtocolInformation.Href;
            if (!epAddress.EndsWith(AssetAdministrationShellRepositoryRoutes.SHELLS))
                epAddress = epAddress.TrimEnd('/') + AssetAdministrationShellRepositoryRoutes.SHELLS;

            aasId = StringOperations.Base64UrlEncode(aasId);

            return epAddress + "/" + aasId;
        }
    }
}
