﻿/*******************************************************************************
* Copyright (c) 2024 Bosch Rexroth AG
* Author: Constantin Ziesche (constantin.ziesche@bosch.com)
*
* This program and the accompanying materials are made available under the
* terms of the MIT License which is available at
* https://github.com/eclipse-basyx/basyx-dotnet/blob/main/LICENSE
*
* SPDX-License-Identifier: MIT
*******************************************************************************/
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using BaSyx.Utils.Assembly;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using BaSyx.Components.Common.Abstractions;
using BaSyx.Common.UI.Swagger.Filters;
using System.Collections.Generic;

namespace BaSyx.Common.UI.Swagger
{
    internal static class OpenApiInfos
    {
        #region Static OpenApi-Infos
        internal static readonly OpenApiInfo AssetAdministrationShell_OpenApiInfo = new OpenApiInfo
        {
            Version = "v1",
            Title = "BaSyx Asset Administration Shell HTTP REST-API",
            Description = "The full description of the generic BaSyx Asset Administration Shell HTTP REST-API",
            Contact = new OpenApiContact { Name = "Constantin Ziesche", Email = "constantin.ziesche@bosch.com", Url = new Uri("https://www.bosch.com/de/") },
            License = new OpenApiLicense { Name = "MIT" }
        };

        internal static readonly OpenApiInfo AssetAdministrationShellRepository_OpenApiInfo = new OpenApiInfo
        {
            Version = "v1",
            Title = "BaSyx Asset Administration Shell Repository HTTP REST-API",
            Description = "The full description of the generic BaSyx Asset Administration Shell Repository HTTP REST-API",
            Contact = new OpenApiContact { Name = "Constantin Ziesche", Email = "constantin.ziesche@bosch.com", Url = new Uri("https://www.bosch.com/de/") },
            License = new OpenApiLicense { Name = "MIT" }
        };

        internal static readonly OpenApiInfo Submodel_OpenApiInfo = new OpenApiInfo
        {
            Version = "v1",
            Title = "BaSyx Submodel HTTP REST-API",
            Description = "The full description of the generic BaSyx Submodel HTTP REST-API",
            Contact = new OpenApiContact { Name = "Constantin Ziesche", Email = "constantin.ziesche@bosch.com", Url = new Uri("https://www.bosch.com/de/") },
            License = new OpenApiLicense { Name = "MIT" }
        };

        internal static readonly OpenApiInfo SubmodelRepository_OpenApiInfo = new OpenApiInfo
        {
            Version = "v1",
            Title = "BaSyx Submodel Repository HTTP REST-API",
            Description = "The full description of the generic BaSyx Submodel Repository HTTP REST-API",
            Contact = new OpenApiContact { Name = "Constantin Ziesche", Email = "constantin.ziesche@bosch.com", Url = new Uri("https://www.bosch.com/de/") },
            License = new OpenApiLicense { Name = "MIT" }
        };

        internal static readonly OpenApiInfo AasRegistry_OpenApiInfo = new OpenApiInfo
        {
            Version = "v1",
            Title = "BaSyx AAS Registry HTTP REST-API",
            Description = "The full description of the BaSyx AAS Registry HTTP REST-API",
            Contact = new OpenApiContact { Name = "Constantin Ziesche", Email = "constantin.ziesche@bosch.com", Url = new Uri("https://www.bosch.com/de/") },
            License = new OpenApiLicense { Name = "MIT" }
        };

        internal static readonly OpenApiInfo SubmodelRegistry_OpenApiInfo = new OpenApiInfo
        {
            Version = "v1",
            Title = "BaSyx Submodel Registry HTTP REST-API",
            Description = "The full description of the BaSyx Submodel Registry HTTP REST-API",
            Contact = new OpenApiContact { Name = "Trung Tran", Email = "trannamtrung1st@gmail.com" },
            License = new OpenApiLicense { Name = "MIT" }
        };

        internal static OpenApiInfo GetApiInfo(Interface interfaceType)
        {
            switch (interfaceType)
            {
                case Interface.AssetAdministrationShell:
                    return AssetAdministrationShell_OpenApiInfo;
                case Interface.AssetAdministrationShellRepository:
                    return AssetAdministrationShellRepository_OpenApiInfo;
                case Interface.Submodel:
                    return Submodel_OpenApiInfo;
                case Interface.SubmodelRepository:
                    return SubmodelRepository_OpenApiInfo;
                case Interface.AssetAdministrationShellRegistry:
                    return AasRegistry_OpenApiInfo;
                case Interface.SubmodelRegistry:
                    return SubmodelRegistry_OpenApiInfo;
                case Interface.All:
                    return AssetAdministrationShell_OpenApiInfo;
                default:
                    return default;
            }
        }

        #endregion
    }

    public enum Interface
    {
        All,
        AssetAdministrationShell,
        AssetAdministrationShellRepository,
        AssetAdministrationShellRegistry,
        Submodel,
        SubmodelRepository,
        SubmodelRegistry,
    }
    public static class SwaggerExtensions
    {
        public static void AddSwagger(this IServerApplication serverApp,
            Interface interfaceType, string xmlCommentFilePath = null,
            IEnumerable<Assembly> scanAssemblies = null)
        {
            OpenApiInfo info = OpenApiInfos.GetApiInfo(interfaceType);
            serverApp.ConfigureServices(services =>
            {
                services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", info);

                    // this operations resolves path conflicts (double route path). Commented out for better error detection (swagger fails).
                    // c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

                    string xmlPath = null;

                    // Set the comments path for the Swagger JSON and UI.
                    if (string.IsNullOrEmpty(xmlCommentFilePath))
                    {
                        string xmlFile = $"{serverApp.ControllerAssembly.GetName().Name}.xml";
                        string executionPath = serverApp.Settings?.ExecutionPath ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                        xmlPath = Path.Combine(executionPath, xmlFile);
                    }
                    else
                        xmlPath = xmlCommentFilePath;

                    if (EmbeddedResource.CheckOrWriteRessourceToFile(serverApp.ControllerAssembly, xmlPath))
                        c.IncludeXmlComments(xmlPath, true);

                    if (interfaceType != Interface.All)
                        c.DocumentFilter<ControllerSelector>(interfaceType);

                    // Scan and apply all classes inheriting from OperationBasedFilter
                    ApplyOperationBasedFilters(c, scanAssemblies);
                });
            });

            serverApp.Configure(app =>
            {
                // Enable middleware to serve generated Swagger as a JSON endpoint.
                app.UseSwagger();
                app.UseDeveloperExceptionPage();

                // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", info.Title);
                });
            });
        }

        private static void ApplyOperationBasedFilters(SwaggerGenOptions options, IEnumerable<Assembly> scanAssemblies)
        {
            var operationBasedFilterTypes = scanAssemblies.SelectMany(a => a.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(OperationBasedFilter).IsAssignableFrom(t)));

            foreach (var filterType in operationBasedFilterTypes)
            {
                options.OperationFilterDescriptors.Add(new FilterDescriptor
                {
                    Type = filterType,
                    FilterInstance = Activator.CreateInstance(filterType)
                });
            }
        }

        internal protected class ControllerSelector : IDocumentFilter
        {
            private readonly Interface _interfaceType;
            private readonly string _interfaceName;
            private readonly string _controllerName;
            public ControllerSelector(Interface interfaceType)
            {
                _interfaceType = interfaceType;
                _interfaceName = Enum.GetName(typeof(Interface), _interfaceType);
                _controllerName = _interfaceName + "Controller";
            }

            public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
            {
                foreach (var apiDescription in context.ApiDescriptions)
                {
                    string name = apiDescription.ActionDescriptor.DisplayName;
                    if (!name.Contains(_controllerName))
                    {
                        string route = "/" + apiDescription.ActionDescriptor.AttributeRouteInfo.Template;
                        swaggerDoc.Paths.Remove(route);
                    }
                }
                foreach (var tag in swaggerDoc.Tags.ToList())
                {
                    if (tag.Name != _controllerName)
                    {
                        swaggerDoc.Tags.Remove(tag);
                    }
                }
            }
        }
    }
}
