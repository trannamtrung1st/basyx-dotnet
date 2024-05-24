/*******************************************************************************
* Copyright (c) 2023 Bosch Rexroth AG
* Author: Constantin Ziesche (constantin.ziesche@bosch.com)
*
* This program and the accompanying materials are made available under the
* terms of the MIT License which is available at
* https://github.com/eclipse-basyx/basyx-dotnet/blob/main/LICENSE
*
* SPDX-License-Identifier: MIT
*******************************************************************************/
using BaSyx.API.Interfaces;
using BaSyx.Models.Connectivity;
using BaSyx.Models.AdminShell;
using System.Collections.Generic;

namespace BaSyx.API.ServiceProvider
{
    public interface ISubmodelRepositoryServiceProvider : IServiceProvider<IEnumerable<ISubmodel>, ISubmodelRepositoryDescriptor>, ISubmodelRepositoryInterface
    {
        ISubmodelServiceProviderRegistry SubmodelProviderRegistry { get; }
    }
}
