﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : ImageFormat.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Core algorithms.
//
// --[ Description ] ----------------------------------------------------------
//
//     Gets a new instance of all known plugins.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2020 Natalia Portillo
// ****************************************************************************/

using DiscImageChef.Checksums;
using DiscImageChef.CommonTypes;
using DiscImageChef.CommonTypes.Interfaces;

namespace DiscImageChef.Core
{
    public static class GetPluginBase
    {
        public static PluginBase Instance
        {
            get
            {
                PluginBase instance = new PluginBase();

                IPluginRegister checksumRegister    = new Register();
                IPluginRegister imagesRegister      = new DiscImages.Register();
                IPluginRegister filesystemsRegister = new DiscImageChef.Filesystems.Register();
                IPluginRegister filtersRegister     = new Filters.Register();
                IPluginRegister partitionsRegister  = new DiscImageChef.Partitions.Register();

                instance.AddPlugins(checksumRegister);
                instance.AddPlugins(imagesRegister);
                instance.AddPlugins(filesystemsRegister);
                instance.AddPlugins(filtersRegister);
                instance.AddPlugins(partitionsRegister);

                return instance;
            }
        }
    }
}