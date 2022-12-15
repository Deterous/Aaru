﻿// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : dump.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : dump(8) file system plugin
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies backups created with dump(8) shows information.
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
// Copyright © 2011-2023 Natalia Portillo
// ****************************************************************************/

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Interfaces;
using ufs_daddr_t = System.Int32;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements identification of a dump(8) image (virtual filesystem on a file)</summary>
[SuppressMessage("ReSharper", "InconsistentNaming"), SuppressMessage("ReSharper", "UnusedMember.Local")]
public sealed partial class dump : IFilesystem
{
    /// <inheritdoc />
    public Encoding Encoding { get; private set; }
    /// <inheritdoc />
    public string Name => Localization.dump_Name;
    /// <inheritdoc />
    public Guid Id => new("E53B4D28-C858-4800-B092-DDAE80D361B9");
    /// <inheritdoc />
    public FileSystem Metadata { get; private set; }
    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;
}