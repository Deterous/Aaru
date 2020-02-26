﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : APFS.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple filesystem plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies the Apple filesystem and shows information.
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
using System.Runtime.InteropServices;
using System.Text;
using DiscImageChef.CommonTypes;
using DiscImageChef.CommonTypes.Interfaces;
using Schemas;
using Marshal = DiscImageChef.Helpers.Marshal;

namespace DiscImageChef.Filesystems
{
    public class APFS : IFilesystem
    {
        const uint APFS_CONTAINER_MAGIC = 0x4253584E; // "NXSB"
        const uint APFS_VOLUME_MAGIC    = 0x42535041; // "APSB"

        public FileSystemType XmlFsType { get; private set; }
        public Encoding       Encoding  { get; private set; }
        public string         Name      => "Apple File System";
        public Guid           Id        => new Guid("A4060F9D-2909-42E2-9D95-DB31FA7EA797");
        public string         Author    => "Natalia Portillo";

        public bool Identify(IMediaImage imagePlugin, Partition partition)
        {
            if(partition.Start >= partition.End) return false;

            byte[]                  sector = imagePlugin.ReadSector(partition.Start);
            ApfsContainerSuperBlock nxSb;

            try { nxSb = Marshal.ByteArrayToStructureLittleEndian<ApfsContainerSuperBlock>(sector); }
            catch { return false; }

            return nxSb.magic == APFS_CONTAINER_MAGIC;
        }

        public void GetInformation(IMediaImage imagePlugin, Partition partition, out string information,
                                   Encoding    encoding)
        {
            Encoding = Encoding.UTF8;
            StringBuilder sbInformation = new StringBuilder();
            XmlFsType   = new FileSystemType();
            information = "";

            if(partition.Start >= partition.End) return;

            byte[]                  sector = imagePlugin.ReadSector(partition.Start);
            ApfsContainerSuperBlock nxSb;

            try { nxSb = Marshal.ByteArrayToStructureLittleEndian<ApfsContainerSuperBlock>(sector); }
            catch { return; }

            if(nxSb.magic != APFS_CONTAINER_MAGIC) return;

            sbInformation.AppendLine("Apple File System");
            sbInformation.AppendLine();
            sbInformation.AppendFormat("{0} bytes per block", nxSb.blockSize).AppendLine();
            sbInformation.AppendFormat("Container has {0} bytes in {1} blocks", nxSb.containerBlocks * nxSb.blockSize,
                                       nxSb.containerBlocks).AppendLine();

            information = sbInformation.ToString();

            XmlFsType = new FileSystemType
            {
                Bootable    = false,
                Clusters    = nxSb.containerBlocks,
                ClusterSize = nxSb.blockSize,
                Type        = "Apple File System"
            };
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct ApfsContainerSuperBlock
        {
            public readonly ulong unknown1; // Varies between copies of the superblock
            public readonly ulong unknown2;
            public readonly ulong unknown3; // Varies by 1 between copies of the superblock
            public readonly ulong unknown4;
            public readonly uint  magic;
            public readonly uint  blockSize;
            public readonly ulong containerBlocks;
        }
    }
}