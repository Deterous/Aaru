// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : Super.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple Lisa filesystem plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Handles mounting and umounting the Apple Lisa filesystem.
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
using Claunia.Encoding;
using DiscImageChef.CommonTypes;
using DiscImageChef.CommonTypes.Enums;
using DiscImageChef.CommonTypes.Interfaces;
using DiscImageChef.CommonTypes.Structs;
using DiscImageChef.Console;
using DiscImageChef.Decoders;
using Schemas;
using Encoding = System.Text.Encoding;

namespace DiscImageChef.Filesystems.LisaFS
{
    public partial class LisaFS
    {
        /// <summary>
        ///     Mounts an Apple Lisa filesystem
        /// </summary>
        public Errno Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                           Dictionary<string, string> options,     string    @namespace)
        {
            try
            {
                device   = imagePlugin;
                Encoding = new LisaRoman();

                // Lisa OS is unable to work on disks without tags.
                // This code is designed like that.
                // However with some effort the code may be modified to ignore them.
                if(device.Info.ReadableSectorTags == null ||
                   !device.Info.ReadableSectorTags.Contains(SectorTagType.AppleSectorTag))
                {
                    DicConsole.DebugWriteLine("LisaFS plugin", "Underlying device does not support Lisa tags");
                    return Errno.InOutError;
                }

                // Minimal LisaOS disk is 3.5" single sided double density, 800 sectors
                if(device.Info.Sectors < 800)
                {
                    DicConsole.DebugWriteLine("LisaFS plugin", "Device is too small");
                    return Errno.InOutError;
                }

                // MDDF cannot be at end of device, of course
                volumePrefix = device.Info.Sectors;

                // LisaOS searches sectors until tag tells MDDF resides there, so we'll search 100 sectors
                for(ulong i = 0; i < 100; i++)
                {
                    DecodeTag(device.ReadSectorTag(i, SectorTagType.AppleSectorTag), out LisaTag.PriamTag searchTag);

                    DicConsole.DebugWriteLine("LisaFS plugin", "Sector {0}, file ID 0x{1:X4}", i, searchTag.FileId);

                    if(volumePrefix == device.Info.Sectors && searchTag.FileId == FILEID_LOADER_SIGNED)
                        volumePrefix = i - 1;

                    if(searchTag.FileId != FILEID_MDDF) continue;

                    devTagSize = device.ReadSectorTag(i, SectorTagType.AppleSectorTag).Length;

                    byte[] sector = device.ReadSector(i);
                    mddf = new MDDF();
                    byte[] pString = new byte[33];

                    mddf.fsversion = BigEndianBitConverter.ToUInt16(sector, 0x00);
                    mddf.volid     = BigEndianBitConverter.ToUInt64(sector, 0x02);
                    mddf.volnum    = BigEndianBitConverter.ToUInt16(sector, 0x0A);
                    Array.Copy(sector, 0x0C, pString, 0, 33);
                    mddf.volname  = StringHandlers.PascalToString(pString, Encoding);
                    mddf.unknown1 = sector[0x2D];
                    Array.Copy(sector, 0x2E, pString, 0, 33);
                    // Prevent garbage
                    mddf.password       = pString[0] <= 32 ? StringHandlers.PascalToString(pString, Encoding) : "";
                    mddf.unknown2       = sector[0x4F];
                    mddf.machine_id     = BigEndianBitConverter.ToUInt32(sector, 0x50);
                    mddf.master_copy_id = BigEndianBitConverter.ToUInt32(sector, 0x54);
                    uint lisaTime = BigEndianBitConverter.ToUInt32(sector, 0x58);
                    mddf.dtvc                         = DateHandlers.LisaToDateTime(lisaTime);
                    lisaTime                          = BigEndianBitConverter.ToUInt32(sector, 0x5C);
                    mddf.dtcc                         = DateHandlers.LisaToDateTime(lisaTime);
                    lisaTime                          = BigEndianBitConverter.ToUInt32(sector, 0x60);
                    mddf.dtvb                         = DateHandlers.LisaToDateTime(lisaTime);
                    lisaTime                          = BigEndianBitConverter.ToUInt32(sector, 0x64);
                    mddf.dtvs                         = DateHandlers.LisaToDateTime(lisaTime);
                    mddf.unknown3                     = BigEndianBitConverter.ToUInt32(sector, 0x68);
                    mddf.mddf_block                   = BigEndianBitConverter.ToUInt32(sector, 0x6C);
                    mddf.volsize_minus_one            = BigEndianBitConverter.ToUInt32(sector, 0x70);
                    mddf.volsize_minus_mddf_minus_one = BigEndianBitConverter.ToUInt32(sector, 0x74);
                    mddf.vol_size                     = BigEndianBitConverter.ToUInt32(sector, 0x78);
                    mddf.blocksize                    = BigEndianBitConverter.ToUInt16(sector, 0x7C);
                    mddf.datasize                     = BigEndianBitConverter.ToUInt16(sector, 0x7E);
                    mddf.unknown4                     = BigEndianBitConverter.ToUInt16(sector, 0x80);
                    mddf.unknown5                     = BigEndianBitConverter.ToUInt32(sector, 0x82);
                    mddf.unknown6                     = BigEndianBitConverter.ToUInt32(sector, 0x86);
                    mddf.clustersize                  = BigEndianBitConverter.ToUInt16(sector, 0x8A);
                    mddf.fs_size                      = BigEndianBitConverter.ToUInt32(sector, 0x8C);
                    mddf.unknown7                     = BigEndianBitConverter.ToUInt32(sector, 0x90);
                    mddf.srec_ptr                     = BigEndianBitConverter.ToUInt32(sector, 0x94);
                    mddf.unknown9                     = BigEndianBitConverter.ToUInt16(sector, 0x98);
                    mddf.srec_len                     = BigEndianBitConverter.ToUInt16(sector, 0x9A);
                    mddf.unknown10                    = BigEndianBitConverter.ToUInt32(sector, 0x9C);
                    mddf.unknown11                    = BigEndianBitConverter.ToUInt32(sector, 0xA0);
                    mddf.unknown12                    = BigEndianBitConverter.ToUInt32(sector, 0xA4);
                    mddf.unknown13                    = BigEndianBitConverter.ToUInt32(sector, 0xA8);
                    mddf.unknown14                    = BigEndianBitConverter.ToUInt32(sector, 0xAC);
                    mddf.filecount                    = BigEndianBitConverter.ToUInt16(sector, 0xB0);
                    mddf.unknown15                    = BigEndianBitConverter.ToUInt32(sector, 0xB2);
                    mddf.unknown16                    = BigEndianBitConverter.ToUInt32(sector, 0xB6);
                    mddf.freecount                    = BigEndianBitConverter.ToUInt32(sector, 0xBA);
                    mddf.unknown17                    = BigEndianBitConverter.ToUInt16(sector, 0xBE);
                    mddf.unknown18                    = BigEndianBitConverter.ToUInt32(sector, 0xC0);
                    mddf.overmount_stamp              = BigEndianBitConverter.ToUInt64(sector, 0xC4);
                    mddf.serialization                = BigEndianBitConverter.ToUInt32(sector, 0xCC);
                    mddf.unknown19                    = BigEndianBitConverter.ToUInt32(sector, 0xD0);
                    mddf.unknown_timestamp            = BigEndianBitConverter.ToUInt32(sector, 0xD4);
                    mddf.unknown20                    = BigEndianBitConverter.ToUInt32(sector, 0xD8);
                    mddf.unknown21                    = BigEndianBitConverter.ToUInt32(sector, 0xDC);
                    mddf.unknown22                    = BigEndianBitConverter.ToUInt32(sector, 0xE0);
                    mddf.unknown23                    = BigEndianBitConverter.ToUInt32(sector, 0xE4);
                    mddf.unknown24                    = BigEndianBitConverter.ToUInt32(sector, 0xE8);
                    mddf.unknown25                    = BigEndianBitConverter.ToUInt32(sector, 0xEC);
                    mddf.unknown26                    = BigEndianBitConverter.ToUInt32(sector, 0xF0);
                    mddf.unknown27                    = BigEndianBitConverter.ToUInt32(sector, 0xF4);
                    mddf.unknown28                    = BigEndianBitConverter.ToUInt32(sector, 0xF8);
                    mddf.unknown29                    = BigEndianBitConverter.ToUInt32(sector, 0xFC);
                    mddf.unknown30                    = BigEndianBitConverter.ToUInt32(sector, 0x100);
                    mddf.unknown31                    = BigEndianBitConverter.ToUInt32(sector, 0x104);
                    mddf.unknown32                    = BigEndianBitConverter.ToUInt32(sector, 0x108);
                    mddf.unknown33                    = BigEndianBitConverter.ToUInt32(sector, 0x10C);
                    mddf.unknown34                    = BigEndianBitConverter.ToUInt32(sector, 0x110);
                    mddf.unknown35                    = BigEndianBitConverter.ToUInt32(sector, 0x114);
                    mddf.backup_volid                 = BigEndianBitConverter.ToUInt64(sector, 0x118);
                    mddf.label_size                   = BigEndianBitConverter.ToUInt16(sector, 0x120);
                    mddf.fs_overhead                  = BigEndianBitConverter.ToUInt16(sector, 0x122);
                    mddf.result_scavenge              = BigEndianBitConverter.ToUInt16(sector, 0x124);
                    mddf.boot_code                    = BigEndianBitConverter.ToUInt16(sector, 0x126);
                    mddf.boot_environ                 = BigEndianBitConverter.ToUInt16(sector, 0x6C);
                    mddf.unknown36                    = BigEndianBitConverter.ToUInt32(sector, 0x12A);
                    mddf.unknown37                    = BigEndianBitConverter.ToUInt32(sector, 0x12E);
                    mddf.unknown38                    = BigEndianBitConverter.ToUInt32(sector, 0x132);
                    mddf.vol_sequence                 = BigEndianBitConverter.ToUInt16(sector, 0x136);
                    mddf.vol_left_mounted             = sector[0x138];

                    // Check that the MDDF is correct
                    if(mddf.mddf_block != i - volumePrefix   ||
                       mddf.vol_size   > device.Info.Sectors ||
                       mddf.vol_size - 1 !=
                       mddf.volsize_minus_one ||
                       mddf.vol_size - i                 - 1 !=
                       mddf.volsize_minus_mddf_minus_one - volumePrefix ||
                       mddf.datasize >
                       mddf.blocksize || mddf.blocksize < device.Info.SectorSize ||
                       mddf.datasize                    != device.Info.SectorSize)
                    {
                        DicConsole.DebugWriteLine("LisaFS plugin", "Incorrect MDDF found");
                        return Errno.InvalidArgument;
                    }

                    // Check MDDF version
                    switch(mddf.fsversion)
                    {
                        case LISA_V1:
                            DicConsole.DebugWriteLine("LisaFS plugin", "Mounting LisaFS v1");
                            break;
                        case LISA_V2:
                            DicConsole.DebugWriteLine("LisaFS plugin", "Mounting LisaFS v2");
                            break;
                        case LISA_V3:
                            DicConsole.DebugWriteLine("LisaFS plugin", "Mounting LisaFS v3");
                            break;
                        default:
                            DicConsole.ErrorWriteLine("Cannot mount LisaFS version {0}", mddf.fsversion.ToString());
                            return Errno.NotSupported;
                    }

                    // Initialize caches
                    extentCache     = new Dictionary<short, ExtentFile>();
                    systemFileCache = new Dictionary<short, byte[]>();
                    fileCache       = new Dictionary<short, byte[]>();
                    //catalogCache = new Dictionary<short, List<CatalogEntry>>();
                    fileSizeCache = new Dictionary<short, int>();

                    mounted = true;
                    if(options == null) options = GetDefaultOptions();
                    if(options.TryGetValue("debug", out string debugString)) bool.TryParse(debugString, out debug);

                    if(debug) printedExtents = new List<short>();

                    // Read the S-Records file
                    Errno error = ReadSRecords();
                    if(error != Errno.NoError)
                    {
                        DicConsole.ErrorWriteLine("Error {0} reading S-Records file.", error);
                        return error;
                    }

                    directoryDtcCache = new Dictionary<short, DateTime> {{DIRID_ROOT, mddf.dtcc}};

                    // Read the Catalog File
                    error = ReadCatalog();

                    if(error != Errno.NoError)
                    {
                        DicConsole.DebugWriteLine("LisaFS plugin", "Cannot read Catalog File, error {0}",
                                                  error.ToString());
                        mounted = false;
                        return error;
                    }

                    // If debug, cache system files
                    if(debug)
                    {
                        error = ReadSystemFile(FILEID_BOOT_SIGNED, out _);
                        if(error != Errno.NoError)
                        {
                            DicConsole.DebugWriteLine("LisaFS plugin", "Unable to read boot blocks");
                            mounted = false;
                            return error;
                        }

                        error = ReadSystemFile(FILEID_LOADER_SIGNED, out _);
                        if(error != Errno.NoError)
                        {
                            DicConsole.DebugWriteLine("LisaFS plugin", "Unable to read boot loader");
                            mounted = false;
                            return error;
                        }

                        error = ReadSystemFile((short)FILEID_MDDF, out _);
                        if(error != Errno.NoError)
                        {
                            DicConsole.DebugWriteLine("LisaFS plugin", "Unable to read MDDF");
                            mounted = false;
                            return error;
                        }

                        error = ReadSystemFile((short)FILEID_BITMAP, out _);
                        if(error != Errno.NoError)
                        {
                            DicConsole.DebugWriteLine("LisaFS plugin", "Unable to read volume bitmap");
                            mounted = false;
                            return error;
                        }

                        error = ReadSystemFile((short)FILEID_SRECORD, out _);
                        if(error != Errno.NoError)
                        {
                            DicConsole.DebugWriteLine("LisaFS plugin", "Unable to read S-Records file");
                            mounted = false;
                            return error;
                        }
                    }

                    // Create XML metadata for mounted filesystem
                    XmlFsType = new FileSystemType();
                    if(DateTime.Compare(mddf.dtvb, DateHandlers.LisaToDateTime(0)) > 0)
                    {
                        XmlFsType.BackupDate          = mddf.dtvb;
                        XmlFsType.BackupDateSpecified = true;
                    }

                    XmlFsType.Clusters    = mddf.vol_size;
                    XmlFsType.ClusterSize = (uint)(mddf.clustersize * mddf.datasize);
                    if(DateTime.Compare(mddf.dtvc, DateHandlers.LisaToDateTime(0)) > 0)
                    {
                        XmlFsType.CreationDate          = mddf.dtvc;
                        XmlFsType.CreationDateSpecified = true;
                    }

                    XmlFsType.Dirty                 = mddf.vol_left_mounted != 0;
                    XmlFsType.Files                 = mddf.filecount;
                    XmlFsType.FilesSpecified        = true;
                    XmlFsType.FreeClusters          = mddf.freecount;
                    XmlFsType.FreeClustersSpecified = true;
                    XmlFsType.Type                  = "LisaFS";
                    XmlFsType.VolumeName            = mddf.volname;
                    XmlFsType.VolumeSerial          = $"{mddf.volid:X16}";

                    return Errno.NoError;
                }

                DicConsole.DebugWriteLine("LisaFS plugin", "Not a Lisa filesystem");
                return Errno.NotSupported;
            }
            catch(Exception ex)
            {
                DicConsole.ErrorWriteLine("Exception {0}, {1}, {2}", ex.Message, ex.InnerException, ex.StackTrace);
                return Errno.InOutError;
            }
        }

        /// <summary>
        ///     Umounts this Lisa filesystem
        /// </summary>
        public Errno Unmount()
        {
            mounted         = false;
            extentCache     = null;
            systemFileCache = null;
            fileCache       = null;
            catalogCache    = null;
            fileSizeCache   = null;
            printedExtents  = null;
            mddf            = new MDDF();
            volumePrefix    = 0;
            devTagSize      = 0;
            srecords        = null;

            return Errno.NoError;
        }

        /// <summary>
        ///     Gets information about the mounted volume.
        /// </summary>
        /// <param name="stat">Information about the mounted volume.</param>
        public Errno StatFs(out FileSystemInfo stat)
        {
            stat = null;
            if(!mounted) return Errno.AccessDenied;

            stat = new FileSystemInfo
            {
                Blocks         = mddf.vol_size,
                FilenameLength = (ushort)E_NAME,
                Files          = mddf.filecount,
                FreeBlocks     = mddf.freecount,
                Id             = {Serial64 = mddf.volid, IsLong = true},
                PluginId       = Id
            };
            stat.FreeFiles = FILEID_MAX - stat.Files;
            switch(mddf.fsversion)
            {
                case LISA_V1:
                    stat.Type = "LisaFS v1";
                    break;
                case LISA_V2:
                    stat.Type = "LisaFS v2";
                    break;
                case LISA_V3:
                    stat.Type = "LisaFS v3";
                    break;
            }

            return Errno.NoError;
        }
    }
}