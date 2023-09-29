﻿/*******************************************************************************
* Copyright (c) 2022 Bosch Rexroth AG
* Author: Constantin Ziesche (constantin.ziesche@bosch.com)
*
* This program and the accompanying materials are made available under the
* terms of the MIT License which is available at
* https://github.com/eclipse-basyx/basyx-dotnet/blob/main/LICENSE
*
* SPDX-License-Identifier: MIT
*******************************************************************************/

namespace BaSyx.Models.AdminShell
{
    public class Environment : IEnvironment
    {
        public IElementContainer<IAssetAdministrationShell> AssetAdministrationShells { get; set; }

        public IElementContainer<ISubmodel> Submodels { get; set; }

        public IElementContainer<IConceptDescription> ConceptDescriptions { get; set; }

        public Environment()
        {
            AssetAdministrationShells = new ElementContainer<IAssetAdministrationShell>();
            Submodels = new ElementContainer<ISubmodel>();
            ConceptDescriptions = new ElementContainer<IConceptDescription>();
        }      
    }
}
