// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : Super.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple DOS filesystem plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Handles mounting and umounting the Apple DOS filesystem.
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

using System.Collections.Generic;
using Claunia.Encoding;
using DiscImageChef.CommonTypes;
using DiscImageChef.CommonTypes.Interfaces;
using DiscImageChef.CommonTypes.Structs;
using DiscImageChef.Console;
using DiscImageChef.Helpers;
using Schemas;
using Encoding = System.Text.Encoding;

namespace DiscImageChef.Filesystems.AppleDOS
{
    public partial class AppleDOS
    {
        /// <summary>
        ///     Mounts an Apple DOS filesystem
        /// </summary>
        public Errno Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                           Dictionary<string, string> options,     string    @namespace)
        {
            device   = imagePlugin;
            start    = partition.Start;
            Encoding = encoding ?? new Apple2();

            if(device.Info.Sectors != 455 && device.Info.Sectors != 560)
            {
                DicConsole.DebugWriteLine("Apple DOS plugin", "Incorrect device size.");
                return Errno.InOutError;
            }

            if(start > 0)
            {
                DicConsole.DebugWriteLine("Apple DOS plugin", "Partitions are not supported.");
                return Errno.InOutError;
            }

            if(device.Info.SectorSize != 256)
            {
                DicConsole.DebugWriteLine("Apple DOS plugin", "Incorrect sector size.");
                return Errno.InOutError;
            }

            sectorsPerTrack = device.Info.Sectors == 455 ? 13 : 16;

            // Read the VTOC
            vtocBlocks = device.ReadSector((ulong)(17 * sectorsPerTrack));
            vtoc       = Marshal.ByteArrayToStructureLittleEndian<Vtoc>(vtocBlocks);

            track1UsedByFiles = false;
            track2UsedByFiles = false;
            usedSectors       = 1;

            Errno error = ReadCatalog();
            if(error != Errno.NoError)
            {
                DicConsole.DebugWriteLine("Apple DOS plugin", "Unable to read catalog.");
                return error;
            }

            error = CacheAllFiles();
            if(error != Errno.NoError)
            {
                DicConsole.DebugWriteLine("Apple DOS plugin", "Unable cache all files.");
                return error;
            }

            // Create XML metadata for mounted filesystem
            XmlFsType = new FileSystemType
            {
                Bootable              = true,
                Clusters              = device.Info.Sectors,
                ClusterSize           = vtoc.bytesPerSector,
                Files                 = (ulong)catalogCache.Count,
                FilesSpecified        = true,
                FreeClustersSpecified = true,
                Type                  = "Apple DOS"
            };
            XmlFsType.FreeClusters = XmlFsType.Clusters - usedSectors;

            if(options == null) options = GetDefaultOptions();
            if(options.TryGetValue("debug", out string debugString)) bool.TryParse(debugString, out debug);
            mounted = true;
            return Errno.NoError;
        }

        /// <summary>
        ///     Umounts this DOS filesystem
        /// </summary>
        public Errno Unmount()
        {
            mounted       = false;
            extentCache   = null;
            fileCache     = null;
            catalogCache  = null;
            fileSizeCache = null;

            return Errno.NoError;
        }

        /// <summary>
        ///     Gets information about the mounted volume.
        /// </summary>
        /// <param name="stat">Information about the mounted volume.</param>
        public Errno StatFs(out FileSystemInfo stat)
        {
            stat = new FileSystemInfo
            {
                Blocks         = device.Info.Sectors,
                FilenameLength = 30,
                Files          = (ulong)catalogCache.Count,
                PluginId       = Id,
                Type           = "Apple DOS"
            };
            stat.FreeFiles  = totalFileEntries - stat.Files;
            stat.FreeBlocks = stat.Blocks      - usedSectors;

            return Errno.NoError;
        }
    }
}