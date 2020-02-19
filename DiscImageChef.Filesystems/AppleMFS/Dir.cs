// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple Macintosh File System plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Methods to handle the Apple Macintosh File System directory.
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
using System.Linq;
using DiscImageChef.CommonTypes.Structs;
using DiscImageChef.Console;
using DiscImageChef.Helpers;

namespace DiscImageChef.Filesystems.AppleMFS
{
    // Information from Inside Macintosh Volume II
    public partial class AppleMFS
    {
        public Errno ReadDir(string path, out List<string> contents)
        {
            contents = null;

            if(!mounted)
                return Errno.AccessDenied;

            if(!string.IsNullOrEmpty(path) &&
               string.Compare(path, "/", StringComparison.OrdinalIgnoreCase) != 0)
                return Errno.NotSupported;

            contents = idToFilename.Select(kvp => kvp.Value).ToList();

            if(debug)
            {
                contents.Add("$");
                contents.Add("$Bitmap");
                contents.Add("$MDB");

                if(bootBlocks != null)
                    contents.Add("$Boot");
            }

            contents.Sort();

            return Errno.NoError;
        }

        bool FillDirectory()
        {
            idToFilename = new Dictionary<uint, string>();
            idToEntry    = new Dictionary<uint, MFS_FileEntry>();
            filenameToId = new Dictionary<string, uint>();

            int offset = 0;

            while(offset + 51 < directoryBlocks.Length)
            {
                var entry = new MFS_FileEntry
                {
                    flFlags = (MFS_FileFlags)directoryBlocks[offset + 0]
                };

                if(!entry.flFlags.HasFlag(MFS_FileFlags.Used))
                    break;

                entry.flTyp = directoryBlocks[offset + 1];

                entry.flUsrWds =
                    Marshal.ByteArrayToStructureBigEndian<AppleCommon.FInfo>(directoryBlocks, offset + 2, 16);

                entry.flFlNum  = BigEndianBitConverter.ToUInt32(directoryBlocks, offset + 18);
                entry.flStBlk  = BigEndianBitConverter.ToUInt16(directoryBlocks, offset + 22);
                entry.flLgLen  = BigEndianBitConverter.ToUInt32(directoryBlocks, offset + 24);
                entry.flPyLen  = BigEndianBitConverter.ToUInt32(directoryBlocks, offset + 28);
                entry.flRStBlk = BigEndianBitConverter.ToUInt16(directoryBlocks, offset + 32);
                entry.flRLgLen = BigEndianBitConverter.ToUInt32(directoryBlocks, offset + 34);
                entry.flRPyLen = BigEndianBitConverter.ToUInt32(directoryBlocks, offset + 38);
                entry.flCrDat  = BigEndianBitConverter.ToUInt32(directoryBlocks, offset + 42);
                entry.flMdDat  = BigEndianBitConverter.ToUInt32(directoryBlocks, offset + 46);
                entry.flNam    = new byte[directoryBlocks[offset + 50] + 1];
                Array.Copy(directoryBlocks, offset + 50, entry.flNam, 0, entry.flNam.Length);

                string lowerFilename = StringHandlers.
                                       PascalToString(entry.flNam, Encoding).ToLowerInvariant().Replace('/', ':');

                if(entry.flFlags.HasFlag(MFS_FileFlags.Used) &&
                   !idToFilename.ContainsKey(entry.flFlNum)  &&
                   !idToEntry.ContainsKey(entry.flFlNum)     &&
                   !filenameToId.ContainsKey(lowerFilename)  &&
                   entry.flFlNum > 0)
                {
                    idToEntry.Add(entry.flFlNum, entry);

                    idToFilename.Add(entry.flFlNum,
                                     StringHandlers.PascalToString(entry.flNam, Encoding).Replace('/', ':'));

                    filenameToId.Add(lowerFilename, entry.flFlNum);

                    DicConsole.DebugWriteLine("DEBUG (AppleMFS plugin)", "entry.flFlags = {0}", entry.flFlags);
                    DicConsole.DebugWriteLine("DEBUG (AppleMFS plugin)", "entry.flTyp = {0}", entry.flTyp);
                    DicConsole.DebugWriteLine("DEBUG (AppleMFS plugin)", "entry.flFlNum = {0}", entry.flFlNum);
                    DicConsole.DebugWriteLine("DEBUG (AppleMFS plugin)", "entry.flStBlk = {0}", entry.flStBlk);
                    DicConsole.DebugWriteLine("DEBUG (AppleMFS plugin)", "entry.flLgLen = {0}", entry.flLgLen);
                    DicConsole.DebugWriteLine("DEBUG (AppleMFS plugin)", "entry.flPyLen = {0}", entry.flPyLen);
                    DicConsole.DebugWriteLine("DEBUG (AppleMFS plugin)", "entry.flRStBlk = {0}", entry.flRStBlk);
                    DicConsole.DebugWriteLine("DEBUG (AppleMFS plugin)", "entry.flRLgLen = {0}", entry.flRLgLen);
                    DicConsole.DebugWriteLine("DEBUG (AppleMFS plugin)", "entry.flRPyLen = {0}", entry.flRPyLen);

                    DicConsole.DebugWriteLine("DEBUG (AppleMFS plugin)", "entry.flCrDat = {0}",
                                              DateHandlers.MacToDateTime(entry.flCrDat));

                    DicConsole.DebugWriteLine("DEBUG (AppleMFS plugin)", "entry.flMdDat = {0}",
                                              DateHandlers.MacToDateTime(entry.flMdDat));

                    DicConsole.DebugWriteLine("DEBUG (AppleMFS plugin)", "entry.flNam0 = {0}",
                                              StringHandlers.PascalToString(entry.flNam, Encoding));
                }

                offset += 50 + entry.flNam.Length;

                // "Entries are always an integral number of words"
                if(offset % 2 != 0)
                    offset++;

                // TODO: "Entries don't cross logical block boundaries"
            }

            return true;
        }
    }
}