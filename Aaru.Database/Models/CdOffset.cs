// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : CdOffset.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Database.
//
// --[ Description ] ----------------------------------------------------------
//
//     Model for storing Compact Disc read offsets in database.
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
using System.ComponentModel.DataAnnotations.Schema;

namespace DiscImageChef.Database.Models
{
    public class CdOffset : CommonTypes.Metadata.CdOffset
    {
        public CdOffset() { }

        public CdOffset(string manufacturer, string model, short offset, int submissions, float agreement)
        {
            Manufacturer = manufacturer;
            Model        = model;
            Offset       = offset;
            Submissions  = submissions;
            Agreement    = agreement;
            AddedWhen    = ModifiedWhen = DateTime.UtcNow;
        }

        public CdOffset(CommonTypes.Metadata.CdOffset offset)
        {
            Manufacturer = offset.Manufacturer;
            Model        = offset.Model;
            Offset       = offset.Offset;
            Submissions  = offset.Submissions;
            Agreement    = offset.Agreement;
            AddedWhen    = ModifiedWhen = DateTime.UtcNow;
        }

        public int      Id        { get; set; }
        public DateTime AddedWhen { get; set; }
        [Index]
        public DateTime ModifiedWhen { get; set; }
    }
}