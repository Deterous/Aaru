﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : Write.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Writes Connectix and Microsoft Virtual PC disk images.
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
using System.IO;
using System.Linq;
using DiscImageChef.CommonTypes;
using DiscImageChef.CommonTypes.Enums;
using DiscImageChef.CommonTypes.Interop;
using DiscImageChef.CommonTypes.Structs;
using Schemas;
using PlatformID = DiscImageChef.CommonTypes.Interop.PlatformID;
using Version = System.Version;

namespace DiscImageChef.DiscImages
{
    public partial class Vhd
    {
        public bool Create(string path, MediaType mediaType, Dictionary<string, string> options, ulong sectors,
                           uint   sectorSize)
        {
            if(sectorSize != 512)
            {
                ErrorMessage = "Unsupported sector size";
                return false;
            }

            if(!SupportedMediaTypes.Contains(mediaType))
            {
                ErrorMessage = $"Unsupport media format {mediaType}";
                return false;
            }

            imageInfo = new ImageInfo {MediaType = mediaType, SectorSize = sectorSize, Sectors = sectors};

            try { writingStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch(IOException e)
            {
                ErrorMessage = $"Could not create new image file, exception {e.Message}";
                return false;
            }

            IsWriting    = true;
            ErrorMessage = null;
            return true;
        }

        public bool WriteMediaTag(byte[] data, MediaTagType tag)
        {
            ErrorMessage = "Writing media tags is not supported.";
            return false;
        }

        public bool WriteSector(byte[] data, ulong sectorAddress)
        {
            if(!IsWriting)
            {
                ErrorMessage = "Tried to write on a non-writable image";
                return false;
            }

            if(data.Length != 512)
            {
                ErrorMessage = "Incorrect data size";
                return false;
            }

            if(sectorAddress >= imageInfo.Sectors)
            {
                ErrorMessage = "Tried to write past image size";
                return false;
            }

            writingStream.Seek((long)(0 + sectorAddress * 512), SeekOrigin.Begin);
            writingStream.Write(data, 0, data.Length);

            ErrorMessage = "";
            return true;
        }

        // TODO: Implement dynamic
        public bool WriteSectors(byte[] data, ulong sectorAddress, uint length)
        {
            if(!IsWriting)
            {
                ErrorMessage = "Tried to write on a non-writable image";
                return false;
            }

            if(data.Length % 512 != 0)
            {
                ErrorMessage = "Incorrect data size";
                return false;
            }

            if(sectorAddress + length > imageInfo.Sectors)
            {
                ErrorMessage = "Tried to write past image size";
                return false;
            }

            writingStream.Seek((long)(0 + sectorAddress * 512), SeekOrigin.Begin);
            writingStream.Write(data, 0, data.Length);

            ErrorMessage = "";
            return true;
        }

        public bool WriteSectorLong(byte[] data, ulong sectorAddress)
        {
            ErrorMessage = "Writing sectors with tags is not supported.";
            return false;
        }

        public bool WriteSectorsLong(byte[] data, ulong sectorAddress, uint length)
        {
            ErrorMessage = "Writing sectors with tags is not supported.";
            return false;
        }

        public bool Close()
        {
            if(!IsWriting)
            {
                ErrorMessage = "Image is not opened for writing";
                return false;
            }

            Version thisVersion = GetType().Assembly.GetName().Version;

            if(imageInfo.Cylinders == 0)
            {
                imageInfo.Cylinders       = (uint)(imageInfo.Sectors / 16 / 63);
                imageInfo.Heads           = 16;
                imageInfo.SectorsPerTrack = 63;

                while(imageInfo.Cylinders == 0)
                {
                    imageInfo.Heads--;

                    if(imageInfo.Heads == 0)
                    {
                        imageInfo.SectorsPerTrack--;
                        imageInfo.Heads = 16;
                    }

                    imageInfo.Cylinders = (uint)(imageInfo.Sectors / imageInfo.Heads / imageInfo.SectorsPerTrack);

                    if(imageInfo.Cylinders == 0 && imageInfo.Heads == 0 && imageInfo.SectorsPerTrack == 0) break;
                }
            }

            HardDiskFooter footer = new HardDiskFooter
            {
                Cookie   = IMAGE_COOKIE,
                Features = FEATURES_RESERVED,
                Version  = VERSION1,
                Timestamp =
                    (uint)(DateTime.Now - new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds,
                CreatorApplication = CREATOR_DISCIMAGECHEF,
                CreatorVersion =
                    (uint)(((thisVersion.Major & 0xFF) << 24) + ((thisVersion.Minor & 0xFF) << 16) +
                           ((thisVersion.Build & 0xFF) << 8)  + (thisVersion.Revision & 0xFF)),
                CreatorHostOs =
                    DetectOS.GetRealPlatformID() == PlatformID.MacOSX ? CREATOR_MACINTOSH : CREATOR_WINDOWS,
                DiskType = TYPE_FIXED,
                UniqueId = Guid.NewGuid(),
                DiskGeometry =
                    ((imageInfo.Cylinders & 0xFFFF) << 16) + ((imageInfo.Heads & 0xFF) << 8) +
                    (imageInfo.SectorsPerTrack & 0xFF),
                OriginalSize = imageInfo.Sectors * 512,
                CurrentSize  = imageInfo.Sectors * 512
            };
            footer.Offset = footer.DiskType == TYPE_FIXED ? ulong.MaxValue : 512;

            byte[] footerBytes = new byte[512];
            Array.Copy(BigEndianBitConverter.GetBytes(footer.Cookie),             0, footerBytes, 0x00, 8);
            Array.Copy(BigEndianBitConverter.GetBytes(footer.Features),           0, footerBytes, 0x08, 4);
            Array.Copy(BigEndianBitConverter.GetBytes(footer.Version),            0, footerBytes, 0x0C, 4);
            Array.Copy(BigEndianBitConverter.GetBytes(footer.Offset),             0, footerBytes, 0x10, 8);
            Array.Copy(BigEndianBitConverter.GetBytes(footer.Timestamp),          0, footerBytes, 0x18, 4);
            Array.Copy(BigEndianBitConverter.GetBytes(footer.CreatorApplication), 0, footerBytes, 0x1C, 4);
            Array.Copy(BigEndianBitConverter.GetBytes(footer.CreatorVersion),     0, footerBytes, 0x20, 4);
            Array.Copy(BigEndianBitConverter.GetBytes(footer.CreatorHostOs),      0, footerBytes, 0x24, 4);
            Array.Copy(BigEndianBitConverter.GetBytes(footer.OriginalSize),       0, footerBytes, 0x28, 8);
            Array.Copy(BigEndianBitConverter.GetBytes(footer.CurrentSize),        0, footerBytes, 0x30, 8);
            Array.Copy(BigEndianBitConverter.GetBytes(footer.DiskGeometry),       0, footerBytes, 0x38, 4);
            Array.Copy(BigEndianBitConverter.GetBytes(footer.DiskType),           0, footerBytes, 0x3C, 4);
            Array.Copy(footer.UniqueId.ToByteArray(),                             0, footerBytes, 0x44, 4);

            footer.Checksum = VhdChecksum(footerBytes);
            Array.Copy(BigEndianBitConverter.GetBytes(footer.Checksum), 0, footerBytes, 0x40, 4);

            writingStream.Seek((long)(footer.DiskType == TYPE_FIXED ? footer.OriginalSize : 0), SeekOrigin.Begin);
            writingStream.Write(footerBytes, 0, 512);

            writingStream.Flush();
            writingStream.Close();

            IsWriting    = false;
            ErrorMessage = "";
            return true;
        }

        public bool SetMetadata(ImageInfo metadata) => true;

        public bool SetGeometry(uint cylinders, uint heads, uint sectorsPerTrack)
        {
            if(cylinders > 0xFFFF)
            {
                ErrorMessage = "Too many cylinders.";
                return false;
            }

            if(heads > 0xFF)
            {
                ErrorMessage = "Too many heads.";
                return false;
            }

            if(sectorsPerTrack > 0xFF)
            {
                ErrorMessage = "Too many sectors per track.";
                return false;
            }

            imageInfo.SectorsPerTrack = sectorsPerTrack;
            imageInfo.Heads           = heads;
            imageInfo.Cylinders       = cylinders;

            return true;
        }

        public bool WriteSectorTag(byte[] data, ulong sectorAddress, SectorTagType tag)
        {
            ErrorMessage = "Unsupported feature";
            return false;
        }

        public bool WriteSectorsTag(byte[] data, ulong sectorAddress, uint length, SectorTagType tag)
        {
            ErrorMessage = "Unsupported feature";
            return false;
        }

        public bool SetDumpHardware(List<DumpHardwareType> dumpHardware) => false;

        public bool SetCicmMetadata(CICMMetadataType metadata) => false;
    }
}