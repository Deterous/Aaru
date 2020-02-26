﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : FAT16.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : DiscImageChef unit testing.
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
// Copyright © 2011-2019 Natalia Portillo
// ****************************************************************************/

using System.Collections.Generic;
using System.IO;
using DiscImageChef.CommonTypes;
using DiscImageChef.CommonTypes.Interfaces;
using DiscImageChef.DiscImages;
using DiscImageChef.Filesystems.FAT;
using DiscImageChef.Filters;
using NUnit.Framework;

namespace DiscImageChef.Tests.Filesystems
{
    [TestFixture]
    public class Fat16
    {
        readonly string[] testfiles =
        {
            // MS-DOS 3.30A
            "msdos_3.30A_mf2ed.img.lz",
            // MS-DOS 3.31
            "msdos_3.31_mf2ed.img.lz"
        };

        readonly MediaType[] mediatypes =
        {
            // MS-DOS 3.30A
            MediaType.DOS_35_ED,
            // MS-DOS 3.31
            MediaType.DOS_35_ED
        };

        readonly ulong[] sectors =
        {
            // MS-DOS 3.30A
            5760,
            // MS-DOS 3.31
            5760
        };

        readonly uint[] sectorsize =
        {
            // MS-DOS 3.30A
            512,
            // MS-DOS 3.31
            512
        };

        readonly long[] clusters =
        {
            // MS-DOS 3.30A
            5760,
            // MS-DOS 3.31
            5760
        };

        readonly int[] clustersize =
        {
            // MS-DOS 3.30A
            512,
            // MS-DOS 3.31
            512
        };

        readonly string[] volumename =
        {
            // MS-DOS 3.30A
            null,
            // MS-DOS 3.31
            null
        };

        readonly string[] volumeserial =
        {
            // MS-DOS 3.30A
            null,
            // MS-DOS 3.31
            null
        };

        readonly string[] oemid =
        {
            // MS-DOS 3.30A
            "MSDOS3.3",
            // MS-DOS 3.31
            "IBM  3.3"
        };

        [Test]
        public void Test()
        {
            for(int i = 0; i < testfiles.Length; i++)
            {
                string  location = Path.Combine(Consts.TestFilesRoot, "filesystems", "fat16", testfiles[i]);
                IFilter filter   = new LZip();
                filter.Open(location);
                IMediaImage image = new ZZZRawImage();
                Assert.AreEqual(true,          image.Open(filter),    testfiles[i]);
                Assert.AreEqual(mediatypes[i], image.Info.MediaType,  testfiles[i]);
                Assert.AreEqual(sectors[i],    image.Info.Sectors,    testfiles[i]);
                Assert.AreEqual(sectorsize[i], image.Info.SectorSize, testfiles[i]);
                IFilesystem fs = new FAT();
                Partition wholePart = new Partition
                {
                    Name   = "Whole device",
                    Length = image.Info.Sectors,
                    Size   = image.Info.Sectors * image.Info.SectorSize
                };
                Assert.AreEqual(true, fs.Identify(image, wholePart), testfiles[i]);
                fs.GetInformation(image, wholePart, out _, null);
                Assert.AreEqual(clusters[i],     fs.XmlFsType.Clusters,         testfiles[i]);
                Assert.AreEqual(clustersize[i],  fs.XmlFsType.ClusterSize,      testfiles[i]);
                Assert.AreEqual("FAT16",         fs.XmlFsType.Type,             testfiles[i]);
                Assert.AreEqual(volumename[i],   fs.XmlFsType.VolumeName,       testfiles[i]);
                Assert.AreEqual(volumeserial[i], fs.XmlFsType.VolumeSerial,     testfiles[i]);
                Assert.AreEqual(oemid[i],        fs.XmlFsType.SystemIdentifier, testfiles[i]);
            }
        }
    }

    [TestFixture]
    public class Fat16Apm
    {
        readonly string[] testfiles = {"macosx_10.11.vdi.lz"};

        readonly ulong[] sectors = {1024000};

        readonly uint[] sectorsize = {512};

        readonly long[] clusters = {63995};

        readonly int[] clustersize = {8192};

        readonly string[] volumename = {"VOLUMELABEL"};

        readonly string[] volumeserial = {"063D1F09"};

        readonly string[] oemid = {"BSD  4.4"};

        [Test]
        public void Test()
        {
            for(int i = 0; i < testfiles.Length; i++)
            {
                string  location = Path.Combine(Consts.TestFilesRoot, "filesystems", "fat16_apm", testfiles[i]);
                IFilter filter   = new LZip();
                filter.Open(location);
                IMediaImage image = new Vdi();
                Assert.AreEqual(true,          image.Open(filter),    testfiles[i]);
                Assert.AreEqual(sectors[i],    image.Info.Sectors,    testfiles[i]);
                Assert.AreEqual(sectorsize[i], image.Info.SectorSize, testfiles[i]);
                List<Partition> partitions = Core.Partitions.GetAll(image);
                IFilesystem     fs         = new FAT();
                int             part       = -1;
                for(int j = 0; j < partitions.Count; j++)
                    if(partitions[j].Type == "DOS_FAT_16")
                    {
                        part = j;
                        break;
                    }

                Assert.AreNotEqual(-1, part, $"Partition not found on {testfiles[i]}");
                Assert.AreEqual(true, fs.Identify(image, partitions[part]), testfiles[i]);
                fs.GetInformation(image, partitions[part], out _, null);
                Assert.AreEqual(clusters[i],     fs.XmlFsType.Clusters,         testfiles[i]);
                Assert.AreEqual(clustersize[i],  fs.XmlFsType.ClusterSize,      testfiles[i]);
                Assert.AreEqual("FAT16",         fs.XmlFsType.Type,             testfiles[i]);
                Assert.AreEqual(volumename[i],   fs.XmlFsType.VolumeName,       testfiles[i]);
                Assert.AreEqual(volumeserial[i], fs.XmlFsType.VolumeSerial,     testfiles[i]);
                Assert.AreEqual(oemid[i],        fs.XmlFsType.SystemIdentifier, testfiles[i]);
            }
        }
    }

    [TestFixture]
    public class Fat16Atari
    {
        readonly string[] testfiles = {"tos_1.04.vdi.lz", "tos_1.04_small.vdi.lz"};

        readonly ulong[] sectors = {81920, 16384};

        readonly uint[] sectorsize = {512, 512};

        readonly long[] clusters = {10239, 8191};

        readonly int[] clustersize = {4096, 1024};

        readonly string[] volumename = {null, null};

        readonly string[] volumeserial = {"BA9831", "2019E1"};

        readonly string[] oemid = {null, null};

        [Test]
        public void Test()
        {
            for(int i = 0; i < testfiles.Length; i++)
            {
                string  location = Path.Combine(Consts.TestFilesRoot, "filesystems", "fat16_atari", testfiles[i]);
                IFilter filter   = new LZip();
                filter.Open(location);
                IMediaImage image = new Vdi();
                Assert.AreEqual(true,          image.Open(filter),    testfiles[i]);
                Assert.AreEqual(sectors[i],    image.Info.Sectors,    testfiles[i]);
                Assert.AreEqual(sectorsize[i], image.Info.SectorSize, testfiles[i]);
                List<Partition> partitions = Core.Partitions.GetAll(image);
                IFilesystem     fs         = new FAT();
                int             part       = -1;
                for(int j = 0; j < partitions.Count; j++)
                    if(partitions[j].Type == "GEM" || partitions[j].Type == "BGM")
                    {
                        part = j;
                        break;
                    }

                Assert.AreNotEqual(-1, part, $"Partition not found on {testfiles[i]}");
                Assert.AreEqual(true, fs.Identify(image, partitions[part]), testfiles[i]);
                fs.GetInformation(image, partitions[part], out _, null);
                Assert.AreEqual(clusters[i],     fs.XmlFsType.Clusters,         testfiles[i]);
                Assert.AreEqual(clustersize[i],  fs.XmlFsType.ClusterSize,      testfiles[i]);
                Assert.AreEqual("FAT16",         fs.XmlFsType.Type,             testfiles[i]);
                Assert.AreEqual(volumename[i],   fs.XmlFsType.VolumeName,       testfiles[i]);
                Assert.AreEqual(volumeserial[i], fs.XmlFsType.VolumeSerial,     testfiles[i]);
                Assert.AreEqual(oemid[i],        fs.XmlFsType.SystemIdentifier, testfiles[i]);
            }
        }
    }

    [TestFixture]
    public class Fat16Gpt
    {
        readonly string[] testfiles = {"macosx_10.11.vdi.lz"};

        readonly ulong[] sectors = {1024000};

        readonly uint[] sectorsize = {512};

        readonly long[] clusters = {63995};

        readonly int[] clustersize = {8192};

        readonly string[] volumename = {"VOLUMELABEL"};

        readonly string[] volumeserial = {"2E8A1F1B"};

        readonly string[] oemid = {"BSD  4.4"};

        [Test]
        public void Test()
        {
            for(int i = 0; i < testfiles.Length; i++)
            {
                string  location = Path.Combine(Consts.TestFilesRoot, "filesystems", "fat16_gpt", testfiles[i]);
                IFilter filter   = new LZip();
                filter.Open(location);
                IMediaImage image = new Vdi();
                Assert.AreEqual(true,          image.Open(filter),    testfiles[i]);
                Assert.AreEqual(sectors[i],    image.Info.Sectors,    testfiles[i]);
                Assert.AreEqual(sectorsize[i], image.Info.SectorSize, testfiles[i]);
                List<Partition> partitions = Core.Partitions.GetAll(image);
                IFilesystem     fs         = new FAT();
                int             part       = -1;
                for(int j = 0; j < partitions.Count; j++)
                    if(partitions[j].Type == "Microsoft Basic data")
                    {
                        part = j;
                        break;
                    }

                Assert.AreNotEqual(-1, part, $"Partition not found on {testfiles[i]}");
                Assert.AreEqual(true, fs.Identify(image, partitions[part]), testfiles[i]);
                fs.GetInformation(image, partitions[part], out _, null);
                Assert.AreEqual(clusters[i],     fs.XmlFsType.Clusters,         testfiles[i]);
                Assert.AreEqual(clustersize[i],  fs.XmlFsType.ClusterSize,      testfiles[i]);
                Assert.AreEqual("FAT16",         fs.XmlFsType.Type,             testfiles[i]);
                Assert.AreEqual(volumename[i],   fs.XmlFsType.VolumeName,       testfiles[i]);
                Assert.AreEqual(volumeserial[i], fs.XmlFsType.VolumeSerial,     testfiles[i]);
                Assert.AreEqual(oemid[i],        fs.XmlFsType.SystemIdentifier, testfiles[i]);
            }
        }
    }

    [TestFixture]
    public class Fat16Mbr
    {
        readonly string[] testfiles =
        {
            "drdos_3.40.vdi.lz", "drdos_3.41.vdi.lz", "drdos_5.00.vdi.lz", "drdos_6.00.vdi.lz", "drdos_7.02.vdi.lz",
            "drdos_7.03.vdi.lz", "drdos_8.00.vdi.lz", "msdos331.vdi.lz", "msdos401.vdi.lz", "msdos500.vdi.lz",
            "msdos600.vdi.lz", "msdos620rc1.vdi.lz", "msdos620.vdi.lz", "msdos621.vdi.lz", "msdos622.vdi.lz",
            "msdos710.vdi.lz", "novelldos_7.00.vdi.lz", "opendos_7.01.vdi.lz", "pcdos2000.vdi.lz",
            "pcdos400.vdi.lz", "pcdos500.vdi.lz", "pcdos502.vdi.lz", "pcdos610.vdi.lz", "pcdos630.vdi.lz",
            "msos2_1.21.vdi.lz", "msos2_1.30.1.vdi.lz", "multiuserdos_7.22r4.vdi.lz", "os2_1.20.vdi.lz",
            "os2_1.30.vdi.lz", "os2_6.307.vdi.lz", "os2_6.514.vdi.lz", "os2_6.617.vdi.lz", "os2_8.162.vdi.lz",
            "os2_9.023.vdi.lz", "ecs.vdi.lz", "macosx_10.11.vdi.lz", "win10.vdi.lz", "win2000.vdi.lz",
            "win95osr2.1.vdi.lz", "win95osr2.5.vdi.lz", "win95osr2.vdi.lz", "win95.vdi.lz", "win98se.vdi.lz",
            "win98.vdi.lz", "winme.vdi.lz", "winnt_3.10.vdi.lz", "winnt_3.50.vdi.lz", "winnt_3.51.vdi.lz",
            "winnt_4.00.vdi.lz", "winvista.vdi.lz", "beos_r4.5.vdi.lz", "linux.vdi.lz", "amigaos_3.9.vdi.lz",
            "aros.vdi.lz", "freebsd_6.1.vdi.lz", "freebsd_7.0.vdi.lz", "freebsd_8.2.vdi.lz", "macos_7.5.3.vdi.lz",
            "macos_7.5.vdi.lz", "macos_7.6.vdi.lz", "macos_8.0.vdi.lz", "ecs20_fstester.vdi.lz",
            "linux_2.2_umsdos16_flashdrive.vdi.lz", "linux_4.19_fat16_msdos_flashdrive.vdi.lz",
            "linux_4.19_vfat16_flashdrive.vdi.lz"
        };

        readonly ulong[] sectors =
        {
            1024000, 1024000, 1024000, 1024000, 1024000, 1024000, 1024000, 1024000, 1024000, 1024000, 1024000,
            1024000, 1024000, 1024000, 1024000, 1024000, 1024000, 1024000, 1024000, 1024000, 1024000, 1024000,
            1024000, 1024000, 1024000, 1024000, 1024000, 1024000, 1024000, 1024000, 1024000, 1024000, 1024000,
            1024000, 1024000, 1024000, 1024000, 1024000, 1024000, 1024000, 1024000, 1024000, 1024000, 1024000,
            1024000, 1024000, 1024000, 1024000, 1024000, 1024000, 1024000, 262144, 1024128, 1024000, 1024000,
            1024000, 1024000, 1024000, 1024000, 1024000, 1024000, 1024000, 1024000, 1024000, 1024000
        };

        readonly uint[] sectorsize =
        {
            512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512,
            512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512,
            512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512,
            512, 512
        };

        readonly long[] clusters =
        {
            63882, 63941, 63941, 63941, 63941, 63941, 63941, 63941, 63941, 63941, 63941, 63941, 63941, 63941, 63941,
            63941, 63941, 63941, 63941, 63941, 63941, 63941, 63941, 63941, 63941, 63941, 63941, 63941, 63941, 63941,
            63941, 63941, 63941, 63941, 63882, 63992, 63864, 63252, 63941, 63941, 63941, 63941, 63998, 63998, 63998,
            63941, 63998, 63998, 63941, 63616, 63996, 65024, 63941, 63882, 63998, 63998, 31999, 63941, 63941, 63941,
            63941, 63882, 63941, 63872, 63872
        };

        readonly int[] clustersize =
        {
            8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192,
            8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192,
            8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192,
            2048, 8192, 8192, 8192, 8192, 16384, 8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192
        };

        readonly string[] volumename =
        {
            "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL",
            null, "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL",
            "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "NO NAME    ", "VOLUMELABEL",
            "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL",
            "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL",
            "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL",
            "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL",
            "VOLUMELABEL", "VOLUMELABEL", "VolumeLabel", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL",
            "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUMELABEL", "VOLUME LABE", "DICSETTER",
            "DICSETTER", "DICSETTER"
        };

        readonly string[] volumeserial =
        {
            null, null, null, null, null, null, "1BFB0748", null, "217B1909", "0C6D18FC", "382B18F4", "3E2018E9",
            "0D2418EF", "195A181B", "27761816", "356B1809", null, null, "2272100F", "07280FE1", "1F630FF9",
            "18340FFE", "3F3F1003", "273D1009", "9C162C15", "9C1E2C15", null, "5BE66015", "5BE43015", "5BEAC015",
            "E6B18414", "E6C63414", "1C069414", "1C059414", "1BE5B814", "3EF71EF4", "DAF97911", "305637BD",
            "275B0DE4", "09650DFC", "38270D18", "2E620D0C", "0B4F0EED", "0E122464", "3B5F0F02", "C84CB6F2",
            "D0E9AD4E", "C039A2EC", "501F9FA6", "9AAA4216", "00000000", "A132D985", "374D3BD1", "52BEA34A",
            "3CF10E0D", "C6C30E0D", "44770E0D", "27761816", "27761816", "27761816", "27761816", "66AAF014",
            "5CC78D47", "A552A493", "FCC308A7"
        };

        readonly string[] oemid =
        {
            "IBM  3.2", "IBM  3.2", "IBM  3.3", "IBM  3.3", "IBM  3.3", "DRDOS  7", "IBM  5.0", "IBM  3.3",
            "MSDOS4.0", "MSDOS5.0", "MSDOS5.0", "MSDOS5.0", "MSDOS5.0", "MSDOS5.0", "MSDOS5.0", "MSWIN4.1",
            "IBM  3.3", "IBM  3.3", "IBM  7.0", "IBM  4.0", "IBM  5.0", "IBM  5.0", "IBM  6.0", "IBM  6.0",
            "IBM 10.2", "IBM 10.2", "IBM  3.2", "IBM 10.2", "IBM 10.2", "IBM 20.0", "IBM 20.0", "IBM 20.0",
            "IBM 20.0", "IBM 20.0", "IBM 4.50", "BSD  4.4", "MSDOS5.0", "MSDOS5.0", "MSWIN4.1", "MSWIN4.1",
            "MSWIN4.1", "MSWIN4.0", "MSWIN4.1", "MSWIN4.1", "MSWIN4.1", "MSDOS5.0", "MSDOS5.0", "MSDOS5.0",
            "MSDOS5.0", "MSDOS5.0", "BeOS    ", "mkfs.fat", "CDP  5.0", "MSWIN4.1", "BSD  4.4", "BSD  4.4",
            "BSD4.4  ", "PCX 2.0 ", "PCX 2.0 ", "PCX 2.0 ", "PCX 2.0 ", "IBM 4.50", null, "mkfs.fat", "mkfs.fat"
        };

        [Test]
        public void Test()
        {
            for(int i = 0; i < testfiles.Length; i++)
            {
                string  location = Path.Combine(Consts.TestFilesRoot, "filesystems", "fat16_mbr", testfiles[i]);
                IFilter filter   = new LZip();
                filter.Open(location);
                IMediaImage image = new Vdi();
                Assert.AreEqual(true,          image.Open(filter),    testfiles[i]);
                Assert.AreEqual(sectors[i],    image.Info.Sectors,    testfiles[i]);
                Assert.AreEqual(sectorsize[i], image.Info.SectorSize, testfiles[i]);
                List<Partition> partitions = Core.Partitions.GetAll(image);
                IFilesystem     fs         = new FAT();
                Assert.AreEqual(true, fs.Identify(image, partitions[0]), testfiles[i]);
                fs.GetInformation(image, partitions[0], out _, null);
                Assert.AreEqual(clusters[i],     fs.XmlFsType.Clusters,         testfiles[i]);
                Assert.AreEqual(clustersize[i],  fs.XmlFsType.ClusterSize,      testfiles[i]);
                Assert.AreEqual("FAT16",         fs.XmlFsType.Type,             testfiles[i]);
                Assert.AreEqual(volumename[i],   fs.XmlFsType.VolumeName,       testfiles[i]);
                Assert.AreEqual(volumeserial[i], fs.XmlFsType.VolumeSerial,     testfiles[i]);
                Assert.AreEqual(oemid[i],        fs.XmlFsType.SystemIdentifier, testfiles[i]);
            }
        }
    }

    [TestFixture]
    public class Fat16Rdb
    {
        readonly string[] testfiles = {"amigaos_3.9.vdi.lz"};

        readonly ulong[] sectors = {1024128};

        readonly uint[] sectorsize = {512};

        readonly long[] clusters = {63689};

        readonly int[] clustersize = {8192};

        readonly string[] volumename = {"VOLUMELABEL"};

        readonly string[] volumeserial = {"374D40D1"};

        readonly string[] oemid = {"CDP  5.0"};

        [Test]
        public void Test()
        {
            for(int i = 0; i < testfiles.Length; i++)
            {
                string  location = Path.Combine(Consts.TestFilesRoot, "filesystems", "fat16_rdb", testfiles[i]);
                IFilter filter   = new LZip();
                filter.Open(location);
                IMediaImage image = new Vdi();
                Assert.AreEqual(true,          image.Open(filter),    testfiles[i]);
                Assert.AreEqual(sectors[i],    image.Info.Sectors,    testfiles[i]);
                Assert.AreEqual(sectorsize[i], image.Info.SectorSize, testfiles[i]);
                List<Partition> partitions = Core.Partitions.GetAll(image);
                IFilesystem     fs         = new FAT();
                int             part       = -1;
                for(int j = 0; j < partitions.Count; j++)
                    if(partitions[j].Type == "0x06")
                    {
                        part = j;
                        break;
                    }

                Assert.AreNotEqual(-1, part, $"Partition not found on {testfiles[i]}");
                Assert.AreEqual(true, fs.Identify(image, partitions[part]), testfiles[i]);
                fs.GetInformation(image, partitions[part], out _, null);
                Assert.AreEqual(clusters[i],     fs.XmlFsType.Clusters,         testfiles[i]);
                Assert.AreEqual(clustersize[i],  fs.XmlFsType.ClusterSize,      testfiles[i]);
                Assert.AreEqual("FAT16",         fs.XmlFsType.Type,             testfiles[i]);
                Assert.AreEqual(volumename[i],   fs.XmlFsType.VolumeName,       testfiles[i]);
                Assert.AreEqual(volumeserial[i], fs.XmlFsType.VolumeSerial,     testfiles[i]);
                Assert.AreEqual(oemid[i],        fs.XmlFsType.SystemIdentifier, testfiles[i]);
            }
        }
    }

    [TestFixture]
    public class Fat16Human
    {
        readonly string[] testfiles = {"sasidisk.hdf.lz", "scsidisk.hds.lz"};

        readonly ulong[] sectors = {162096, 204800};

        readonly uint[] sectorsize = {256, 512};

        readonly long[] clusters = {40510, 102367};

        readonly int[] clustersize = {1024, 1024};

        readonly string[] volumename = {null, null};

        readonly string[] volumeserial = {null, null};

        readonly string[] oemid = {"Hudson soft 2.00", " Hero Soft V1.10"};

        [Test]
        public void Test()
        {
            for(int i = 0; i < testfiles.Length; i++)
            {
                string  location = Path.Combine(Consts.TestFilesRoot, "filesystems", "fat16_human", testfiles[i]);
                IFilter filter   = new LZip();
                filter.Open(location);
                IMediaImage image = new ZZZRawImage();
                Assert.AreEqual(true,          image.Open(filter),    testfiles[i]);
                Assert.AreEqual(sectors[i],    image.Info.Sectors,    testfiles[i]);
                Assert.AreEqual(sectorsize[i], image.Info.SectorSize, testfiles[i]);
                List<Partition> partitions = Core.Partitions.GetAll(image);
                IFilesystem     fs         = new FAT();
                int             part       = -1;
                for(int j = 0; j < partitions.Count; j++)
                    if(partitions[j].Type == "Human68k")
                    {
                        part = j;
                        break;
                    }

                Assert.AreNotEqual(-1, part, $"Partition not found on {testfiles[i]}");
                Assert.AreEqual(true, fs.Identify(image, partitions[part]), testfiles[i]);
                fs.GetInformation(image, partitions[part], out _, null);
                Assert.AreEqual(clusters[i],     fs.XmlFsType.Clusters,         testfiles[i]);
                Assert.AreEqual(clustersize[i],  fs.XmlFsType.ClusterSize,      testfiles[i]);
                Assert.AreEqual("FAT16",         fs.XmlFsType.Type,             testfiles[i]);
                Assert.AreEqual(volumename[i],   fs.XmlFsType.VolumeName,       testfiles[i]);
                Assert.AreEqual(volumeserial[i], fs.XmlFsType.VolumeSerial,     testfiles[i]);
                Assert.AreEqual(oemid[i],        fs.XmlFsType.SystemIdentifier, testfiles[i]);
            }
        }
    }
}