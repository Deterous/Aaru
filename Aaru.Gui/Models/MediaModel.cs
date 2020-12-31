// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : MediaModel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI data models.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains information about physical media.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General public License for more details.
//
//     You should have received a copy of the GNU General public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2021 Natalia Portillo
// ****************************************************************************/

using Aaru.Gui.ViewModels.Panels;
using Avalonia.Media.Imaging;

namespace Aaru.Gui.Models
{
    public sealed class MediaModel
    {
        public Bitmap             Icon            { get; set; }
        public string             Name            { get; set; }
        public string             DevicePath      { get; set; }
        public bool               NonRemovable    { get; set; }
        public bool               NoMediaInserted { get; set; }
        public MediaInfoViewModel ViewModel       { get; set; }
    }
}