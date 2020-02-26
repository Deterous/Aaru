﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : Helpers.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains helpers for Microsoft Hyper-V disk images.
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
using System.Linq;

namespace DiscImageChef.DiscImages
{
    public partial class Vhdx
    {
        bool CheckBitmap(ulong sectorAddress)
        {
            long index = (long)(sectorAddress / 8);
            int  shift = (int)(sectorAddress  % 8);
            byte val   = (byte)(1 << shift);

            if(index > sectorBitmap.LongLength) return false;

            return (sectorBitmap[index] & val) == val;
        }

        static uint VhdxChecksum(IEnumerable<byte> data)
        {
            uint checksum = data.Aggregate<byte, uint>(0, (current, b) => current + b);

            return ~checksum;
        }
    }
}