﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : Identify.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies Spectrum FDI disk images.
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

using System.IO;
using System.Linq;
using DiscImageChef.CommonTypes.Interfaces;
using DiscImageChef.Helpers;

namespace DiscImageChef.DiscImages
{
    public partial class UkvFdi
    {
        public bool Identify(IFilter imageFilter)
        {
            Stream stream = imageFilter.GetDataForkStream();
            stream.Seek(0, SeekOrigin.Begin);

            FdiHeader hdr = new FdiHeader();

            if(stream.Length < Marshal.SizeOf<FdiHeader>()) return false;

            byte[] hdrB = new byte[Marshal.SizeOf<FdiHeader>()];
            stream.Read(hdrB, 0, hdrB.Length);
            hdr = Marshal.ByteArrayToStructureLittleEndian<FdiHeader>(hdrB);

            return hdr.magic.SequenceEqual(signature);
        }
    }
}