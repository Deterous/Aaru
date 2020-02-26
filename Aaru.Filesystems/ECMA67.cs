﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : ECMA67.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : ECMA-67 plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies the ECMA-67 file system and shows information.
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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DiscImageChef.CommonTypes;
using DiscImageChef.CommonTypes.Interfaces;
using Schemas;
using Marshal = DiscImageChef.Helpers.Marshal;

namespace DiscImageChef.Filesystems
{
    public class ECMA67 : IFilesystem
    {
        readonly byte[] ecma67_magic = {0x56, 0x4F, 0x4C};

        public Encoding       Encoding  { get; private set; }
        public string         Name      => "ECMA-67";
        public Guid           Id        => new Guid("62A2D44A-CBC1-4377-B4B6-28C5C92034A1");
        public FileSystemType XmlFsType { get; private set; }
        public string         Author    => "Natalia Portillo";

        public bool Identify(IMediaImage imagePlugin, Partition partition)
        {
            if(partition.Start > 0) return false;

            if(partition.End < 8) return false;

            byte[] sector = imagePlugin.ReadSector(6);

            if(sector.Length != 128) return false;

            VolumeLabel vol = Marshal.ByteArrayToStructureLittleEndian<VolumeLabel>(sector);

            return ecma67_magic.SequenceEqual(vol.labelIdentifier) && vol.labelNumber == 1 && vol.recordLength == 0x31;
        }

        public void GetInformation(IMediaImage imagePlugin, Partition partition, out string information,
                                   Encoding    encoding)
        {
            Encoding = encoding ?? Encoding.GetEncoding("iso-8859-1");
            byte[] sector = imagePlugin.ReadSector(6);

            StringBuilder sbInformation = new StringBuilder();

            VolumeLabel vol = Marshal.ByteArrayToStructureLittleEndian<VolumeLabel>(sector);

            sbInformation.AppendLine("ECMA-67");

            sbInformation.AppendFormat("Volume name: {0}", Encoding.ASCII.GetString(vol.volumeIdentifier)).AppendLine();
            sbInformation.AppendFormat("Volume owner: {0}", Encoding.ASCII.GetString(vol.owner)).AppendLine();

            XmlFsType = new FileSystemType
            {
                Type        = "ECMA-67",
                ClusterSize = 256,
                Clusters    = partition.End - partition.Start + 1,
                VolumeName  = Encoding.ASCII.GetString(vol.volumeIdentifier)
            };

            information = sbInformation.ToString();
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct VolumeLabel
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public readonly byte[] labelIdentifier;
            public readonly byte labelNumber;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public readonly byte[] volumeIdentifier;
            public readonly byte volumeAccessibility;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 26)]
            public readonly byte[] reserved1;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
            public readonly byte[] owner;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public readonly byte[] reserved2;
            public readonly byte surface;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public readonly byte[] reserved3;
            public readonly byte recordLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public readonly byte[] reserved4;
            public readonly byte fileLabelAllocation;
            public readonly byte labelStandardVersion;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]
            public readonly byte[] reserved5;
        }
    }
}