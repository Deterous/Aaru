// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : Verify.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Verifies Apple DiskCopy 4.2 disk images.
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
using DiscImageChef.Console;

namespace DiscImageChef.DiscImages
{
    public partial class DiskCopy42
    {
        public bool? VerifyMediaImage()
        {
            byte[] data    = new byte[header.DataSize];
            byte[] tags    = new byte[header.TagSize];
            uint   tagsChk = 0;

            DicConsole.DebugWriteLine("DC42 plugin", "Reading data");
            Stream datastream = dc42ImageFilter.GetDataForkStream();
            datastream.Seek(dataOffset, SeekOrigin.Begin);
            datastream.Read(data, 0, (int)header.DataSize);

            DicConsole.DebugWriteLine("DC42 plugin", "Calculating data checksum");
            uint dataChk = DC42CheckSum(data);
            DicConsole.DebugWriteLine("DC42 plugin", "Calculated data checksum = 0x{0:X8}", dataChk);
            DicConsole.DebugWriteLine("DC42 plugin", "Stored data checksum = 0x{0:X8}",     header.DataChecksum);

            if(header.TagSize <= 0) return dataChk == header.DataChecksum && tagsChk == header.TagChecksum;

            DicConsole.DebugWriteLine("DC42 plugin", "Reading tags");
            Stream tagstream = dc42ImageFilter.GetDataForkStream();
            tagstream.Seek(tagOffset, SeekOrigin.Begin);
            tagstream.Read(tags, 0, (int)header.TagSize);

            DicConsole.DebugWriteLine("DC42 plugin", "Calculating tag checksum");
            tagsChk = DC42CheckSum(tags);
            DicConsole.DebugWriteLine("DC42 plugin", "Calculated tag checksum = 0x{0:X8}", tagsChk);
            DicConsole.DebugWriteLine("DC42 plugin", "Stored tag checksum = 0x{0:X8}",     header.TagChecksum);

            return dataChk == header.DataChecksum && tagsChk == header.TagChecksum;
        }
    }
}