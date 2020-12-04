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
using System.Collections.Generic;
using System.Text;

namespace BaSyx.Utils.Settings
{
    public class SettingsCollection : List<Settings>
    {
        public Settings this[string name] => this.Find(e => e.Name == name);

        public T GetSettings<T>(string name) where T : Settings
        {
            return (T)this[name];
        }
    }
}
