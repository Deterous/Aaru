﻿// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : UFS.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Aaru unit testing.
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
// Copyright © 2011-2024 Natalia Portillo
// ****************************************************************************/

using System.IO;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Interfaces;
using Aaru.Filesystems;
using NUnit.Framework;

namespace Aaru.Tests.Filesystems.UFS;

[TestFixture]
public class MBR : FilesystemTest
{
    public override string DataFolder => Path.Combine(Consts.TestFilesRoot, "Filesystems", "UNIX filesystem (MBR)");
    public override IFilesystem Plugin => new FFSPlugin();
    public override bool Partitions => true;

    public override FileSystemTest[] Tests =>
    [
        new FileSystemTest
        {
            TestFile    = "ufs1/linux.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 65024,
            ClusterSize = 2048,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ufs2/linux.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 65024,
            ClusterSize = 2048,
            Type        = "ufs2",
            VolumeName  = "VolumeLabel"
        },
        new FileSystemTest
        {
            TestFile    = "ffs43/darwin_1.3.1.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 131008,
            ClusterSize = 1024,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ffs43/darwin_1.4.1.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 131008,
            ClusterSize = 1024,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ffs43/darwin_6.0.2.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 131008,
            ClusterSize = 1024,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ffs43/darwin_7.0.1.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 131040,
            ClusterSize = 1024,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ffs43/darwin_8.0.1.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 131040,
            ClusterSize = 1024,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ffs43/dflybsd_1.2.0.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 1024000,
            SectorSize  = 512,
            Clusters    = 511950,
            ClusterSize = 1024,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ffs43/dflybsd_3.6.1.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 1024000,
            SectorSize  = 512,
            Clusters    = 255470,
            ClusterSize = 2048,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ffs43/dflybsd_4.0.5.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 1024000,
            SectorSize  = 512,
            Clusters    = 255470,
            ClusterSize = 2048,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ffs43/netbsd_1.6.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 130032,
            ClusterSize = 1024,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ffs43/netbsd_6.1.5.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 131040,
            ClusterSize = 1024,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ffs43/netbsd_7.1.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 131040,
            ClusterSize = 1024,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ffs43/netbsd_7.1_be.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 131040,
            ClusterSize = 1024,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ffs43/openbsd_4.7.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 65504,
            ClusterSize = 2048,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ufs1/darwin_1.3.1.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 131008,
            ClusterSize = 1024,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ufs1/darwin_1.4.1.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 131008,
            ClusterSize = 1024,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ufs1/darwin_6.0.2.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 131008,
            ClusterSize = 1024,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ufs1/darwin_7.0.1.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 131040,
            ClusterSize = 1024,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ufs1/darwin_8.0.1.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 131040,
            ClusterSize = 1024,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ufs1/dflybsd_1.2.0.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 2097152,
            SectorSize  = 512,
            Clusters    = 1048500,
            ClusterSize = 1024,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ufs1/dflybsd_3.6.1.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 2097152,
            SectorSize  = 512,
            Clusters    = 523758,
            ClusterSize = 2048,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ufs1/dflybsd_4.0.5.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 2097152,
            SectorSize  = 512,
            Clusters    = 523758,
            ClusterSize = 2048,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ufs1/freebsd_6.1.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 65500,
            ClusterSize = 2048,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ufs1/freebsd_7.0.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 65500,
            ClusterSize = 2048,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ufs1/freebsd_8.2.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 65500,
            ClusterSize = 2048,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ufs1/netbsd_1.6.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 130032,
            ClusterSize = 1024,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ufs1/netbsd_6.1.5.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 131040,
            ClusterSize = 1024,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ufs1/netbsd_7.1.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 131040,
            ClusterSize = 1024,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ufs1/netbsd_7.1_be.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 131040,
            ClusterSize = 1024,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ufs1/solaris_7.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 2097152,
            SectorSize  = 512,
            Clusters    = 1038240,
            ClusterSize = 1024,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ufs1/solaris_9.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 2097152,
            SectorSize  = 512,
            Clusters    = 1046808,
            ClusterSize = 1024,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ufs2/freebsd_6.1.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 65500,
            ClusterSize = 2048,
            Type        = "ufs2",
            VolumeName  = ""
        },
        new FileSystemTest
        {
            TestFile    = "ufs2/freebsd_7.0.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 65500,
            ClusterSize = 2048,
            Type        = "ufs2",
            VolumeName  = ""
        },
        new FileSystemTest
        {
            TestFile    = "ufs2/freebsd_8.2.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 65500,
            ClusterSize = 2048,
            Type        = "ufs2",
            VolumeName  = ""
        },
        new FileSystemTest
        {
            TestFile    = "ufs2/netbsd_7.1.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 131040,
            ClusterSize = 1024,
            Type        = "ufs2",
            VolumeName  = ""
        },
        new FileSystemTest
        {
            TestFile    = "ufs2/netbsd_7.1_be.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 131040,
            ClusterSize = 1024,
            Type        = "ufs2",
            VolumeName  = ""
        },
        new FileSystemTest
        {
            TestFile    = "ufs2/netbsd_6.1.5.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 131040,
            ClusterSize = 1024,
            Type        = "ufs2",
            VolumeName  = ""
        },
        new FileSystemTest
        {
            TestFile    = "ufs2/openbsd_4.7.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 65254,
            ClusterSize = 2048,
            Type        = "ufs2",
            VolumeName  = ""
        },
        new FileSystemTest
        {
            TestFile    = "ffs43/dflybsd_1.0.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 130504,
            ClusterSize = 1024,
            Type        = "ufs",
            VolumeName  = null
        },
        new FileSystemTest
        {
            TestFile    = "ufs1/dflybsd_1.0.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 131008,
            ClusterSize = 1024,
            Type        = "ufs",
            VolumeName  = null
        },
        new FileSystemTest
        {
            TestFile    = "ufs1/openbsd_4.7.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 65504,
            ClusterSize = 2048,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ffs43/macosx_10.3.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 131068,
            ClusterSize = 1024,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ufs1/macosx_10.3.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 131068,
            ClusterSize = 1024,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ffs43/macosx_10.4.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 131040,
            ClusterSize = 1024,
            Type        = "ufs"
        },
        new FileSystemTest
        {
            TestFile    = "ufs1/macosx_10.4.aif",
            MediaType   = MediaType.GENERIC_HDD,
            Sectors     = 262144,
            SectorSize  = 512,
            Clusters    = 131040,
            ClusterSize = 1024,
            Type        = "ufs"
        }
    ];
}