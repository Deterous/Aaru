﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : Read.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Reads NHD r0 disk images.
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
using System.IO;
using System.Text;
using DiscImageChef.CommonTypes;
using DiscImageChef.CommonTypes.Enums;
using DiscImageChef.CommonTypes.Interfaces;
using DiscImageChef.Helpers;

namespace DiscImageChef.DiscImages
{
    public partial class Nhdr0
    {
        public bool Open(IFilter imageFilter)
        {
            Stream stream = imageFilter.GetDataForkStream();
            stream.Seek(0, SeekOrigin.Begin);
            // Even if comment is supposedly ASCII, I'm pretty sure most emulators allow Shift-JIS to be used :p
            Encoding shiftjis = Encoding.GetEncoding("shift_jis");

            if(stream.Length < Marshal.SizeOf<Nhdr0Header>()) return false;

            byte[] hdrB = new byte[Marshal.SizeOf<Nhdr0Header>()];
            stream.Read(hdrB, 0, hdrB.Length);

            nhdhdr = Marshal.ByteArrayToStructureLittleEndian<Nhdr0Header>(hdrB);

            imageInfo.MediaType = MediaType.GENERIC_HDD;

            imageInfo.ImageSize            = (ulong)(stream.Length - nhdhdr.dwHeadSize);
            imageInfo.CreationTime         = imageFilter.GetCreationTime();
            imageInfo.LastModificationTime = imageFilter.GetLastWriteTime();
            imageInfo.MediaTitle           = Path.GetFileNameWithoutExtension(imageFilter.GetFilename());
            imageInfo.Sectors              = (ulong)(nhdhdr.dwCylinder * nhdhdr.wHead * nhdhdr.wSect);
            imageInfo.XmlMediaType         = XmlMediaType.BlockMedia;
            imageInfo.SectorSize           = (uint)nhdhdr.wSectLen;
            imageInfo.Cylinders            = (uint)nhdhdr.dwCylinder;
            imageInfo.Heads                = (uint)nhdhdr.wHead;
            imageInfo.SectorsPerTrack      = (uint)nhdhdr.wSect;
            imageInfo.Comments             = StringHandlers.CToString(nhdhdr.szComment, shiftjis);

            nhdImageFilter = imageFilter;

            return true;
        }

        public byte[] ReadSector(ulong sectorAddress) => ReadSectors(sectorAddress, 1);

        public byte[] ReadSectors(ulong sectorAddress, uint length)
        {
            if(sectorAddress > imageInfo.Sectors - 1)
                throw new ArgumentOutOfRangeException(nameof(sectorAddress), "Sector address not found");

            if(sectorAddress + length > imageInfo.Sectors)
                throw new ArgumentOutOfRangeException(nameof(length), "Requested more sectors than available");

            byte[] buffer = new byte[length * imageInfo.SectorSize];

            Stream stream = nhdImageFilter.GetDataForkStream();

            stream.Seek((long)((ulong)nhdhdr.dwHeadSize + sectorAddress * imageInfo.SectorSize), SeekOrigin.Begin);

            stream.Read(buffer, 0, (int)(length * imageInfo.SectorSize));

            return buffer;
        }
    }
}