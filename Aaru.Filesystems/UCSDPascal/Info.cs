﻿// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Info.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : U.C.S.D. Pascal filesystem plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies the U.C.S.D. Pascal filesystem and shows information.
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
// Copyright © 2011-2024 Natalia Portillo
// ****************************************************************************/

using System;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Console;
using Aaru.Helpers;
using Claunia.Encoding;
using Encoding = System.Text.Encoding;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

// Information from Call-A.P.P.L.E. Pascal Disk Directory Structure
public sealed partial class PascalPlugin
{
#region IReadOnlyFilesystem Members

    /// <inheritdoc />
    public bool Identify(IMediaImage imagePlugin, Partition partition)
    {
        if(partition.Length < 3) return false;

        _multiplier = (uint)(imagePlugin.Info.SectorSize == 256 ? 2 : 1);

        // Blocks 0 and 1 are boot code
        ErrorNumber errno =
            imagePlugin.ReadSectors(_multiplier * 2 + partition.Start, _multiplier, out byte[] volBlock);

        if(errno != ErrorNumber.NoError) return false;

        // On Apple II, it's little endian
        // TODO: Fix
        /*BigEndianBitConverter.IsLittleEndian =
            multiplier == 2 ? !BitConverter.IsLittleEndian : BitConverter.IsLittleEndian;*/

        var volEntry = new PascalVolumeEntry
        {
            FirstBlock = BigEndianBitConverter.ToInt16(volBlock, 0x00),
            LastBlock  = BigEndianBitConverter.ToInt16(volBlock, 0x02),
            EntryType  = (PascalFileKind)BigEndianBitConverter.ToInt16(volBlock, 0x04),
            VolumeName = new byte[8],
            Blocks     = BigEndianBitConverter.ToInt16(volBlock, 0x0E),
            Files      = BigEndianBitConverter.ToInt16(volBlock, 0x10),
            Dummy      = BigEndianBitConverter.ToInt16(volBlock, 0x12),
            LastBoot   = BigEndianBitConverter.ToInt16(volBlock, 0x14),
            Tail       = BigEndianBitConverter.ToInt32(volBlock, 0x16)
        };

        Array.Copy(volBlock, 0x06, volEntry.VolumeName, 0, 8);

        AaruConsole.DebugWriteLine(MODULE_NAME, "volEntry.firstBlock = {0}", volEntry.FirstBlock);
        AaruConsole.DebugWriteLine(MODULE_NAME, "volEntry.lastBlock = {0}",  volEntry.LastBlock);
        AaruConsole.DebugWriteLine(MODULE_NAME, "volEntry.entryType = {0}",  volEntry.EntryType);

        //            AaruConsole.DebugWriteLine(MODULE_NAME, "volEntry.volumeName = {0}", volEntry.VolumeName);
        AaruConsole.DebugWriteLine(MODULE_NAME, "volEntry.blocks = {0}",   volEntry.Blocks);
        AaruConsole.DebugWriteLine(MODULE_NAME, "volEntry.files = {0}",    volEntry.Files);
        AaruConsole.DebugWriteLine(MODULE_NAME, "volEntry.dummy = {0}",    volEntry.Dummy);
        AaruConsole.DebugWriteLine(MODULE_NAME, "volEntry.lastBoot = {0}", volEntry.LastBoot);
        AaruConsole.DebugWriteLine(MODULE_NAME, "volEntry.tail = {0}",     volEntry.Tail);

        // First block is always 0 (even is it's sector 2)
        if(volEntry.FirstBlock != 0) return false;

        // Last volume record block must be after first block, and before end of device
        if(volEntry.LastBlock        <= volEntry.FirstBlock ||
           (ulong)volEntry.LastBlock > imagePlugin.Info.Sectors / _multiplier - 2)
            return false;

        // Volume record entry type must be volume or secure
        if(volEntry.EntryType != PascalFileKind.Volume && volEntry.EntryType != PascalFileKind.Secure) return false;

        // Volume name is max 7 characters
        if(volEntry.VolumeName[0] > 7) return false;

        // Volume blocks is equal to volume sectors
        if(volEntry.Blocks < 0 || (ulong)volEntry.Blocks != imagePlugin.Info.Sectors / _multiplier) return false;

        // There can be not less than zero files
        return volEntry.Files >= 0;
    }

    /// <inheritdoc />
    public void GetInformation(IMediaImage imagePlugin, Partition partition, Encoding encoding, out string information,
                               out FileSystem metadata)
    {
        encoding ??= new Apple2();
        var sbInformation = new StringBuilder();
        metadata    = new FileSystem();
        information = "";
        _multiplier = (uint)(imagePlugin.Info.SectorSize == 256 ? 2 : 1);

        if(imagePlugin.Info.Sectors < 3) return;

        // Blocks 0 and 1 are boot code
        ErrorNumber errno =
            imagePlugin.ReadSectors(_multiplier * 2 + partition.Start, _multiplier, out byte[] volBlock);

        if(errno != ErrorNumber.NoError) return;

        // On Apple //, it's little endian
        // TODO: Fix
        //BigEndianBitConverter.IsLittleEndian =
        //    multiplier == 2 ? !BitConverter.IsLittleEndian : BitConverter.IsLittleEndian;

        var volEntry = new PascalVolumeEntry
        {
            FirstBlock = BigEndianBitConverter.ToInt16(volBlock, 0x00),
            LastBlock  = BigEndianBitConverter.ToInt16(volBlock, 0x02),
            EntryType  = (PascalFileKind)BigEndianBitConverter.ToInt16(volBlock, 0x04),
            VolumeName = new byte[8],
            Blocks     = BigEndianBitConverter.ToInt16(volBlock, 0x0E),
            Files      = BigEndianBitConverter.ToInt16(volBlock, 0x10),
            Dummy      = BigEndianBitConverter.ToInt16(volBlock, 0x12),
            LastBoot   = BigEndianBitConverter.ToInt16(volBlock, 0x14),
            Tail       = BigEndianBitConverter.ToInt32(volBlock, 0x16)
        };

        Array.Copy(volBlock, 0x06, volEntry.VolumeName, 0, 8);

        // First block is always 0 (even is it's sector 2)
        if(volEntry.FirstBlock != 0) return;

        // Last volume record block must be after first block, and before end of device
        if(volEntry.LastBlock        <= volEntry.FirstBlock ||
           (ulong)volEntry.LastBlock > imagePlugin.Info.Sectors / _multiplier - 2)
            return;

        // Volume record entry type must be volume or secure
        if(volEntry.EntryType != PascalFileKind.Volume && volEntry.EntryType != PascalFileKind.Secure) return;

        // Volume name is max 7 characters
        if(volEntry.VolumeName[0] > 7) return;

        // Volume blocks is equal to volume sectors
        if(volEntry.Blocks < 0 || (ulong)volEntry.Blocks != imagePlugin.Info.Sectors / _multiplier) return;

        // There can be not less than zero files
        if(volEntry.Files < 0) return;

        sbInformation.AppendFormat(Localization.Volume_record_spans_from_block_0_to_block_1,
                                   volEntry.FirstBlock,
                                   volEntry.LastBlock)
                     .AppendLine();

        sbInformation
           .AppendFormat(Localization.Volume_name_0, StringHandlers.PascalToString(volEntry.VolumeName, encoding))
           .AppendLine();

        sbInformation.AppendFormat(Localization.Volume_has_0_blocks, volEntry.Blocks).AppendLine();
        sbInformation.AppendFormat(Localization.Volume_has_0_files,  volEntry.Files).AppendLine();

        sbInformation
           .AppendFormat(Localization.Volume_last_booted_on_0, DateHandlers.UcsdPascalToDateTime(volEntry.LastBoot))
           .AppendLine();

        information = sbInformation.ToString();

        imagePlugin.ReadSectors(partition.Start, _multiplier * 2, out byte[] boot);

        metadata = new FileSystem
        {
            Bootable    = !ArrayHelpers.ArrayIsNullOrEmpty(boot),
            Clusters    = (ulong)volEntry.Blocks,
            ClusterSize = imagePlugin.Info.SectorSize,
            Files       = (ulong)volEntry.Files,
            Type        = FS_TYPE,
            VolumeName  = StringHandlers.PascalToString(volEntry.VolumeName, encoding)
        };
    }

#endregion
}