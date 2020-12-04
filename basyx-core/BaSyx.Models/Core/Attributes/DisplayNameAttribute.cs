/*******************************************************************************
* Copyright (c) 2020 Robert Bosch GmbH
* Author: Constantin Ziesche (constantin.ziesche@bosch.com)
*
* This program and the accompanying materials are made available under the
* terms of the Eclipse Public License 2.0 which is available at
* http://www.eclipse.org/legal/epl-2.0
*
* SPDX-License-Identifier: EPL-2.0
*******************************************************************************/
using System;
using BaSyx.Models.Core.AssetAdministrationShell;

namespace BaSyx.Models.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Interface | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public sealed class DisplayNameAttribute : Attribute
    {
        public LangString DisplayName { get; }
        public DisplayNameAttribute(string language, string text)
        {
            DisplayName = new LangString(language, text);
        }
    }
}
