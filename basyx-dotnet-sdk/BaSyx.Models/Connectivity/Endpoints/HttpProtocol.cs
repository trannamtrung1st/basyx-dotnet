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
using System;

namespace BaSyx.Models.Connectivity
{
    public class HttpProtocol : ProtocolInformation
    {
        public HttpProtocol(string endpointAddress) : base(endpointAddress)
        { }

        public HttpProtocol(Uri uri) : this(uri?.ToString())
        { }
    }
}
