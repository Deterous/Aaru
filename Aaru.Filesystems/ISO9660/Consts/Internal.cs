﻿// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : ISO9660 filesystem plugin.
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
// In the loving memory of Facunda "Tata" Suárez Domínguez, R.I.P. 2019/07/24
// ****************************************************************************/

using System.Diagnostics.CodeAnalysis;
using Aaru.Helpers;

namespace Aaru.Filesystems;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
public sealed partial class ISO9660
{
    const byte MODE2_FORM2 = 0x20;

    const           string FS_TYPE_HSF          = "hfs";
    const           string FS_TYPE_CDI          = "cdi";
    const           string FS_TYPE_ISO          = "iso9660";
    static readonly int    _directoryRecordSize = Marshal.SizeOf<DirectoryRecord>();

#region Nested type: Namespace

    enum Namespace
    {
        Normal,
        Vms,
        Joliet,
        Rrip,
        Romeo
    }

#endregion
}