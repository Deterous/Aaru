// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Console;
using Aaru.Helpers;
using FileAttributes = Aaru.CommonTypes.Structs.FileAttributes;

namespace Aaru.Filesystems;

public sealed partial class ISO9660
{
#region IReadOnlyFilesystem Members

    /// <inheritdoc />
    public ErrorNumber GetAttributes(string path, out FileAttributes attributes)
    {
        attributes = new FileAttributes();

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber err = Stat(path, out FileEntryInfo stat);

        if(err != ErrorNumber.NoError) return err;

        attributes = stat.Attributes;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber OpenFile(string path, out IFileNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber err = GetFileEntry(path, out DecodedDirectoryEntry entry);

        if(err != ErrorNumber.NoError) return err;

        if(entry.Flags.HasFlag(FileFlags.Directory) && !_debug) return ErrorNumber.IsDirectory;

        if(entry.Extents is null) return ErrorNumber.InvalidArgument;

        node = new Iso9660FileNode
        {
            Path   = path,
            Length = (long)entry.Size,
            Offset = 0,
            Dentry = entry
        };

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node)
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not Iso9660FileNode mynode) return ErrorNumber.InvalidArgument;

        mynode.Dentry = null;

        return ErrorNumber.NoError;
    }

    // TODO: Resolve symbolic link
    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read)
    {
        read = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(buffer is null || buffer.Length < length) return ErrorNumber.InvalidArgument;

        if(node is not Iso9660FileNode mynode) return ErrorNumber.InvalidArgument;

        read = length;

        if(length + mynode.Offset >= mynode.Length) read = mynode.Length - mynode.Offset;

        long offset = mynode.Offset + mynode.Dentry.XattrLength * _blockSize;

        if(mynode.Dentry.CdiSystemArea?.attributes.HasFlag(CdiAttributes.DigitalAudio) != true ||
           mynode.Dentry.Extents.Count                                                 != 1)
        {
            ErrorNumber err = ReadWithExtents(offset,
                                              read,
                                              mynode.Dentry.Extents,
                                              mynode.Dentry.XA?.signature == XA_MAGIC &&
                                              mynode.Dentry.XA?.attributes.HasFlag(XaAttributes.Interleaved) == true,
                                              mynode.Dentry.XA?.filenumber ?? 0,
                                              out byte[] buf);

            if(err != ErrorNumber.NoError)
            {
                read = 0;

                return err;
            }

            Array.Copy(buf, 0, buffer, 0, read);

            node.Offset += read;

            return ErrorNumber.NoError;
        }

        try
        {
            long firstSector    = offset                  / 2352;
            long offsetInSector = offset                  % 2352;
            long sizeInSectors  = (read + offsetInSector) / 2352;

            if((read + offsetInSector) % 2352 > 0) sizeInSectors++;

            ErrorNumber errno = _image.ReadSectorsLong((ulong)(mynode.Dentry.Extents[0].extent + firstSector),
                                                       (uint)sizeInSectors,
                                                       out byte[] buf);

            if(errno != ErrorNumber.NoError)
            {
                read = 0;

                return errno;
            }

            Array.Copy(buf, offsetInSector, buffer, 0, read);

            node.Offset += read;

            return ErrorNumber.NoError;
        }
        catch(Exception ex)
        {
            AaruConsole.DebugWriteLine(MODULE_NAME, Localization.Exception_reading_CD_i_audio_file);
            AaruConsole.WriteException(ex);

            read = 0;

            return ErrorNumber.UnexpectedException;
        }
    }

    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber err = GetFileEntry(path, out DecodedDirectoryEntry entry);

        if(err != ErrorNumber.NoError) return err;

        stat = new FileEntryInfo
        {
            Attributes       = new FileAttributes(),
            Blocks           = (long)(entry.Size / 2048), // TODO: XA
            BlockSize        = 2048,
            Length           = (long)entry.Size,
            Links            = 1,
            LastWriteTimeUtc = entry.Timestamp
        };

        if(entry.Extents?.Count > 0) stat.Inode = entry.Extents[0].extent;

        if(entry.Size % 2048 > 0) stat.Blocks++;

        if(entry.Flags.HasFlag(FileFlags.Directory)) stat.Attributes |= FileAttributes.Directory;

        if(entry.Flags.HasFlag(FileFlags.Hidden)) stat.Attributes |= FileAttributes.Hidden;

        if(entry.FinderInfo?.fdFlags.HasFlag(AppleCommon.FinderFlags.kIsAlias) == true)
            stat.Attributes |= FileAttributes.Alias;

        if(entry.FinderInfo?.fdFlags.HasFlag(AppleCommon.FinderFlags.kIsInvisible) == true)
            stat.Attributes |= FileAttributes.Hidden;

        if(entry.FinderInfo?.fdFlags.HasFlag(AppleCommon.FinderFlags.kHasBeenInited) == true)
            stat.Attributes |= FileAttributes.HasBeenInited;

        if(entry.FinderInfo?.fdFlags.HasFlag(AppleCommon.FinderFlags.kHasCustomIcon) == true)
            stat.Attributes |= FileAttributes.HasCustomIcon;

        if(entry.FinderInfo?.fdFlags.HasFlag(AppleCommon.FinderFlags.kHasNoINITs) == true)
            stat.Attributes |= FileAttributes.HasNoINITs;

        if(entry.FinderInfo?.fdFlags.HasFlag(AppleCommon.FinderFlags.kIsOnDesk) == true)
            stat.Attributes |= FileAttributes.IsOnDesk;

        if(entry.FinderInfo?.fdFlags.HasFlag(AppleCommon.FinderFlags.kIsShared) == true)
            stat.Attributes |= FileAttributes.Shared;

        if(entry.FinderInfo?.fdFlags.HasFlag(AppleCommon.FinderFlags.kIsStationery) == true)
            stat.Attributes |= FileAttributes.Stationery;

        if(entry.FinderInfo?.fdFlags.HasFlag(AppleCommon.FinderFlags.kHasBundle) == true)
            stat.Attributes |= FileAttributes.Bundle;

        if(entry.AppleIcon != null) stat.Attributes |= FileAttributes.HasCustomIcon;

        if(entry.XA != null)
        {
            if(entry.XA.Value.attributes.HasFlag(XaAttributes.GroupExecute)) stat.Mode |= 8;

            if(entry.XA.Value.attributes.HasFlag(XaAttributes.GroupRead)) stat.Mode |= 32;

            if(entry.XA.Value.attributes.HasFlag(XaAttributes.OwnerExecute)) stat.Mode |= 64;

            if(entry.XA.Value.attributes.HasFlag(XaAttributes.OwnerRead)) stat.Mode |= 256;

            if(entry.XA.Value.attributes.HasFlag(XaAttributes.SystemExecute)) stat.Mode |= 1;

            if(entry.XA.Value.attributes.HasFlag(XaAttributes.SystemRead)) stat.Mode |= 4;

            stat.UID   = entry.XA.Value.user;
            stat.GID   = entry.XA.Value.group;
            stat.Inode = entry.XA.Value.filenumber;
        }

        if(entry.PosixAttributes != null)
        {
            stat.Mode = (uint?)entry.PosixAttributes.Value.st_mode & 0x0FFF;

            if(entry.PosixAttributes.Value.st_mode.HasFlag(PosixMode.Block))
                stat.Attributes |= FileAttributes.BlockDevice;

            if(entry.PosixAttributes.Value.st_mode.HasFlag(PosixMode.Character))
                stat.Attributes |= FileAttributes.CharDevice;

            if(entry.PosixAttributes.Value.st_mode.HasFlag(PosixMode.Pipe)) stat.Attributes |= FileAttributes.Pipe;

            if(entry.PosixAttributes.Value.st_mode.HasFlag(PosixMode.Socket)) stat.Attributes |= FileAttributes.Socket;

            if(entry.PosixAttributes.Value.st_mode.HasFlag(PosixMode.Symlink))
                stat.Attributes |= FileAttributes.Symlink;

            stat.Links = entry.PosixAttributes.Value.st_nlink;
            stat.UID   = entry.PosixAttributes.Value.st_uid;
            stat.GID   = entry.PosixAttributes.Value.st_gid;
            stat.Inode = entry.PosixAttributes.Value.st_ino;
        }
        else if(entry.PosixAttributesOld != null)
        {
            stat.Mode = (uint?)entry.PosixAttributesOld.Value.st_mode & 0x0FFF;

            if(entry.PosixAttributesOld.Value.st_mode.HasFlag(PosixMode.Block))
                stat.Attributes |= FileAttributes.BlockDevice;

            if(entry.PosixAttributesOld.Value.st_mode.HasFlag(PosixMode.Character))
                stat.Attributes |= FileAttributes.CharDevice;

            if(entry.PosixAttributesOld.Value.st_mode.HasFlag(PosixMode.Pipe)) stat.Attributes |= FileAttributes.Pipe;

            if(entry.PosixAttributesOld.Value.st_mode.HasFlag(PosixMode.Socket))
                stat.Attributes |= FileAttributes.Socket;

            if(entry.PosixAttributesOld.Value.st_mode.HasFlag(PosixMode.Symlink))
                stat.Attributes |= FileAttributes.Symlink;

            stat.Links = entry.PosixAttributesOld.Value.st_nlink;
            stat.UID   = entry.PosixAttributesOld.Value.st_uid;
            stat.GID   = entry.PosixAttributesOld.Value.st_gid;
        }

        if(entry.AmigaProtection != null)
        {
            if(entry.AmigaProtection.Value.Multiuser.HasFlag(AmigaMultiuser.GroupExec)) stat.Mode |= 8;

            if(entry.AmigaProtection.Value.Multiuser.HasFlag(AmigaMultiuser.GroupRead)) stat.Mode |= 32;

            if(entry.AmigaProtection.Value.Multiuser.HasFlag(AmigaMultiuser.GroupWrite)) stat.Mode |= 16;

            if(entry.AmigaProtection.Value.Multiuser.HasFlag(AmigaMultiuser.OtherExec)) stat.Mode |= 1;

            if(entry.AmigaProtection.Value.Multiuser.HasFlag(AmigaMultiuser.OtherRead)) stat.Mode |= 4;

            if(entry.AmigaProtection.Value.Multiuser.HasFlag(AmigaMultiuser.OtherWrite)) stat.Mode |= 2;

            if(entry.AmigaProtection.Value.Protection.HasFlag(AmigaAttributes.OwnerExec)) stat.Mode |= 64;

            if(entry.AmigaProtection.Value.Protection.HasFlag(AmigaAttributes.OwnerRead)) stat.Mode |= 256;

            if(entry.AmigaProtection.Value.Protection.HasFlag(AmigaAttributes.OwnerWrite)) stat.Mode |= 128;

            if(entry.AmigaProtection.Value.Protection.HasFlag(AmigaAttributes.Archive))
                stat.Attributes |= FileAttributes.Archive;
        }

        if(entry.PosixDeviceNumber != null)
        {
            stat.DeviceNo = ((ulong)entry.PosixDeviceNumber.Value.dev_t_high << 32) +
                            entry.PosixDeviceNumber.Value.dev_t_low;
        }

        if(entry.RripModify != null) stat.LastWriteTimeUtc = DecodeIsoDateTime(entry.RripModify);

        if(entry.RripAccess != null) stat.AccessTimeUtc = DecodeIsoDateTime(entry.RripAccess);

        if(entry.RripAttributeChange != null) stat.StatusChangeTimeUtc = DecodeIsoDateTime(entry.RripAttributeChange);

        if(entry.RripBackup != null) stat.BackupTimeUtc = DecodeIsoDateTime(entry.RripBackup);

        if(entry.SymbolicLink != null) stat.Attributes |= FileAttributes.Symlink;

        if(entry.XattrLength == 0 || _cdi || _highSierra) return ErrorNumber.NoError;

        if(entry.CdiSystemArea != null)
        {
            stat.UID = entry.CdiSystemArea.Value.owner;
            stat.GID = entry.CdiSystemArea.Value.group;

            if(entry.CdiSystemArea.Value.attributes.HasFlag(CdiAttributes.GroupExecute)) stat.Mode |= 8;

            if(entry.CdiSystemArea.Value.attributes.HasFlag(CdiAttributes.GroupRead)) stat.Mode |= 32;

            if(entry.CdiSystemArea.Value.attributes.HasFlag(CdiAttributes.OtherExecute)) stat.Mode |= 1;

            if(entry.CdiSystemArea.Value.attributes.HasFlag(CdiAttributes.OtherRead)) stat.Mode |= 4;

            if(entry.CdiSystemArea.Value.attributes.HasFlag(CdiAttributes.OwnerExecute)) stat.Mode |= 64;

            if(entry.CdiSystemArea.Value.attributes.HasFlag(CdiAttributes.OwnerRead)) stat.Mode |= 256;
        }

        ErrorNumber errno = ReadSingleExtent(entry.XattrLength * _blockSize, entry.Extents[0].extent, out byte[] ea);

        if(errno != ErrorNumber.NoError) return ErrorNumber.NoError;

        ExtendedAttributeRecord ear = Marshal.ByteArrayToStructureLittleEndian<ExtendedAttributeRecord>(ea);

        stat.UID = ear.owner;
        stat.GID = ear.group;

        stat.Mode = 0;

        if(ear.permissions.HasFlag(Permissions.GroupExecute)) stat.Mode |= 8;

        if(ear.permissions.HasFlag(Permissions.GroupRead)) stat.Mode |= 32;

        if(ear.permissions.HasFlag(Permissions.OwnerExecute)) stat.Mode |= 64;

        if(ear.permissions.HasFlag(Permissions.OwnerRead)) stat.Mode |= 256;

        if(ear.permissions.HasFlag(Permissions.OtherExecute)) stat.Mode |= 1;

        if(ear.permissions.HasFlag(Permissions.OtherRead)) stat.Mode |= 4;

        stat.CreationTimeUtc  = DateHandlers.Iso9660ToDateTime(ear.creation_date);
        stat.LastWriteTimeUtc = DateHandlers.Iso9660ToDateTime(ear.modification_date);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadLink(string path, out string dest)
    {
        dest = null;

        ErrorNumber err = GetFileEntry(path, out DecodedDirectoryEntry entry);

        if(err != ErrorNumber.NoError) return err;

        if(entry.SymbolicLink is null) return ErrorNumber.InvalidArgument;

        dest = entry.SymbolicLink;

        return ErrorNumber.NoError;
    }

#endregion

    ErrorNumber GetFileEntry(string path, out DecodedDirectoryEntry entry)
    {
        entry = null;

        string cutPath = path.StartsWith("/", StringComparison.Ordinal)
                             ? path[1..].ToLower(CultureInfo.CurrentUICulture)
                             : path.ToLower(CultureInfo.CurrentUICulture);

        string[] pieces = cutPath.Split(new[]
                                        {
                                            '/'
                                        },
                                        StringSplitOptions.RemoveEmptyEntries);

        if(pieces.Length == 0) return ErrorNumber.InvalidArgument;

        var parentPath = string.Join("/", pieces, 0, pieces.Length - 1);

        if(!_directoryCache.TryGetValue(parentPath, out _))
        {
            ErrorNumber err = OpenDir(parentPath, out IDirNode node);

            if(err != ErrorNumber.NoError) return err;

            CloseDir(node);
        }

        Dictionary<string, DecodedDirectoryEntry> parent;

        if(pieces.Length == 1)
            parent = _rootDirectoryCache;
        else if(!_directoryCache.TryGetValue(parentPath, out parent)) return ErrorNumber.InvalidArgument;

        KeyValuePair<string, DecodedDirectoryEntry> dirent =
            parent.FirstOrDefault(t => t.Key.Equals(pieces[^1], StringComparison.CurrentCultureIgnoreCase));

        if(string.IsNullOrEmpty(dirent.Key))
        {
            if(!_joliet && !pieces[^1].EndsWith(";1", StringComparison.Ordinal))
            {
                dirent = parent.FirstOrDefault(t => t.Key.Equals(pieces[^1] + ";1",
                                                                 StringComparison.CurrentCultureIgnoreCase));

                if(string.IsNullOrEmpty(dirent.Key)) return ErrorNumber.NoSuchFile;
            }
            else
                return ErrorNumber.NoSuchFile;
        }

        entry = dirent.Value;

        return ErrorNumber.NoError;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ErrorNumber ReadSingleExtent(long size, uint startingSector, out byte[] buffer, bool interleaved = false,
                                 byte fileNumber = 0) => ReadWithExtents(0,
                                                                         size,
                                                                         [(startingSector, (uint)size)],
                                                                         interleaved,
                                                                         fileNumber,
                                                                         out buffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ErrorNumber ReadSingleExtent(long offset,              long size, uint startingSector, out byte[] buffer,
                                 bool interleaved = false, byte fileNumber = 0) => ReadWithExtents(offset,
        size,
        [(startingSector, (uint)size)],
        interleaved,
        fileNumber,
        out buffer);

    // Cannot think how to make this faster, as we don't know the mode sector until it is read, but we have size in bytes
    ErrorNumber ReadWithExtents(long offset,     long size, List<(uint extent, uint size)> extents, bool interleaved,
                                byte fileNumber, out byte[] buffer)
    {
        var  ms             = new MemoryStream();
        long currentFilePos = 0;

        for(var i = 0; i < extents.Count; i++)
        {
            if(offset - currentFilePos >= extents[i].size)
            {
                currentFilePos += extents[i].size;

                continue;
            }

            long leftExtentSize      = extents[i].size;
            uint currentExtentSector = 0;

            while(leftExtentSize > 0)
            {
                ErrorNumber errno = ReadSector(extents[i].extent + currentExtentSector,
                                               out byte[] sector,
                                               interleaved,
                                               fileNumber);

                if(errno != ErrorNumber.NoError)
                {
                    buffer = ms.ToArray();

                    return errno;
                }

                if(sector is null)
                {
                    currentExtentSector++;
                    leftExtentSize -= 2048;
                    currentFilePos += 2048;

                    continue;
                }

                if(offset - currentFilePos > sector.Length)
                {
                    currentExtentSector++;
                    leftExtentSize -= sector.Length;
                    currentFilePos += sector.Length;

                    continue;
                }

                if(offset - currentFilePos > 0)
                    ms.Write(sector, (int)(offset - currentFilePos), (int)(sector.Length - (offset - currentFilePos)));
                else
                    ms.Write(sector, 0, sector.Length);

                currentExtentSector++;
                leftExtentSize -= sector.Length;
                currentFilePos += sector.Length;

                if(ms.Length >= size) break;
            }

            if(ms.Length >= size) break;
        }

        if(ms.Length >= size) ms.SetLength(size);

        buffer = ms.ToArray();

        return ErrorNumber.NoError;
    }

    byte[] ReadSubheaderWithExtents(List<(uint extent, uint size)> extents, bool copy)
    {
        var ms = new MemoryStream();

        for(var i = 0; i < extents.Count; i++)
        {
            long leftExtentSize      = extents[i].size;
            uint currentExtentSector = 0;

            while(leftExtentSize > 0)
            {
                ErrorNumber errno = _image.ReadSectorTag((extents[i].extent + currentExtentSector) * _blockSize / 2048,
                                                         SectorTagType.CdSectorSubHeader,
                                                         out byte[] fullSector);

                if(errno != ErrorNumber.NoError) return null;

                ms.Write(fullSector, copy ? 0 : 4, 4);

                currentExtentSector++;
                leftExtentSize -= 2048;
            }
        }

        return ms.ToArray();
    }
}