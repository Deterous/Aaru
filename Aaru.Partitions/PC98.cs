// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : PC98.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Partitioning scheme plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Manages NEC PC-9800 partitions.
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
using System.Runtime.InteropServices;
using System.Text;
using DiscImageChef.CommonTypes;
using DiscImageChef.CommonTypes.Interfaces;
using DiscImageChef.Console;
using DiscImageChef.Helpers;
using Marshal = DiscImageChef.Helpers.Marshal;

namespace DiscImageChef.Partitions
{
    public class PC98 : IPartition
    {
        public string Name   => "NEC PC-9800 partition table";
        public Guid   Id     => new Guid("27333401-C7C2-447D-961C-22AD0641A09A");
        public string Author => "Natalia Portillo";

        public bool GetInformation(IMediaImage imagePlugin, out List<Partition> partitions, ulong sectorOffset)
        {
            partitions = new List<Partition>();

            if(sectorOffset != 0) return false;

            byte[] bootSector = imagePlugin.ReadSector(0);
            byte[] sector     = imagePlugin.ReadSector(1);
            if(bootSector[bootSector.Length - 2] != 0x55 || bootSector[bootSector.Length - 1] != 0xAA) return false;

            // Prevent false positives with some FAT BPBs
            if(Encoding.ASCII.GetString(bootSector, 0x36, 3) == "FAT") return false;

            PC98Table table = Marshal.ByteArrayToStructureLittleEndian<PC98Table>(sector);

            ulong counter = 0;

            foreach(PC98Partition entry in table.entries)
            {
                DicConsole.DebugWriteLine("PC98 plugin", "entry.dp_mid = {0}",      entry.dp_mid);
                DicConsole.DebugWriteLine("PC98 plugin", "entry.dp_sid = {0}",      entry.dp_sid);
                DicConsole.DebugWriteLine("PC98 plugin", "entry.dp_dum1 = {0}",     entry.dp_dum1);
                DicConsole.DebugWriteLine("PC98 plugin", "entry.dp_dum2 = {0}",     entry.dp_dum2);
                DicConsole.DebugWriteLine("PC98 plugin", "entry.dp_ipl_sct = {0}",  entry.dp_ipl_sct);
                DicConsole.DebugWriteLine("PC98 plugin", "entry.dp_ipl_head = {0}", entry.dp_ipl_head);
                DicConsole.DebugWriteLine("PC98 plugin", "entry.dp_ipl_cyl = {0}",  entry.dp_ipl_cyl);
                DicConsole.DebugWriteLine("PC98 plugin", "entry.dp_ssect = {0}",    entry.dp_ssect);
                DicConsole.DebugWriteLine("PC98 plugin", "entry.dp_shd = {0}",      entry.dp_shd);
                DicConsole.DebugWriteLine("PC98 plugin", "entry.dp_scyl = {0}",     entry.dp_scyl);
                DicConsole.DebugWriteLine("PC98 plugin", "entry.dp_esect = {0}",    entry.dp_esect);
                DicConsole.DebugWriteLine("PC98 plugin", "entry.dp_ehd = {0}",      entry.dp_ehd);
                DicConsole.DebugWriteLine("PC98 plugin", "entry.dp_ecyl = {0}",     entry.dp_ecyl);
                DicConsole.DebugWriteLine("PC98 plugin", "entry.dp_name = \"{0}\"",
                                          StringHandlers.CToString(entry.dp_name, Encoding.GetEncoding(932)));

                if(entry.dp_scyl  == entry.dp_ecyl             || entry.dp_ecyl <= 0 ||
                   entry.dp_scyl  > imagePlugin.Info.Cylinders ||
                   entry.dp_ecyl  > imagePlugin.Info.Cylinders || entry.dp_shd   > imagePlugin.Info.Heads           ||
                   entry.dp_ehd   > imagePlugin.Info.Heads     || entry.dp_ssect > imagePlugin.Info.SectorsPerTrack ||
                   entry.dp_esect > imagePlugin.Info.SectorsPerTrack) continue;

                Partition part = new Partition
                {
                    Start = CHS.ToLBA(entry.dp_scyl, entry.dp_shd, (uint)(entry.dp_ssect + 1),
                                      imagePlugin.Info.Heads, imagePlugin.Info.SectorsPerTrack),
                    Type     = DecodePC98Sid(entry.dp_sid),
                    Name     = StringHandlers.CToString(entry.dp_name, Encoding.GetEncoding(932)).Trim(),
                    Sequence = counter,
                    Scheme   = Name
                };
                part.Offset = part.Start * imagePlugin.Info.SectorSize;
                part.Length = CHS.ToLBA(entry.dp_ecyl, entry.dp_ehd, (uint)(entry.dp_esect + 1), imagePlugin.Info.Heads,
                                        imagePlugin.Info.SectorsPerTrack) - part.Start;
                part.Size = part.Length * imagePlugin.Info.SectorSize;

                DicConsole.DebugWriteLine("PC98 plugin", "part.Start = {0}",    part.Start);
                DicConsole.DebugWriteLine("PC98 plugin", "part.Type = {0}",     part.Type);
                DicConsole.DebugWriteLine("PC98 plugin", "part.Name = {0}",     part.Name);
                DicConsole.DebugWriteLine("PC98 plugin", "part.Sequence = {0}", part.Sequence);
                DicConsole.DebugWriteLine("PC98 plugin", "part.Offset = {0}",   part.Offset);
                DicConsole.DebugWriteLine("PC98 plugin", "part.Length = {0}",   part.Length);
                DicConsole.DebugWriteLine("PC98 plugin", "part.Size = {0}",     part.Size);

                if((entry.dp_mid & 0x20) != 0x20 && (entry.dp_mid & 0x44) != 0x44 ||
                   part.Start >= imagePlugin.Info.Sectors                         ||
                   part.End   > imagePlugin.Info.Sectors) continue;

                partitions.Add(part);
                counter++;
            }

            return partitions.Count > 0;
        }

        static string DecodePC98Sid(byte sid)
        {
            switch(sid & 0x7F)
            {
                case 0x01: return "FAT12";
                case 0x04: return "PC-UX";
                case 0x06: return "N88-BASIC(86)";
                // Supposedly for FAT16 < 32 MiB, seen in bigger partitions
                case 0x11:
                case 0x21: return "FAT16";
                case 0x28:
                case 0x41:
                case 0x48: return "Windows Volume Set";
                case 0x44: return "FreeBSD";
                case 0x61: return "FAT32";
                case 0x62: return "Linux";
                default:   return "Unknown";
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct PC98Table
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public PC98Partition[] entries;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct PC98Partition
        {
            /// <summary>
            ///     Some ID, if 0x80 bit is set, it is bootable
            /// </summary>
            public byte dp_mid;
            /// <summary>
            ///     Some ID, if 0x80 bit is set, it is active
            /// </summary>
            public byte dp_sid;
            public byte   dp_dum1;
            public byte   dp_dum2;
            public byte   dp_ipl_sct;
            public byte   dp_ipl_head;
            public ushort dp_ipl_cyl;
            public byte   dp_ssect;
            public byte   dp_shd;
            public ushort dp_scyl;
            public byte   dp_esect;
            public byte   dp_ehd;
            public ushort dp_ecyl;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] dp_name;
        }
    }
}