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
//     Identifies Virtual98 disk images.
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
using System.Text;
using DiscImageChef.CommonTypes.Interfaces;
using DiscImageChef.Console;
using DiscImageChef.Helpers;

namespace DiscImageChef.DiscImages
{
    public partial class Virtual98
    {
        public bool Identify(IFilter imageFilter)
        {
            Stream stream = imageFilter.GetDataForkStream();
            stream.Seek(0, SeekOrigin.Begin);
            // Even if comment is supposedly ASCII, I'm pretty sure most emulators allow Shift-JIS to be used :p
            Encoding shiftjis = Encoding.GetEncoding("shift_jis");

            if(stream.Length < Marshal.SizeOf<Virtual98Header>()) return false;

            byte[] hdrB = new byte[Marshal.SizeOf<Virtual98Header>()];
            stream.Read(hdrB, 0, hdrB.Length);

            v98Hdr = Marshal.ByteArrayToStructureLittleEndian<Virtual98Header>(hdrB);

            if(!v98Hdr.signature.SequenceEqual(signature)) return false;

            DicConsole.DebugWriteLine("Virtual98 plugin", "v98hdr.signature = \"{0}\"",
                                      StringHandlers.CToString(v98Hdr.signature, shiftjis));
            DicConsole.DebugWriteLine("Virtual98 plugin", "v98hdr.comment = \"{0}\"",
                                      StringHandlers.CToString(v98Hdr.comment, shiftjis));
            DicConsole.DebugWriteLine("Virtual98 plugin", "v98hdr.padding = {0}",    v98Hdr.padding);
            DicConsole.DebugWriteLine("Virtual98 plugin", "v98hdr.mbsize = {0}",     v98Hdr.mbsize);
            DicConsole.DebugWriteLine("Virtual98 plugin", "v98hdr.sectorsize = {0}", v98Hdr.sectorsize);
            DicConsole.DebugWriteLine("Virtual98 plugin", "v98hdr.sectors = {0}",    v98Hdr.sectors);
            DicConsole.DebugWriteLine("Virtual98 plugin", "v98hdr.surfaces = {0}",   v98Hdr.surfaces);
            DicConsole.DebugWriteLine("Virtual98 plugin", "v98hdr.cylinders = {0}",  v98Hdr.cylinders);
            DicConsole.DebugWriteLine("Virtual98 plugin", "v98hdr.totals = {0}",     v98Hdr.totals);

            return true;
        }
    }
}