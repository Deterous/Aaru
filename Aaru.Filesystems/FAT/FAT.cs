﻿// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : FAT.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Microsoft FAT filesystem plugin.
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
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;

namespace Aaru.Filesystems;

// TODO: Differentiate between Atari and X68k FAT, as this one uses a standard BPB.
// X68K uses cdate/adate from direntry for extending filename
/// <inheritdoc />
/// <summary>Implements the File Allocation Table, aka FAT, filesystem (FAT12, FAT16 and FAT32 variants).</summary>
public sealed partial class FAT : IReadOnlyFilesystem
{
    const string                                                   MODULE_NAME = "FAT plugin";
    uint                                                           _bytesPerCluster;
    byte[]                                                         _cachedEaData;
    CultureInfo                                                    _cultureInfo;
    bool                                                           _debug;
    Dictionary<string, Dictionary<string, CompleteDirectoryEntry>> _directoryCache;
    DirectoryEntry                                                 _eaDirEntry;
    Encoding                                                       _encoding;
    bool                                                           _fat12;
    bool                                                           _fat16;
    bool                                                           _fat32;
    ushort[]                                                       _fatEntries;
    uint                                                           _fatEntriesPerSector;
    ulong                                                          _fatFirstSector;
    ulong                                                          _firstClusterSector;
    IMediaImage                                                    _image;
    bool                                                           _mounted;
    Namespace                                                      _namespace;
    uint                                                           _reservedSectors;
    Dictionary<string, CompleteDirectoryEntry>                     _rootDirectoryCache;
    uint                                                           _sectorsPerCluster;
    uint                                                           _sectorsPerFat;
    FileSystemInfo                                                 _statfs;
    bool                                                           _useFirstFat;

#region IReadOnlyFilesystem Members

    /// <inheritdoc />
    public FileSystem Metadata { get; private set; }

    /// <inheritdoc />
    public string Name => Localization.FAT_Name;

    /// <inheritdoc />
    public Guid Id => new("33513B2C-0D26-0D2D-32C3-79D8611158E0");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

    /// <inheritdoc />
    public IEnumerable<(string name, Type type, string description)> SupportedOptions =>
        Array.Empty<(string name, Type type, string description)>();

    /// <inheritdoc />
    public Dictionary<string, string> Namespaces => new()
    {
        {
            "dos", Localization.DOS_8_3_all_uppercase
        },
        {
            "nt", Localization.Windows_NT_8_3_mixed_case
        },
        {
            "os2", Localization.OS2_LONGNAME_extended_attribute
        },
        {
            "ecs", Localization.Use_LFN_when_available_with_fallback_to_LONGNAME_default
        },
        {
            "lfn", Localization.Long_file_names
        }
    };

#endregion

    static Dictionary<string, string> GetDefaultOptions() => new()
    {
        {
            "debug", false.ToString()
        }
    };
}