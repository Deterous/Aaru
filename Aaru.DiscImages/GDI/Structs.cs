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
//     Contains structures for Dreamcast GDI disc images.
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
using DiscImageChef.CommonTypes;
using DiscImageChef.CommonTypes.Enums;
using DiscImageChef.CommonTypes.Interfaces;
using DiscImageChef.CommonTypes.Structs;

namespace DiscImageChef.DiscImages
{
    public partial class Gdi
    {
        struct GdiTrack
        {
            /// <summary>Track #</summary>
            public uint Sequence;
            /// <summary>Track filter</summary>
            public IFilter Trackfilter;
            /// <summary>Track file</summary>
            public string Trackfile;
            /// <summary>Track byte offset in file</summary>
            public long Offset;
            /// <summary>Track flags</summary>
            public byte Flags;
            /// <summary>Track starting sector</summary>
            public ulong StartSector;
            /// <summary>Bytes per sector</summary>
            public ushort Bps;
            /// <summary>Sectors in track</summary>
            public ulong Sectors;
            /// <summary>Track type</summary>
            public TrackType Tracktype;
            /// <summary>Track session</summary>
            public bool HighDensity;
            /// <summary>Pregap sectors not stored in track file</summary>
            public ulong Pregap;
        }

        struct GdiDisc
        {
            /// <summary>Sessions</summary>
            public List<Session> Sessions;
            /// <summary>Tracks</summary>
            public List<GdiTrack> Tracks;
            /// <summary>Disk type</summary>
            public MediaType Disktype;
        }
    }
}