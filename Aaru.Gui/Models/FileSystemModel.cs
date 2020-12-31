// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : FileSystemModel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI data models.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains information about a filesystem.
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

using System.Collections.ObjectModel;
using Aaru.CommonTypes.Interfaces;
using Aaru.Gui.ViewModels.Panels;

namespace Aaru.Gui.Models
{
    public sealed class FileSystemModel : RootModel
    {
        public FileSystemModel() => Roots = new ObservableCollection<SubdirectoryModel>();

        public string                                  VolumeName         { get; set; }
        public IFilesystem                             Filesystem         { get; set; }
        public IReadOnlyFilesystem                     ReadOnlyFilesystem { get; set; }
        public FileSystemViewModel                     ViewModel          { get; set; }
        public ObservableCollection<SubdirectoryModel> Roots              { get; set; }
    }
}