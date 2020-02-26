﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : HAMMER.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : HAMMER filesystem plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies the HAMMER filesystem and shows information.
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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using DiscImageChef.CommonTypes;
using DiscImageChef.CommonTypes.Interfaces;
using Schemas;
using hammer_crc_t = System.UInt32;
using hammer_off_t = System.UInt64;
using hammer_tid_t = System.UInt64;
using Marshal = DiscImageChef.Helpers.Marshal;

#pragma warning disable 169

namespace DiscImageChef.Filesystems
{
    public class HAMMER : IFilesystem
    {
        const ulong HAMMER_FSBUF_VOLUME     = 0xC8414D4DC5523031;
        const ulong HAMMER_FSBUF_VOLUME_REV = 0x313052C54D4D41C8;
        const uint  HAMMER_VOLHDR_SIZE      = 1928;
        const int   HAMMER_BIGBLOCK_SIZE    = 8192 * 1024;

        public FileSystemType XmlFsType { get; private set; }
        public Encoding       Encoding  { get; private set; }
        public string         Name      => "HAMMER Filesystem";
        public Guid           Id        => new Guid("91A188BF-5FD7-4677-BBD3-F59EBA9C864D");
        public string         Author    => "Natalia Portillo";

        public bool Identify(IMediaImage imagePlugin, Partition partition)
        {
            uint run = HAMMER_VOLHDR_SIZE / imagePlugin.Info.SectorSize;

            if(HAMMER_VOLHDR_SIZE % imagePlugin.Info.SectorSize > 0) run++;

            if(run + partition.Start >= partition.End) return false;

            byte[] sbSector = imagePlugin.ReadSectors(partition.Start, run);

            ulong magic = BitConverter.ToUInt64(sbSector, 0);

            return magic == HAMMER_FSBUF_VOLUME || magic == HAMMER_FSBUF_VOLUME_REV;
        }

        public void GetInformation(IMediaImage imagePlugin, Partition partition, out string information,
                                   Encoding    encoding)
        {
            Encoding    = encoding ?? Encoding.GetEncoding("iso-8859-15");
            information = "";

            StringBuilder sb = new StringBuilder();

            HammerSuperBlock hammerSb;

            uint run = HAMMER_VOLHDR_SIZE / imagePlugin.Info.SectorSize;

            if(HAMMER_VOLHDR_SIZE % imagePlugin.Info.SectorSize > 0) run++;

            byte[] sbSector = imagePlugin.ReadSectors(partition.Start, run);

            ulong magic = BitConverter.ToUInt64(sbSector, 0);

            if(magic == HAMMER_FSBUF_VOLUME)
                hammerSb  = Marshal.ByteArrayToStructureLittleEndian<HammerSuperBlock>(sbSector);
            else hammerSb = Marshal.ByteArrayToStructureBigEndian<HammerSuperBlock>(sbSector);

            sb.AppendLine("HAMMER filesystem");

            sb.AppendFormat("Volume version: {0}", hammerSb.vol_version).AppendLine();
            sb.AppendFormat("Volume {0} of {1} on this filesystem", hammerSb.vol_no + 1, hammerSb.vol_count)
              .AppendLine();
            sb.AppendFormat("Volume name: {0}", StringHandlers.CToString(hammerSb.vol_label, Encoding)).AppendLine();
            sb.AppendFormat("Volume serial: {0}", hammerSb.vol_fsid).AppendLine();
            sb.AppendFormat("Filesystem type: {0}", hammerSb.vol_fstype).AppendLine();
            sb.AppendFormat("Boot area starts at {0}", hammerSb.vol_bot_beg).AppendLine();
            sb.AppendFormat("Memory log starts at {0}", hammerSb.vol_mem_beg).AppendLine();
            sb.AppendFormat("First volume buffer starts at {0}", hammerSb.vol_buf_beg).AppendLine();
            sb.AppendFormat("Volume ends at {0}", hammerSb.vol_buf_end).AppendLine();

            XmlFsType = new FileSystemType
            {
                Clusters     = partition.Size / HAMMER_BIGBLOCK_SIZE,
                ClusterSize  = HAMMER_BIGBLOCK_SIZE,
                Dirty        = false,
                Type         = "HAMMER",
                VolumeName   = StringHandlers.CToString(hammerSb.vol_label, Encoding),
                VolumeSerial = hammerSb.vol_fsid.ToString()
            };

            if(hammerSb.vol_no == hammerSb.vol_rootvol)
            {
                sb.AppendFormat("Filesystem contains {0} \"big-blocks\" ({1} bytes)", hammerSb.vol0_stat_bigblocks,
                                hammerSb.vol0_stat_bigblocks * HAMMER_BIGBLOCK_SIZE).AppendLine();
                sb.AppendFormat("Filesystem has {0} \"big-blocks\" free ({1} bytes)", hammerSb.vol0_stat_freebigblocks,
                                hammerSb.vol0_stat_freebigblocks * HAMMER_BIGBLOCK_SIZE).AppendLine();
                sb.AppendFormat("Filesystem has {0} inode used", hammerSb.vol0_stat_inodes).AppendLine();

                XmlFsType.Clusters              = (ulong)hammerSb.vol0_stat_bigblocks;
                XmlFsType.FreeClusters          = (ulong)hammerSb.vol0_stat_freebigblocks;
                XmlFsType.FreeClustersSpecified = true;
                XmlFsType.Files                 = (ulong)hammerSb.vol0_stat_inodes;
                XmlFsType.FilesSpecified        = true;
            }
            // 0 ?
            //sb.AppendFormat("Volume header CRC: 0x{0:X8}", afs_sb.vol_crc).AppendLine();

            information = sb.ToString();
        }

        /// <summary>
        ///     Hammer superblock
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [SuppressMessage("ReSharper", "BuiltInTypeReferenceStyle")]
        struct HammerSuperBlock
        {
            /// <summary><see cref="HAMMER_FSBUF_VOLUME" /> for a valid header</summary>
            public readonly ulong vol_signature;

            /* These are relative to block device offset, not zone offsets. */
            /// <summary>offset of boot area</summary>
            public readonly long vol_bot_beg;
            /// <summary>offset of memory log</summary>
            public readonly long vol_mem_beg;
            /// <summary>offset of the first buffer in volume</summary>
            public readonly long vol_buf_beg;
            /// <summary>offset of volume EOF (on buffer boundary)</summary>
            public readonly long vol_buf_end;
            public readonly long vol_reserved01;

            /// <summary>identify filesystem</summary>
            public readonly Guid vol_fsid;
            /// <summary>identify filesystem type</summary>
            public readonly Guid vol_fstype;
            /// <summary>filesystem label</summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public readonly byte[] vol_label;

            /// <summary>volume number within filesystem</summary>
            public readonly int vol_no;
            /// <summary>number of volumes making up filesystem</summary>
            public readonly int vol_count;

            /// <summary>version control information</summary>
            public readonly uint vol_version;
            /// <summary>header crc</summary>
            public readonly hammer_crc_t vol_crc;
            /// <summary>volume flags</summary>
            public readonly uint vol_flags;
            /// <summary>the root volume number (must be 0)</summary>
            public readonly uint vol_rootvol;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public readonly uint[] vol_reserved;

            /*
             * These fields are initialized and space is reserved in every
             * volume making up a HAMMER filesytem, but only the root volume
             * contains valid data.  Note that vol0_stat_bigblocks does not
             * include big-blocks for freemap and undomap initially allocated
             * by newfs_hammer(8).
             */
            /// <summary>total big-blocks when fs is empty</summary>
            public readonly long vol0_stat_bigblocks;
            /// <summary>number of free big-blocks</summary>
            public readonly long vol0_stat_freebigblocks;
            public readonly long vol0_reserved01;
            /// <summary>for statfs only</summary>
            public readonly long vol0_stat_inodes;
            public readonly long vol0_reserved02;
            /// <summary>B-Tree root offset in zone-8</summary>
            public readonly hammer_off_t vol0_btree_root;
            /// <summary>highest partially synchronized TID</summary>
            public readonly hammer_tid_t vol0_next_tid;
            public readonly hammer_off_t vol0_reserved03;

            /// <summary>
            ///     Blockmaps for zones.  Not all zones use a blockmap.  Note that the entire root blockmap is cached in the
            ///     hammer_mount structure.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public readonly HammerBlockMap[] vol0_blockmap;

            /// <summary>Array of zone-2 addresses for undo FIFO.</summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            public readonly hammer_off_t[] vol0_undo_array;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "BuiltInTypeReferenceStyle")]
        struct HammerBlockMap
        {
            /// <summary>zone-2 offset only used by zone-4</summary>
            public hammer_off_t phys_offset;
            /// <summary>zone-X offset only used by zone-3</summary>
            public hammer_off_t first_offset;
            /// <summary>zone-X offset for allocation</summary>
            public hammer_off_t next_offset;
            /// <summary>zone-X offset only used by zone-3</summary>
            public hammer_off_t alloc_offset;
            public uint         reserved01;
            public hammer_crc_t entry_crc;
        }
    }
}