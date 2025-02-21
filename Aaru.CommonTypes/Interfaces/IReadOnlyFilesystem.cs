﻿// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : IReadOnlyFilesystem.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Filesystem plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Interface for filesystem plugins that offer read-only support of their
//     contents.
//
// --[ License ] --------------------------------------------------------------
//
//     Permission is hereby granted, free of charge, to any person obtaining a
//     copy of this software and associated documentation files (the
//     "Software"), to deal in the Software without restriction, including
//     without limitation the rights to use, copy, modify, merge, publish,
//     distribute, sublicense, and/or sell copies of the Software, and to
//     permit persons to whom the Software is furnished to do so, subject to
//     the following conditions:
//
//     The above copyright notice and this permission notice shall be included
//     in all copies or substantial portions of the Software.
//
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
//     OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//     MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//     IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//     CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//     TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//     SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2024 Natalia Portillo
// ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using FileAttributes = Aaru.CommonTypes.Structs.FileAttributes;
using FileSystemInfo = Aaru.CommonTypes.Structs.FileSystemInfo;

namespace Aaru.CommonTypes.Interfaces;

/// <inheritdoc />
/// <summary>Defines the interface to implement reading the contents of a filesystem</summary>
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "UnusedMethodReturnValue.Global")]
[SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
public interface IReadOnlyFilesystem : IFilesystem
{
    /// <summary>Information about the filesystem as expected by Aaru Metadata</summary>
    FileSystem Metadata { get; }

    /// <summary>Retrieves a list of options supported by the filesystem, with name, type and description</summary>
    IEnumerable<(string name, Type type, string description)> SupportedOptions { get; }

    /// <summary>Supported namespaces</summary>
    Dictionary<string, string> Namespaces { get; }

    /// <summary>
    ///     Initializes whatever internal structures the filesystem plugin needs to be able to read files and directories
    ///     from the filesystem.
    /// </summary>
    /// <param name="imagePlugin"></param>
    /// <param name="partition"></param>
    /// <param name="encoding">Which encoding to use for this filesystem.</param>
    /// <param name="options">Dictionary of key=value pairs containing options to pass to the filesystem</param>
    /// <param name="namespace">Filename namespace</param>
    ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                      Dictionary<string, string> options,     string    @namespace);

    /// <summary>Frees all internal structures created by <see cref="Mount" /></summary>
    ErrorNumber Unmount();

    /// <summary>Gets the attributes of a file or directory</summary>
    /// <returns>Error number.</returns>
    /// <param name="path">File path.</param>
    /// <param name="attributes">File attributes.</param>
    ErrorNumber GetAttributes(string path, out FileAttributes attributes);

    /// <summary>Lists all extended attributes, alternate data streams and forks of the given file.</summary>
    /// <returns>Error number.</returns>
    /// <param name="path">Path.</param>
    /// <param name="xattrs">List of extended attributes, alternate data streams and forks.</param>
    ErrorNumber ListXAttr(string path, out List<string> xattrs);

    /// <summary>Reads an extended attribute, alternate data stream or fork from the given file.</summary>
    /// <returns>Error number.</returns>
    /// <param name="path">File path.</param>
    /// <param name="xattr">Extended attribute, alternate data stream or fork name.</param>
    /// <param name="buf">Buffer.</param>
    ErrorNumber GetXattr(string path, string xattr, ref byte[] buf);

    /// <summary>Gets information about the mounted volume.</summary>
    /// <param name="stat">Information about the mounted volume.</param>
    ErrorNumber StatFs(out FileSystemInfo stat);

    /// <summary>Gets information about a file or directory.</summary>
    /// <param name="path">File path.</param>
    /// <param name="stat">File information.</param>
    ErrorNumber Stat(string path, out FileEntryInfo stat);

    /// <summary>Solves a symbolic link.</summary>
    /// <param name="path">Link path.</param>
    /// <param name="dest">Link destination.</param>
    ErrorNumber ReadLink(string path, out string dest);

    /// <summary>Opens a file for reading.</summary>
    /// <param name="path">Path to the file.</param>
    /// <param name="node">Represents the opened file and is needed for other file-related operations.</param>
    /// <returns>Error number</returns>
    ErrorNumber OpenFile(string path, out IFileNode node);

    /// <summary>Closes a file, freeing any private data allocated on opening.</summary>
    /// <param name="node">The file node.</param>
    /// <returns>Error number.</returns>
    ErrorNumber CloseFile(IFileNode node);

    /// <summary>Move the file node position pointer to the specified position with the specified origin</summary>
    /// <param name="node">The file node.</param>
    /// <param name="position">Desired position.</param>
    /// <param name="origin">From where in the file to move the position pointer to.</param>
    /// <returns>Error number.</returns>
    ErrorNumber Seek(IFileNode node, long position, SeekOrigin origin)
    {
        if(node is null) return ErrorNumber.InvalidArgument;

        long desiredPosition = origin switch
                               {
                                   SeekOrigin.Begin => position,
                                   SeekOrigin.End   => node.Length + position,
                                   _                => node.Offset + position
                               };

        if(desiredPosition < 0) return ErrorNumber.InvalidArgument;

        if(desiredPosition >= node.Length) return ErrorNumber.InvalidArgument;

        node.Offset = desiredPosition;

        return ErrorNumber.NoError;
    }

    /// <summary>Reads data from a file (main/only data stream or data fork).</summary>
    /// <param name="node">File node.</param>
    /// <param name="length">Bytes to read.</param>
    /// <param name="buffer">Buffer. Must exist and be of size equal or bigger than <see cref="length" /></param>
    /// <param name="read">How many bytes were read into the buffer</param>
    ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read);

    /// <summary>Opens a directory for listing.</summary>
    /// <param name="path">Path to the directory.</param>
    /// <param name="node">Represents the opened directory and is needed for other directory-related operations.</param>
    /// <returns>Error number</returns>
    ErrorNumber OpenDir(string path, out IDirNode node);

    /// <summary>Closes a directory, freeing any private data allocated on opening.</summary>
    /// <param name="node">The directory node.</param>
    /// <returns>Error number.</returns>
    ErrorNumber CloseDir(IDirNode node);

    /// <summary>Reads the next entry in the directory.</summary>
    /// <param name="node">Represent an opened directory.</param>
    /// <param name="filename">
    ///     The next entry name.
    ///     <value>null</value>
    ///     if there are no more entries.
    /// </param>
    /// <returns>Error number.</returns>
    ErrorNumber ReadDir(IDirNode node, out string filename);
}

/// <summary>Represents an opened file from a filesystem</summary>
[SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
public interface IFileNode
{
    /// <summary>Path to the file</summary>
    string Path { get; }

    /// <summary>File length</summary>
    long Length { get; }

    /// <summary>Current position in file</summary>
    long Offset { get; set; }
}

/// <summary>Represents an opened directory from a filesystem</summary>
[SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
public interface IDirNode
{
    /// <summary>Path to the directory</summary>
    string Path { get; }
}