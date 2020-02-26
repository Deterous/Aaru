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
//     Writes IBM SaveDskF disk images.
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
using System.Text;
using DiscImageChef.CommonTypes;
using DiscImageChef.CommonTypes.Enums;
using DiscImageChef.CommonTypes.Structs;
using DiscImageChef.Helpers;
using Schemas;

namespace DiscImageChef.DiscImages
{
    public partial class SaveDskF
    {
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

            if(data.Length != imageInfo.SectorSize)
            {
                ErrorMessage = "Incorrect data size";
                return false;
            }

            if(sectorAddress >= imageInfo.Sectors)
            {
                ErrorMessage = "Tried to write past image size";
                return false;
            }

            writingStream.Seek((long)(512 + sectorAddress * imageInfo.SectorSize), SeekOrigin.Begin);
            writingStream.Write(data, 0, data.Length);

            ErrorMessage = "";
            return true;
        }

        public bool WriteSectors(byte[] data, ulong sectorAddress, uint length)
        {
            if(!IsWriting)
            {
                ErrorMessage = "Tried to write on a non-writable image";
                return false;
            }

            if(data.Length % imageInfo.SectorSize != 0)
            {
                ErrorMessage = "Incorrect data size";
                return false;
            }

            if(sectorAddress + length > imageInfo.Sectors)
            {
                ErrorMessage = "Tried to write past image size";
                return false;
            }

            writingStream.Seek((long)(512 + sectorAddress * imageInfo.SectorSize), SeekOrigin.Begin);
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

            if(!string.IsNullOrWhiteSpace(imageInfo.Comments))
            {
                byte[] commentsBytes = Encoding.GetEncoding("ibm437").GetBytes(imageInfo.Comments);
                header.commentOffset = (ushort)Marshal.SizeOf<SaveDskFHeader>();
                writingStream.Seek(header.commentOffset, SeekOrigin.Begin);
                writingStream.Write(commentsBytes, 0,
                                    commentsBytes.Length >= 512 - header.commentOffset
                                        ? 512 - header.commentOffset
                                        : commentsBytes.Length);
            }

            byte[] hdr    = new byte[Marshal.SizeOf<SaveDskFHeader>()];
            IntPtr hdrPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(Marshal.SizeOf<SaveDskFHeader>());
            System.Runtime.InteropServices.Marshal.StructureToPtr(header, hdrPtr, true);
            System.Runtime.InteropServices.Marshal.Copy(hdrPtr, hdr, 0, hdr.Length);
            System.Runtime.InteropServices.Marshal.FreeHGlobal(hdrPtr);

            writingStream.Seek(0, SeekOrigin.Begin);
            writingStream.Write(hdr, 0, hdr.Length);

            header.checksum = 0;
            writingStream.Seek(0, SeekOrigin.Begin);

            int b;
            do
            {
                b = writingStream.ReadByte();
                if(b >= 0) header.checksum += (uint)b;
            }
            while(b >= 0);

            hdr    = new byte[Marshal.SizeOf<SaveDskFHeader>()];
            hdrPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(Marshal.SizeOf<SaveDskFHeader>());
            System.Runtime.InteropServices.Marshal.StructureToPtr(header, hdrPtr, true);
            System.Runtime.InteropServices.Marshal.Copy(hdrPtr, hdr, 0, hdr.Length);
            System.Runtime.InteropServices.Marshal.FreeHGlobal(hdrPtr);

            writingStream.Seek(0, SeekOrigin.Begin);
            writingStream.Write(hdr, 0, hdr.Length);

            writingStream.Flush();
            writingStream.Close();

            IsWriting    = false;
            ErrorMessage = "";
            return true;
        }

        public bool SetMetadata(ImageInfo metadata)
        {
            imageInfo.Comments = metadata.Comments;
            return true;
        }

        public bool Create(string path, MediaType mediaType, Dictionary<string, string> options, ulong sectors,
                           uint   sectorSize)
        {
            if(sectorSize == 0)
            {
                ErrorMessage = "Unsupported sector size";
                return false;
            }

            if(sectors > ushort.MaxValue)
            {
                ErrorMessage = "Too many sectors";
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

            (ushort cylinders, byte heads, ushort sectorsPerTrack, uint bytesPerSector, MediaEncoding encoding, bool
                variableSectorsPerTrack, MediaType type) geometry = Geometry.GetGeometry(mediaType);

            header = new SaveDskFHeader
            {
                cylinders       = geometry.cylinders,
                dataOffset      = 512,
                heads           = geometry.heads,
                magic           = SDF_MAGIC,
                sectorsCopied   = (ushort)sectors,
                sectorsPerTrack = geometry.sectorsPerTrack,
                sectorSize      = (ushort)sectorSize
            };

            IsWriting    = true;
            ErrorMessage = null;
            return true;
        }

        public bool SetGeometry(uint cylinders, uint heads, uint sectorsPerTrack) => true;

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