﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains structures for partclone disk images.
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

using System.Runtime.InteropServices;

namespace DiscImageChef.DiscImages
{
    public partial class PartClone
    {
        /// <summary>
        ///     PartClone disk image header, little-endian
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct PartCloneHeader
        {
            /// <summary>
            ///     Magic, <see cref="PartClone.partCloneMagic" />
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            public byte[] magic;
            /// <summary>
            ///     Source filesystem
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            public byte[] filesystem;
            /// <summary>
            ///     Version
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] version;
            /// <summary>
            ///     Padding
            /// </summary>
            public ushort padding;
            /// <summary>
            ///     Block (sector) size
            /// </summary>
            public uint blockSize;
            /// <summary>
            ///     Size of device containing the cloned partition
            /// </summary>
            public ulong deviceSize;
            /// <summary>
            ///     Total blocks in cloned partition
            /// </summary>
            public ulong totalBlocks;
            /// <summary>
            ///     Used blocks in cloned partition
            /// </summary>
            public ulong usedBlocks;
            /// <summary>
            ///     Empty space
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4096)]
            public byte[] buffer;
        }
    }
}