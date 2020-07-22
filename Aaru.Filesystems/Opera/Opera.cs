﻿// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Opera.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Opera filesystem plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies the Opera filesystem and shows information.
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
using System.Collections.Generic;
using System.Text;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Schemas;

namespace Aaru.Filesystems
{
    public sealed partial class OperaFS : IReadOnlyFilesystem
    {
        bool                                                               _debug;
        Dictionary<string, Dictionary<string, DirectoryEntryWithPointers>> _directoryCache;
        IMediaImage                                                        _image;
        bool                                                               _mounted;
        Dictionary<string, DirectoryEntryWithPointers>                     _rootDirectoryCache;
        FileSystemInfo                                                     _statfs;
        uint                                                               _volumeBlockSizeRatio;

        public FileSystemType XmlFsType { get; private set; }
        public Encoding       Encoding  { get; private set; }
        public string         Name      => "Opera Filesystem Plugin";
        public Guid           Id        => new Guid("0ec84ec7-eae6-4196-83fe-943b3fe46dbd");
        public string         Author    => "Natalia Portillo";

        public Errno ListXAttr(string path, out List<string> xattrs)
        {
            xattrs = null;

            return Errno.NotSupported;
        }

        public Errno GetXattr(string path, string xattr, ref byte[] buf) => Errno.NotSupported;

        public Errno ReadLink(string path, out string dest)
        {
            dest = null;

            return Errno.NotSupported;
        }

        public IEnumerable<(string name, Type type, string description)> SupportedOptions =>
            new (string name, Type type, string description)[]
                {};

        public Dictionary<string, string> Namespaces => null;

        static Dictionary<string, string> GetDefaultOptions() => new Dictionary<string, string>
        {
            {
                "debug", false.ToString()
            }
        };
    }
}