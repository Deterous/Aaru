﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : Properties.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains properties for CPCEMU disk images.
//
// --[ License ] --------------------------------------------------------------
//
//     This library is free software; you can redistribute it and/or modify
//     it under the terms of the GNU Lesser General Public License as
//     published by the Free Software Foundation; either version 2.1 of the
//     License, or (at your option) any later version.
//
//     This library is distributed in the hope that it will be useful, but
//     WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//     Lesser General Public License for more details.
//
//     You should have received a copy of the GNU Lesser General Public
//     License along with this library; if not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2020 Natalia Portillo
// ****************************************************************************/

using System;
using System.Collections.Generic;
using DiscImageChef.CommonTypes.Structs;
using Schemas;

namespace DiscImageChef.DiscImages
{
    public partial class Cpcdsk
    {
        public ImageInfo              Info         => imageInfo;
        public string                 Name         => "CPCEMU Disk-File and Extended CPC Disk-File";
        public Guid                   Id           => new Guid("724B16CC-ADB9-492E-BA07-CAEEC1012B16");
        public string                 Format       => extended ? "CPCEMU Extended disk image" : "CPCEMU disk image";
        public string                 Author       => "Natalia Portillo";
        public List<DumpHardwareType> DumpHardware => null;
        public CICMMetadataType       CicmMetadata => null;
    }
}