// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Files.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Core algorithms.
//
// --[ Description ] ----------------------------------------------------------
//
//     Creates sidecar information of files contained in supported read-only
//     filesystems.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2023 Natalia Portillo
// ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Console;

namespace Aaru.Core;

public sealed partial class Sidecar
{
    FilesystemContents Files(IReadOnlyFilesystem filesystem)
    {
        var contents = new FilesystemContents();

        ErrorNumber ret = filesystem.ReadDir("/", out List<string> dirents);

        if(ret != ErrorNumber.NoError)
            return null;

        List<Directory>    directories = new();
        List<ContentsFile> files       = new();

        foreach(string dirent in dirents)
        {
            ret = filesystem.Stat(dirent, out FileEntryInfo stat);

            if(ret != ErrorNumber.NoError)
            {
                AaruConsole.DebugWriteLine("Create-Sidecar command", Localization.Core.Cannot_stat_0, dirent);

                continue;
            }

            if(stat.Attributes.HasFlag(FileAttributes.Directory))
            {
                directories.Add(SidecarDirectory(filesystem, "", dirent, stat));

                continue;
            }

            files.Add(SidecarFile(filesystem, "", dirent, stat));
        }

        if(files.Count > 0)
            contents.Files = files.OrderBy(f => f.Name).ToList();

        if(directories.Count > 0)
            contents.Directories = directories.OrderBy(d => d.Name).ToList();

        return contents;
    }

    Directory SidecarDirectory(IReadOnlyFilesystem filesystem, string path, string filename, FileEntryInfo stat)
    {
        var directory = new Directory
        {
            AccessTime       = stat.AccessTimeUtc,
            Attributes       = (ulong)stat.Attributes,
            BackupTime       = stat.BackupTimeUtc,
            CreationTime     = stat.CreationTimeUtc,
            DeviceNumber     = stat.DeviceNo,
            Inode            = stat.Inode,
            LastWriteTime    = stat.LastWriteTimeUtc,
            Links            = stat.Links,
            Name             = filename,
            PosixGroupId     = stat.GID,
            PosixMode        = stat.Mode,
            PosixUserId      = stat.UID,
            StatusChangeTime = stat.StatusChangeTimeUtc
        };

        ErrorNumber ret = filesystem.ReadDir(path + "/" + filename, out List<string> dirents);

        if(ret != ErrorNumber.NoError)
            return null;

        List<Directory>    directories = new();
        List<ContentsFile> files       = new();

        foreach(string dirent in dirents)
        {
            ret = filesystem.Stat(path + "/" + filename + "/" + dirent, out FileEntryInfo entryStat);

            if(ret != ErrorNumber.NoError)
            {
                AaruConsole.DebugWriteLine("Create-Sidecar command", Localization.Core.Cannot_stat_0, dirent);

                continue;
            }

            if(entryStat.Attributes.HasFlag(FileAttributes.Directory))
            {
                directories.Add(SidecarDirectory(filesystem, path + "/" + filename, dirent, entryStat));

                continue;
            }

            files.Add(SidecarFile(filesystem, path + "/" + filename, dirent, entryStat));
        }

        if(files.Count > 0)
            directory.Files = files.OrderBy(f => f.Name).ToList();

        if(directories.Count > 0)
            directory.Directories = directories.OrderBy(d => d.Name).ToList();

        return directory;
    }

    ContentsFile SidecarFile(IReadOnlyFilesystem filesystem, string path, string filename, FileEntryInfo stat)
    {
        var fileChkWorker = new Checksum();

        var file = new ContentsFile
        {
            AccessTime       = stat.AccessTimeUtc,
            Attributes       = (ulong)stat.Attributes,
            BackupTime       = stat.BackupTimeUtc,
            CreationTime     = stat.CreationTimeUtc,
            DeviceNumber     = stat.DeviceNo,
            Inode            = stat.Inode,
            LastWriteTime    = stat.LastWriteTimeUtc,
            Length           = (ulong)stat.Length,
            Links            = stat.Links,
            Name             = filename,
            PosixGroupId     = stat.GID,
            PosixMode        = stat.Mode,
            PosixUserId      = stat.UID,
            StatusChangeTime = stat.StatusChangeTimeUtc
        };

        byte[] data = Array.Empty<byte>();

        if(stat.Length > 0)
        {
            long position = 0;
            UpdateStatus(string.Format(Localization.Core.Hashing_file_0_1, path, filename));
            InitProgress2();

            while(position < stat.Length - 1048576)
            {
                if(_aborted)
                    return file;

                data = new byte[1048576];
                filesystem.Read(path + "/" + filename, position, 1048576, ref data);

                UpdateProgress2(Localization.Core.Hashing_file_byte_0_of_1, position, stat.Length);

                fileChkWorker.Update(data);

                position += 1048576;
            }

            data = new byte[stat.Length - position];
            filesystem.Read(path + "/" + filename, position, stat.Length - position, ref data);

            UpdateProgress2(Localization.Core.Hashing_file_byte_0_of_1, position, stat.Length);

            fileChkWorker.Update(data);

            EndProgress2();

            file.Checksums = fileChkWorker.End();
        }
        else
            file.Checksums = _emptyChecksums;

        ErrorNumber ret = filesystem.ListXAttr(path + "/" + filename, out List<string> xattrs);

        if(ret != ErrorNumber.NoError)
            return file;

        List<ExtendedAttribute> xattrTypes = new();

        foreach(string xattr in xattrs)
        {
            ret = filesystem.GetXattr(path + "/" + filename, xattr, ref data);

            if(ret != ErrorNumber.NoError)
                continue;

            var xattrChkWorker = new Checksum();
            xattrChkWorker.Update(data);

            xattrTypes.Add(new ExtendedAttribute
            {
                Checksums = xattrChkWorker.End(),
                Length    = (ulong)data.Length,
                Name      = xattr
            });
        }

        if(xattrTypes.Count > 0)
            file.ExtendedAttributes = xattrTypes.OrderBy(x => x.Name).ToList();

        return file;
    }
}