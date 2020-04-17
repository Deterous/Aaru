// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : ViewLocator.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI.
//
// --[ Description ] ----------------------------------------------------------
//
//     View locator.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General public License for more details.
//
//     You should have received a copy of the GNU General public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2020 Natalia Portillo
// ****************************************************************************/

using System;
using Aaru.Gui.ViewModels;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Aaru.Gui
{
    public class ViewLocator : IDataTemplate
    {
        public bool SupportsRecycling => false;

        public IControl Build(object data)
        {
            string name = data.GetType().FullName?.Replace("ViewModel", "View");

            if(name is null)
                return null;

            var type = Type.GetType(name);

            if(type != null)
            {
                return (Control)Activator.CreateInstance(type);
            }

            return new TextBlock
            {
                Text = "Not Found: " + name
            };
        }

        public bool Match(object data) => data is ViewModelBase;
    }
}