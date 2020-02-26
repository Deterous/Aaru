// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : ZZZNoFilter.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Filters.
//
// --[ Description ] ----------------------------------------------------------
//
//     Provides a filter to open single files.
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
using System.IO;
using DiscImageChef.CommonTypes.Interfaces;

namespace DiscImageChef.Filters
{
    /// <summary>
    ///     No filter for reading files not recognized by any filter
    /// </summary>
    public class ZZZNoFilter : IFilter
    {
        string   basePath;
        DateTime creationTime;
        Stream   dataStream;
        DateTime lastWriteTime;
        bool     opened;

        public string Name   => "No filter";
        public Guid   Id     => new Guid("12345678-AAAA-BBBB-CCCC-123456789000");
        public string Author => "Natalia Portillo";

        public void Close()
        {
            dataStream?.Close();
            dataStream = null;
            basePath   = null;
            opened     = false;
        }

        public string GetBasePath() => basePath;

        public Stream GetDataForkStream() => dataStream;

        public string GetPath() => basePath;

        public Stream GetResourceForkStream() => null;

        public bool HasResourceFork() => false;

        public bool Identify(byte[] buffer) => buffer != null && buffer.Length > 0;

        public bool Identify(Stream stream) => stream != null && stream.Length > 0;

        public bool Identify(string path) => File.Exists(path);

        public void Open(byte[] buffer)
        {
            dataStream    = new MemoryStream(buffer);
            basePath      = null;
            creationTime  = DateTime.UtcNow;
            lastWriteTime = creationTime;
            opened        = true;
        }

        public void Open(Stream stream)
        {
            dataStream    = stream;
            basePath      = null;
            creationTime  = DateTime.UtcNow;
            lastWriteTime = creationTime;
            opened        = true;
        }

        public void Open(string path)
        {
            dataStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            basePath   = Path.GetFullPath(path);
            FileInfo fi = new FileInfo(path);
            creationTime  = fi.CreationTimeUtc;
            lastWriteTime = fi.LastWriteTimeUtc;
            opened        = true;
        }

        public DateTime GetCreationTime() => creationTime;

        public long GetDataForkLength() => dataStream.Length;

        public DateTime GetLastWriteTime() => lastWriteTime;

        public long GetLength() => dataStream.Length;

        public long GetResourceForkLength() => 0;

        public string GetFilename() => Path.GetFileName(basePath);

        public string GetParentFolder() => Path.GetDirectoryName(basePath);

        public bool IsOpened() => opened;
    }
}