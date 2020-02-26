// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : PathTable.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : ISO9660 filesystem plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Decodes path tables.
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
// In the loving memory of Facunda "Tata" Suárez Domínguez, R.I.P. 2019/07/24
// ****************************************************************************/

using System.Collections.Generic;
using DiscImageChef.Helpers;

namespace DiscImageChef.Filesystems.ISO9660
{
    public partial class ISO9660
    {
        PathTableEntryInternal[] DecodePathTable(byte[] data)
        {
            if(data is null) return null;

            List<PathTableEntryInternal> table = new List<PathTableEntryInternal>();

            int off = 0;
            while(off < data.Length)
            {
                PathTableEntry entry =
                    Marshal.ByteArrayToStructureBigEndian<PathTableEntry>(data, off, Marshal.SizeOf<PathTableEntry>());

                if(entry.name_len == 0) break;

                off += Marshal.SizeOf<PathTableEntry>();

                string name = Encoding.GetString(data, off, entry.name_len);

                table.Add(new PathTableEntryInternal
                {
                    Extent      = entry.start_lbn,
                    Name        = name,
                    Parent      = entry.parent_dirno,
                    XattrLength = entry.xattr_len
                });

                off += entry.name_len;

                if(entry.name_len % 2 != 0) off++;
            }

            return table.ToArray();
        }

        PathTableEntryInternal[] DecodeHighSierraPathTable(byte[] data)
        {
            if(data is null) return null;

            List<PathTableEntryInternal> table = new List<PathTableEntryInternal>();

            int off = 0;
            while(off < data.Length)
            {
                HighSierraPathTableEntry entry =
                    Marshal.ByteArrayToStructureBigEndian<HighSierraPathTableEntry>(data, off,
                                                                                    Marshal
                                                                                       .SizeOf<HighSierraPathTableEntry
                                                                                        >());

                if(entry.name_len == 0) break;

                off += Marshal.SizeOf<HighSierraPathTableEntry>();

                string name = Encoding.GetString(data, off, entry.name_len);

                table.Add(new PathTableEntryInternal
                {
                    Extent      = entry.start_lbn,
                    Name        = name,
                    Parent      = entry.parent_dirno,
                    XattrLength = entry.xattr_len
                });

                off += entry.name_len;

                if(entry.name_len % 2 != 0) off++;
            }

            return table.ToArray();
        }
    }
}