﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : CisCopy.cs
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

using System.IO;
using DiscImageChef.Checksums;
using DiscImageChef.CommonTypes;
using DiscImageChef.CommonTypes.Interfaces;
using DiscImageChef.Filters;
using NUnit.Framework;

namespace DiscImageChef.Tests.Images
{
    [TestFixture]
    public class CisCopy
    {
        // TODO: Support compression
        readonly string[] testfiles =
        {
            "md1dd8_all.dcf.lz", "md1dd8_belelung.dcf.lz", "md1dd8_fat.dcf.lz", "md1dd_all.dcf.lz",
            "md1dd_belelung.dcf.lz", "md1dd_fat.dcf.lz", "md2dd8_all.dcf.lz", "md2dd8_belelung.dcf.lz",
            "md2dd8_fat.dcf.lz", "md2dd_all.dcf.lz", "md2dd_belelung.dcf.lz", "md2dd_fat.dcf.lz",
            "md2hd_all.dcf.lz", "md2hd_belelung.dcf.lz", "md2hd_fat.dcf.lz", "mf2dd_all.dcf.lz",
            "mf2dd_belelung.dcf.lz", "mf2dd_fat.dcf.lz", "mf2hd_all.dcf.lz", "mf2hd_belelung.dcf.lz",
            "mf2hd_fat.dcf.lz"
        };

        readonly ulong[] sectors =
        {
            320, 320, 320, 360, 360, 360, 640, 640, 640, 720, 720, 720, 2400, 2400, 2400, 1440, 1440, 1440, 2880,
            2880, 2880
        };

        readonly uint[] sectorsize =
        {
            512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512, 512
        };

        readonly MediaType[] mediatypes =
        {
            MediaType.DOS_525_SS_DD_8, MediaType.DOS_525_SS_DD_8, MediaType.DOS_525_SS_DD_8,
            MediaType.DOS_525_SS_DD_9, MediaType.DOS_525_SS_DD_9, MediaType.DOS_525_SS_DD_9,
            MediaType.DOS_525_DS_DD_8, MediaType.DOS_525_DS_DD_8, MediaType.DOS_525_DS_DD_8,
            MediaType.DOS_525_DS_DD_9, MediaType.DOS_525_DS_DD_9, MediaType.DOS_525_DS_DD_9, MediaType.DOS_525_HD,
            MediaType.DOS_525_HD, MediaType.DOS_525_HD, MediaType.DOS_35_DS_DD_9, MediaType.DOS_35_DS_DD_9,
            MediaType.DOS_35_DS_DD_9, MediaType.DOS_35_HD, MediaType.DOS_35_HD, MediaType.DOS_35_HD
        };

        readonly string[] md5S =
        {
            "95c0b76419c1c74db6dbe1d790f97dde", "95c0b76419c1c74db6dbe1d790f97dde",
            "6f6507e416b7320d583dc347b8e57844", "48b93e8619c4c13f4a3724b550e4b371",
            "48b93e8619c4c13f4a3724b550e4b371", "1d060d2e2543e1c2e8569f5451660060",
            "0c93155bbc5e412f5014e037d08c2745", "0c93155bbc5e412f5014e037d08c2745",
            "0c93155bbc5e412f5014e037d08c2745", "d2a33090ec03bfb536e7356deacf4bbc",
            "d2a33090ec03bfb536e7356deacf4bbc", "d2a33090ec03bfb536e7356deacf4bbc",
            "181f3bc62f0b90f74af9d8027ebf7512", "181f3bc62f0b90f74af9d8027ebf7512",
            "181f3bc62f0b90f74af9d8027ebf7512", "783559ee5e774515d5e7d2feab9c333e",
            "783559ee5e774515d5e7d2feab9c333e", "783559ee5e774515d5e7d2feab9c333e",
            "91f3fde8d56a536cdda4c6758e5dbc93", "91f3fde8d56a536cdda4c6758e5dbc93",
            "91f3fde8d56a536cdda4c6758e5dbc93"
        };

        [Test]
        public void Test()
        {
            for(int i = 0; i < testfiles.Length; i++)
            {
                string  location = Path.Combine(Consts.TestFilesRoot, "images", "ciscopy", testfiles[i]);
                IFilter filter   = new LZip();
                filter.Open(location);
                IMediaImage image = new DiscImages.CisCopy();
                Assert.AreEqual(true,          image.Open(filter),    testfiles[i]);
                Assert.AreEqual(sectors[i],    image.Info.Sectors,    testfiles[i]);
                Assert.AreEqual(sectorsize[i], image.Info.SectorSize, testfiles[i]);
                Assert.AreEqual(mediatypes[i], image.Info.MediaType,  testfiles[i]);

                // How many sectors to read at once
                const uint SECTORS_TO_READ = 256;
                ulong      doneSectors     = 0;

                Md5Context ctx = new Md5Context();

                while(doneSectors < image.Info.Sectors)
                {
                    byte[] sector;

                    if(image.Info.Sectors - doneSectors >= SECTORS_TO_READ)
                    {
                        sector      =  image.ReadSectors(doneSectors, SECTORS_TO_READ);
                        doneSectors += SECTORS_TO_READ;
                    }
                    else
                    {
                        sector      =  image.ReadSectors(doneSectors, (uint)(image.Info.Sectors - doneSectors));
                        doneSectors += image.Info.Sectors - doneSectors;
                    }

                    ctx.Update(sector);
                }

                Assert.AreEqual(md5S[i], ctx.End(), testfiles[i]);
            }
        }
    }
}