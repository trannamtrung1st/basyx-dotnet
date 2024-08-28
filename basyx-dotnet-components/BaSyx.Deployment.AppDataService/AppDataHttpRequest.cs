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
namespace BaSyx.Deployment.AppDataService
{
    public class AppDataHttpRequest
    {
        public string ConfigurationPath { get; set; }
        public string Id { get; set; }
        public string Phase { get; set; }
    }
}
