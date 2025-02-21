// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Data.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : CompactDisc dumping.
//
// --[ Description ] ----------------------------------------------------------
//
//     Dumps user data part.
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
// Copyright © 2011-2024 Natalia Portillo
// ****************************************************************************/

// ReSharper disable JoinDeclarationAndInitializer
// ReSharper disable InlineOutVariableDeclaration
// ReSharper disable TooWideLocalVariableScope

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aaru.Checksums;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Extents;
using Aaru.CommonTypes.Interfaces;
using Aaru.Console;
using Aaru.Core.Logging;
using Aaru.Decoders.CD;
using Aaru.Decoders.SCSI;
using Aaru.Devices;
using Humanizer;
using Humanizer.Bytes;
using Track = Aaru.CommonTypes.Structs.Track;
using TrackType = Aaru.CommonTypes.Enums.TrackType;

namespace Aaru.Core.Devices.Dumping;

partial class Dump
{
    /// <summary>Reads all CD user data</summary>
    /// <param name="audioExtents">Extents with audio sectors</param>
    /// <param name="blocks">Total number of positive sectors</param>
    /// <param name="blockSize">Size of the read sector in bytes</param>
    /// <param name="currentSpeed">Current read speed</param>
    /// <param name="currentTry">Current dump hardware try</param>
    /// <param name="extents">Extents</param>
    /// <param name="ibgLog">IMGBurn log</param>
    /// <param name="imageWriteDuration">Duration of image write</param>
    /// <param name="lastSector">Last sector number</param>
    /// <param name="leadOutExtents">Lead-out extents</param>
    /// <param name="maxSpeed">Maximum speed</param>
    /// <param name="mhddLog">MHDD log</param>
    /// <param name="minSpeed">Minimum speed</param>
    /// <param name="newTrim">Is trim a new one?</param>
    /// <param name="nextData">Next cluster of sectors is all data</param>
    /// <param name="offsetBytes">Read offset</param>
    /// <param name="read6">Device supports READ(6)</param>
    /// <param name="read10">Device supports READ(10)</param>
    /// <param name="read12">Device supports READ(12)</param>
    /// <param name="read16">Device supports READ(16)</param>
    /// <param name="readcd">Device supports READ CD</param>
    /// <param name="sectorsForOffset">Sectors needed to fix offset</param>
    /// <param name="subSize">Subchannel size in bytes</param>
    /// <param name="supportedSubchannel">Drive's maximum supported subchannel</param>
    /// <param name="supportsLongSectors">Supports reading EDC and ECC</param>
    /// <param name="totalDuration">Total commands duration</param>
    /// <param name="tracks">Disc tracks</param>
    /// <param name="subLog">Subchannel log</param>
    /// <param name="desiredSubchannel">Subchannel desired to save</param>
    /// <param name="isrcs">List of disc ISRCs</param>
    /// <param name="mcn">Disc media catalogue number</param>
    /// <param name="subchannelExtents">List of subchannels not yet dumped correctly</param>
    /// <param name="smallestPregapLbaPerTrack">List of smallest pregap relative address per track</param>
    void ReadCdData(ExtentsULong audioExtents, ulong blocks, uint blockSize, ref double currentSpeed,
                    DumpHardware currentTry, ExtentsULong extents, IbgLog ibgLog, ref double imageWriteDuration,
                    long lastSector, ExtentsULong leadOutExtents, ref double maxSpeed, MhddLog mhddLog,
                    ref double minSpeed, out bool newTrim, bool nextData, int offsetBytes, bool read6, bool read10,
                    bool read12, bool read16, bool readcd, int sectorsForOffset, uint subSize,
                    MmcSubchannel supportedSubchannel, bool supportsLongSectors, ref double totalDuration,
                    Track[] tracks, SubchannelLog subLog, MmcSubchannel desiredSubchannel,
                    Dictionary<byte, string> isrcs, ref string mcn, HashSet<int> subchannelExtents,
                    Dictionary<byte, int> smallestPregapLbaPerTrack)
    {
        ulong      sectorSpeedStart = 0; // Used to calculate correct speed
        uint       blocksToRead;         // How many sectors to read at once
        var        sense       = true;   // Sense indicator
        byte[]     cmdBuf      = null;   // Data buffer
        byte[]     senseBuf    = null;   // Sense buffer
        double     cmdDuration = 0;      // Command execution time
        const uint sectorSize  = 2352;   // Full sector size
        newTrim = false;
        PlextorSubchannel supportedPlextorSubchannel;
        var               outputFormat = _outputPlugin as IWritableImage;

        supportedPlextorSubchannel = supportedSubchannel switch
                                     {
                                         MmcSubchannel.None => PlextorSubchannel.None,
                                         MmcSubchannel.Raw  => PlextorSubchannel.Pack,
                                         MmcSubchannel.Q16  => PlextorSubchannel.Q16,
                                         _                  => PlextorSubchannel.None
                                     };

        InitProgress?.Invoke();

        int currentReadSpeed      = _speed;
        var crossingLeadOut       = false;
        var failedCrossingLeadOut = false;
        var skippingLead          = false;

        for(ulong i = _resume.NextBlock; (long)i <= lastSector; i += blocksToRead)
        {
            if(_aborted)
            {
                currentTry.Extents = ExtentsConverter.ToMetadata(extents);
                UpdateStatus?.Invoke(Localization.Core.Aborted);
                _dumpLog.WriteLine(Localization.Core.Aborted);

                break;
            }

            while(leadOutExtents.Contains(i))
            {
                skippingLead = true;
                i++;
            }

            if((long)i > lastSector) break;

            var firstSectorToRead = (uint)i;

            Track track = tracks.OrderBy(t => t.StartSector).LastOrDefault(t => i >= t.StartSector);

            blocksToRead = 0;
            bool inData = nextData;

            for(ulong j = i; j < i + _maximumReadable; j++)
            {
                if(j > (ulong)lastSector)
                {
                    if(!failedCrossingLeadOut && !inData) blocksToRead += (uint)sectorsForOffset;

                    if(sectorsForOffset > 0 && !inData) crossingLeadOut = true;

                    break;
                }

                if(nextData)
                {
                    if(audioExtents.Contains(j))
                    {
                        nextData = false;

                        break;
                    }

                    blocksToRead++;
                }
                else
                {
                    if(!audioExtents.Contains(j))
                    {
                        nextData = true;

                        break;
                    }

                    blocksToRead++;
                }
            }

            if(track.Sequence != 0 && i + blocksToRead - (ulong)sectorsForOffset > track.EndSector + 1)
                blocksToRead = (uint)(track.EndSector + 1 - i + (ulong)sectorsForOffset);

            if(blocksToRead == 1 && !inData) blocksToRead += (uint)sectorsForOffset;

            if(blocksToRead == 0)
            {
                if(!skippingLead) i += (ulong)sectorsForOffset;

                skippingLead = false;

                continue;
            }

            if(_fixOffset && !inData)
            {
                if(offsetBytes < 0)
                {
                    if(i == 0)
                        firstSectorToRead = uint.MaxValue - (uint)(sectorsForOffset - 1); // -1
                    else
                        firstSectorToRead -= (uint)sectorsForOffset;

                    if(blocksToRead <= sectorsForOffset) blocksToRead += (uint)sectorsForOffset;
                }
            }

            switch(inData)
            {
                case false when currentReadSpeed == 0xFFFF:
                    _dumpLog.WriteLine(Localization.Core.Setting_speed_to_8x_for_audio_reading);
                    UpdateStatus?.Invoke(Localization.Core.Setting_speed_to_8x_for_audio_reading);

                    _dev.SetCdSpeed(out _, RotationalControl.ClvAndImpureCav, 1416, 0, _dev.Timeout, out _);

                    currentReadSpeed = 1200;

                    break;
                case true when currentReadSpeed != _speed:
                {
                    _dumpLog.WriteLine(_speed == 0xFFFF
                                           ? Localization.Core.Setting_speed_to_MAX_for_data_reading
                                           : string.Format(Localization.Core.Setting_speed_to_0_x_for_data_reading,
                                                           _speed));

                    UpdateStatus?.Invoke(_speed == 0xFFFF
                                             ? Localization.Core.Setting_speed_to_MAX_for_data_reading
                                             : string.Format(Localization.Core.Setting_speed_to_0_x_for_data_reading,
                                                             _speed));

                    _speed *= _speedMultiplier;

                    if(_speed is 0 or > 0xFFFF) _speed = 0xFFFF;

                    currentReadSpeed = _speed;

                    _dev.SetCdSpeed(out _, RotationalControl.ClvAndImpureCav, (ushort)_speed, 0, _dev.Timeout, out _);

                    break;
                }
            }

            if(inData && crossingLeadOut)
            {
                firstSectorToRead = (uint)i;
                blocksToRead      = (uint)(lastSector - firstSectorToRead) + 1;
                crossingLeadOut   = false;
            }

            if(currentSpeed > maxSpeed && currentSpeed > 0) maxSpeed = currentSpeed;

            if(currentSpeed < minSpeed && currentSpeed > 0) minSpeed = currentSpeed;

            UpdateProgress?.Invoke(string.Format(Localization.Core.Reading_sector_0_of_1_2,
                                                 i,
                                                 blocks,
                                                 ByteSize.FromMegabytes(currentSpeed).Per(_oneSecond).Humanize()),
                                   (long)i,
                                   (long)blocks);

            if(crossingLeadOut && failedCrossingLeadOut && blocksToRead > 1) blocksToRead--;

            if(_supportsPlextorD8 && !inData)
            {
                _speedStopwatch.Start();

                sense = ReadPlextorWithSubchannel(out cmdBuf,
                                                  out senseBuf,
                                                  firstSectorToRead,
                                                  blockSize,
                                                  blocksToRead,
                                                  supportedPlextorSubchannel,
                                                  out cmdDuration);

                totalDuration += cmdDuration;
                _speedStopwatch.Stop();
            }
            else if(readcd)
            {
                if(inData)
                {
                    _speedStopwatch.Start();

                    sense = _dev.ReadCd(out cmdBuf,
                                        out senseBuf,
                                        firstSectorToRead,
                                        blockSize,
                                        blocksToRead,
                                        MmcSectorTypes.AllTypes,
                                        false,
                                        false,
                                        true,
                                        MmcHeaderCodes.AllHeaders,
                                        true,
                                        true,
                                        MmcErrorField.None,
                                        supportedSubchannel,
                                        _dev.Timeout,
                                        out cmdDuration);

                    _speedStopwatch.Stop();

                    if(sense)
                    {
                        DecodedSense? decSense = Sense.Decode(senseBuf);

                        // Try to workaround firmware
                        if(decSense?.ASC == 0x64)
                        {
                            var goBackTrackTypeChange = false;

                            // Go one for one as the drive does not tell us which one failed
                            for(var bi = 0; bi < blocksToRead; bi++)
                            {
                                _speedStopwatch.Start();

                                sense = _dev.ReadCd(out cmdBuf,
                                                    out senseBuf,
                                                    (uint)(firstSectorToRead + bi),
                                                    blockSize,
                                                    1,
                                                    MmcSectorTypes.AllTypes,
                                                    false,
                                                    false,
                                                    true,
                                                    MmcHeaderCodes.AllHeaders,
                                                    true,
                                                    true,
                                                    MmcErrorField.None,
                                                    supportedSubchannel,
                                                    _dev.Timeout,
                                                    out double cmdDuration2);

                                _speedStopwatch.Stop();

                                cmdDuration += cmdDuration2;

                                if(!sense             &&
                                   cmdBuf[0]  == 0x00 &&
                                   cmdBuf[1]  == 0xFF &&
                                   cmdBuf[2]  == 0xFF &&
                                   cmdBuf[3]  == 0xFF &&
                                   cmdBuf[4]  == 0xFF &&
                                   cmdBuf[5]  == 0xFF &&
                                   cmdBuf[6]  == 0xFF &&
                                   cmdBuf[7]  == 0xFF &&
                                   cmdBuf[8]  == 0xFF &&
                                   cmdBuf[9]  == 0xFF &&
                                   cmdBuf[10] == 0xFF &&
                                   cmdBuf[11] == 0x00)
                                    continue;

                                // Set those sectors as audio
                                for(int bip = bi; bip < blocksToRead; bip++)
                                    audioExtents.Add((ulong)(firstSectorToRead + bip));

                                goBackTrackTypeChange = true;

                                break;
                            }

                            // Go back to read again
                            if(goBackTrackTypeChange)
                            {
                                blocksToRead = 0;
                                nextData     = true;

                                continue;
                            }

                            // Drive definitively didn't like to read everything so just do something clever...

                            // Try again
                            _speedStopwatch.Start();

                            sense = _dev.ReadCd(out cmdBuf,
                                                out senseBuf,
                                                firstSectorToRead,
                                                blockSize,
                                                blocksToRead,
                                                MmcSectorTypes.AllTypes,
                                                false,
                                                false,
                                                true,
                                                MmcHeaderCodes.AllHeaders,
                                                true,
                                                true,
                                                MmcErrorField.None,
                                                supportedSubchannel,
                                                _dev.Timeout,
                                                out cmdDuration);

                            _speedStopwatch.Stop();

                            if(sense)

                                // Try reading one less every time
                            {
                                for(uint bi = blocksToRead; bi > 0; bi--)
                                {
                                    _speedStopwatch.Start();

                                    sense = _dev.ReadCd(out cmdBuf,
                                                        out senseBuf,
                                                        firstSectorToRead,
                                                        blockSize,
                                                        bi,
                                                        MmcSectorTypes.AllTypes,
                                                        false,
                                                        false,
                                                        true,
                                                        MmcHeaderCodes.AllHeaders,
                                                        true,
                                                        true,
                                                        MmcErrorField.None,
                                                        supportedSubchannel,
                                                        _dev.Timeout,
                                                        out cmdDuration);

                                    _speedStopwatch.Stop();

                                    if(sense) continue;

                                    blocksToRead = bi;

                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    _speedStopwatch.Start();

                    sense = _dev.ReadCd(out cmdBuf,
                                        out senseBuf,
                                        firstSectorToRead,
                                        blockSize,
                                        blocksToRead,
                                        MmcSectorTypes.Cdda,
                                        false,
                                        false,
                                        false,
                                        MmcHeaderCodes.None,
                                        true,
                                        false,
                                        MmcErrorField.None,
                                        supportedSubchannel,
                                        _dev.Timeout,
                                        out cmdDuration);

                    _speedStopwatch.Stop();

                    if(sense)
                    {
                        DecodedSense? decSense = Sense.Decode(senseBuf);

                        // Try to workaround firmware
                        if(decSense is { ASC: 0x11, ASCQ: 0x05 } || decSense?.ASC == 0x64)
                        {
                            _speedStopwatch.Start();

                            sense = _dev.ReadCd(out cmdBuf,
                                                out _,
                                                firstSectorToRead,
                                                blockSize,
                                                blocksToRead,
                                                MmcSectorTypes.AllTypes,
                                                false,
                                                false,
                                                true,
                                                MmcHeaderCodes.AllHeaders,
                                                true,
                                                true,
                                                MmcErrorField.None,
                                                supportedSubchannel,
                                                _dev.Timeout,
                                                out double cmdDuration2);

                            _speedStopwatch.Stop();

                            cmdDuration += cmdDuration2;
                        }
                    }
                }

                totalDuration += cmdDuration;
            }
            else if(read16)
            {
                _speedStopwatch.Start();

                sense = _dev.Read16(out cmdBuf,
                                    out senseBuf,
                                    0,
                                    false,
                                    false,
                                    false,
                                    firstSectorToRead,
                                    blockSize,
                                    0,
                                    blocksToRead,
                                    false,
                                    _dev.Timeout,
                                    out cmdDuration);

                _speedStopwatch.Stop();
            }
            else if(read12)
            {
                _speedStopwatch.Start();

                sense = _dev.Read12(out cmdBuf,
                                    out senseBuf,
                                    0,
                                    false,
                                    false,
                                    false,
                                    false,
                                    firstSectorToRead,
                                    blockSize,
                                    0,
                                    blocksToRead,
                                    false,
                                    _dev.Timeout,
                                    out cmdDuration);

                _speedStopwatch.Stop();
            }
            else if(read10)
            {
                _speedStopwatch.Start();

                sense = _dev.Read10(out cmdBuf,
                                    out senseBuf,
                                    0,
                                    false,
                                    false,
                                    false,
                                    false,
                                    firstSectorToRead,
                                    blockSize,
                                    0,
                                    (ushort)blocksToRead,
                                    _dev.Timeout,
                                    out cmdDuration);

                _speedStopwatch.Stop();
            }
            else if(read6)
            {
                _speedStopwatch.Start();

                sense = _dev.Read6(out cmdBuf,
                                   out senseBuf,
                                   firstSectorToRead,
                                   blockSize,
                                   (byte)blocksToRead,
                                   _dev.Timeout,
                                   out cmdDuration);

                _speedStopwatch.Stop();
            }

            double elapsed;

            // Overcome the track mode change drive error
            if(inData && !nextData && sense)
            {
                for(uint r = 0; r < blocksToRead; r++)
                {
                    UpdateProgress?.Invoke(string.Format(Localization.Core.Reading_sector_0_of_1_2,
                                                         i + r,
                                                         blocks,
                                                         ByteSize.FromMegabytes(currentSpeed)
                                                                 .Per(_oneSecond)
                                                                 .Humanize()),
                                           (long)i + r,
                                           (long)blocks);

                    if(_supportsPlextorD8)
                    {
                        var adjustment = 0;

                        if(offsetBytes < 0) adjustment = -sectorsForOffset;

                        _speedStopwatch.Start();

                        sense = ReadPlextorWithSubchannel(out cmdBuf,
                                                          out senseBuf,
                                                          (uint)(firstSectorToRead + r + adjustment),
                                                          blockSize,
                                                          (uint)sectorsForOffset + 1,
                                                          supportedPlextorSubchannel,
                                                          out cmdDuration);

                        _speedStopwatch.Stop();

                        totalDuration += cmdDuration;

                        if(!sense)
                        {
                            var sectorsForFix = (uint)(1 + sectorsForOffset);

                            FixOffsetData(offsetBytes,
                                          sectorSize,
                                          sectorsForOffset,
                                          supportedSubchannel,
                                          ref sectorsForFix,
                                          subSize,
                                          ref cmdBuf,
                                          blockSize,
                                          false);

                            // Descramble
                            cmdBuf = Sector.Scramble(cmdBuf);

                            // Check valid sector
                            CdChecksums.CheckCdSector(cmdBuf,
                                                      out bool? correctEccP,
                                                      out bool? correctEccQ,
                                                      out bool? correctEdc);

                            // Check mode, set sense if EDC/ECC validity is not correct
                            switch(cmdBuf[15] & 0x03)
                            {
                                case 0:

                                    for(var c = 16; c < 2352; c++)
                                    {
                                        if(cmdBuf[c] == 0x00) continue;

                                        sense = true;

                                        break;
                                    }

                                    break;
                                case 1:
                                    sense = correctEdc != true || correctEccP != true || correctEccQ != true;

                                    break;
                                case 2:
                                    if((cmdBuf[18] & 0x20) != 0x20)
                                    {
                                        if(correctEccP != true) sense = true;

                                        if(correctEccQ != true) sense = true;
                                    }

                                    if(correctEdc != true) sense = true;

                                    break;
                            }
                        }
                    }
                    else if(readcd)
                    {
                        _speedStopwatch.Start();

                        sense = _dev.ReadCd(out cmdBuf,
                                            out senseBuf,
                                            (uint)(i + r),
                                            blockSize,
                                            1,
                                            MmcSectorTypes.AllTypes,
                                            false,
                                            false,
                                            true,
                                            MmcHeaderCodes.AllHeaders,
                                            true,
                                            true,
                                            MmcErrorField.None,
                                            supportedSubchannel,
                                            _dev.Timeout,
                                            out cmdDuration);

                        _speedStopwatch.Stop();

                        totalDuration += cmdDuration;
                    }
                    else if(read16)
                    {
                        _speedStopwatch.Start();

                        sense = _dev.Read16(out cmdBuf,
                                            out senseBuf,
                                            0,
                                            false,
                                            true,
                                            false,
                                            i + r,
                                            blockSize,
                                            0,
                                            1,
                                            false,
                                            _dev.Timeout,
                                            out cmdDuration);

                        _speedStopwatch.Stop();
                    }
                    else if(read12)
                    {
                        _speedStopwatch.Start();

                        sense = _dev.Read12(out cmdBuf,
                                            out senseBuf,
                                            0,
                                            false,
                                            true,
                                            false,
                                            false,
                                            (uint)(i + r),
                                            blockSize,
                                            0,
                                            1,
                                            false,
                                            _dev.Timeout,
                                            out cmdDuration);

                        _speedStopwatch.Stop();
                    }
                    else if(read10)
                    {
                        _speedStopwatch.Start();

                        sense = _dev.Read10(out cmdBuf,
                                            out senseBuf,
                                            0,
                                            false,
                                            true,
                                            false,
                                            false,
                                            (uint)(i + r),
                                            blockSize,
                                            0,
                                            1,
                                            _dev.Timeout,
                                            out cmdDuration);

                        _speedStopwatch.Stop();
                    }
                    else if(read6)
                    {
                        _speedStopwatch.Start();

                        sense = _dev.Read6(out cmdBuf,
                                           out senseBuf,
                                           (uint)(i + r),
                                           blockSize,
                                           1,
                                           _dev.Timeout,
                                           out cmdDuration);

                        _speedStopwatch.Stop();
                    }

                    if(!sense && !_dev.Error)
                    {
                        mhddLog.Write(i + r, cmdDuration);
                        ibgLog.Write(i  + r, currentSpeed * 1024);
                        extents.Add(i   + r, 1, true);
                        _writeStopwatch.Restart();

                        if(supportedSubchannel != MmcSubchannel.None)
                        {
                            var data = new byte[sectorSize];
                            var sub  = new byte[subSize];

                            Array.Copy(cmdBuf, 0, data, 0, sectorSize);

                            Array.Copy(cmdBuf, sectorSize, sub, 0, subSize);

                            if(supportsLongSectors)
                                outputFormat.WriteSectorsLong(data, i + r, 1);
                            else
                            {
                                var cooked = new MemoryStream();
                                var sector = new byte[sectorSize];

                                for(var b = 0; b < blocksToRead; b++)
                                {
                                    Array.Copy(cmdBuf, (int)(0 + b * blockSize), sector, 0, sectorSize);
                                    byte[] cookedSector = Sector.GetUserData(sector);
                                    cooked.Write(cookedSector, 0, cookedSector.Length);
                                }

                                outputFormat.WriteSectors(cooked.ToArray(), i, blocksToRead);
                            }

                            bool indexesChanged = Media.CompactDisc.WriteSubchannelToImage(supportedSubchannel,
                                desiredSubchannel,
                                sub,
                                i + r,
                                1,
                                subLog,
                                isrcs,
                                (byte)track.Sequence,
                                ref mcn,
                                tracks,
                                subchannelExtents,
                                _fixSubchannelPosition,
                                outputFormat as IWritableOpticalImage,
                                _fixSubchannel,
                                _fixSubchannelCrc,
                                _dumpLog,
                                UpdateStatus,
                                smallestPregapLbaPerTrack,
                                true,
                                out List<ulong> newPregapSectors);

                            // Set tracks and go back
                            if(indexesChanged)
                            {
                                (outputFormat as IWritableOpticalImage).SetTracks(tracks.ToList());

                                foreach(ulong newPregapSector in newPregapSectors)
                                    _resume.BadBlocks.Add(newPregapSector);

                                if(i >= blocksToRead)
                                    i -= blocksToRead;
                                else
                                    i = 0;

                                if(i > 0) i--;

                                foreach(Track aTrack in tracks.Where(aTrack => aTrack.Type == TrackType.Audio))
                                    audioExtents.Add(aTrack.StartSector, aTrack.EndSector);

                                continue;
                            }
                        }
                        else
                        {
                            if(supportsLongSectors)
                                outputFormat.WriteSectorsLong(cmdBuf, i + r, 1);
                            else
                            {
                                var cooked = new MemoryStream();
                                var sector = new byte[sectorSize];

                                for(var b = 0; b < blocksToRead; b++)
                                {
                                    Array.Copy(cmdBuf, (int)(b * sectorSize), sector, 0, sectorSize);
                                    byte[] cookedSector = Sector.GetUserData(sector);
                                    cooked.Write(cookedSector, 0, cookedSector.Length);
                                }

                                outputFormat.WriteSectors(cooked.ToArray(), i, blocksToRead);
                            }
                        }

                        _mediaGraph?.PaintSectorGood(i + r);

                        imageWriteDuration += _writeStopwatch.Elapsed.TotalSeconds;
                    }
                    else
                    {
                        _errorLog?.WriteLine(i + r, _dev.Error, _dev.LastError, senseBuf);

                        // Write empty data
                        _writeStopwatch.Restart();

                        if(supportedSubchannel != MmcSubchannel.None)
                        {
                            outputFormat.WriteSectorsLong(new byte[sectorSize], i + r, 1);

                            if(desiredSubchannel != MmcSubchannel.None)
                            {
                                outputFormat.WriteSectorsTag(new byte[subSize],
                                                             i + r,
                                                             1,
                                                             SectorTagType.CdSectorSubchannel);
                            }
                        }
                        else
                        {
                            if(supportsLongSectors)
                                outputFormat.WriteSectorsLong(new byte[blockSize], i + r, 1);
                            else
                            {
                                if(cmdBuf.Length % sectorSize == 0)
                                    outputFormat.WriteSectors(new byte[2048], i + r, 1);
                                else
                                    outputFormat.WriteSectorsLong(new byte[blockSize], i + r, 1);
                            }
                        }

                        imageWriteDuration += _writeStopwatch.Elapsed.TotalSeconds;

                        _mediaGraph?.PaintSectorBad(i + r);

                        _resume.BadBlocks.Add(i + r);

                        AaruConsole.DebugWriteLine(MODULE_NAME,
                                                   Localization.Core.READ_error_0,
                                                   Sense.PrettifySense(senseBuf));

                        mhddLog.Write(i + r, cmdDuration < 500 ? 65535 : cmdDuration);

                        ibgLog.Write(i                                                                    + r, 0);
                        _dumpLog.WriteLine(Localization.Core.Skipping_0_blocks_from_errored_block_1, 1, i + r);
                        newTrim = true;
                    }

                    _writeStopwatch.Stop();

                    sectorSpeedStart += r;

                    _resume.NextBlock = i + r;

                    elapsed = _speedStopwatch.Elapsed.TotalSeconds;

                    if(elapsed <= 0 || sectorSpeedStart * blockSize < 524288) continue;

                    currentSpeed     = sectorSpeedStart * blockSize / (1048576 * elapsed);
                    sectorSpeedStart = 0;
                    _speedStopwatch.Reset();
                }

                continue;
            }

            if(!sense && !_dev.Error)
            {
                if(crossingLeadOut && failedCrossingLeadOut)
                {
                    var tmp = new byte[cmdBuf.Length + blockSize];
                    Array.Copy(cmdBuf, 0, tmp, 0, cmdBuf.Length);
                }

                // Because one block has been partially used to fix the offset
                if(_fixOffset && !inData && offsetBytes != 0)
                {
                    FixOffsetData(offsetBytes,
                                  sectorSize,
                                  sectorsForOffset,
                                  supportedSubchannel,
                                  ref blocksToRead,
                                  subSize,
                                  ref cmdBuf,
                                  blockSize,
                                  failedCrossingLeadOut);
                }

                mhddLog.Write(i, cmdDuration, blocksToRead);
                ibgLog.Write(i, currentSpeed * 1024);
                extents.Add(i, blocksToRead, true);
                _writeStopwatch.Restart();

                if(supportedSubchannel != MmcSubchannel.None)
                {
                    var data = new byte[sectorSize * blocksToRead];
                    var sub  = new byte[subSize    * blocksToRead];

                    for(var b = 0; b < blocksToRead; b++)
                    {
                        Array.Copy(cmdBuf, (int)(0 + b * blockSize), data, sectorSize * b, sectorSize);

                        Array.Copy(cmdBuf, (int)(sectorSize + b * blockSize), sub, subSize * b, subSize);
                    }

                    if(supportsLongSectors)
                        outputFormat.WriteSectorsLong(data, i, blocksToRead);
                    else
                    {
                        var cooked = new MemoryStream();
                        var sector = new byte[sectorSize];

                        for(var b = 0; b < blocksToRead; b++)
                        {
                            Array.Copy(cmdBuf, (int)(0 + b * blockSize), sector, 0, sectorSize);
                            byte[] cookedSector = Sector.GetUserData(sector);
                            cooked.Write(cookedSector, 0, cookedSector.Length);
                        }

                        outputFormat.WriteSectors(cooked.ToArray(), i, blocksToRead);
                    }

                    bool indexesChanged = Media.CompactDisc.WriteSubchannelToImage(supportedSubchannel,
                        desiredSubchannel,
                        sub,
                        i,
                        blocksToRead,
                        subLog,
                        isrcs,
                        (byte)track.Sequence,
                        ref mcn,
                        tracks,
                        subchannelExtents,
                        _fixSubchannelPosition,
                        outputFormat as IWritableOpticalImage,
                        _fixSubchannel,
                        _fixSubchannelCrc,
                        _dumpLog,
                        UpdateStatus,
                        smallestPregapLbaPerTrack,
                        true,
                        out List<ulong> newPregapSectors);

                    // Set tracks and go back
                    if(indexesChanged)
                    {
                        (outputFormat as IWritableOpticalImage).SetTracks(tracks.ToList());

                        foreach(ulong newPregapSector in newPregapSectors) _resume.BadBlocks.Add(newPregapSector);

                        if(i >= blocksToRead)
                            i -= blocksToRead;
                        else
                            i = 0;

                        if(i > 0) i--;

                        foreach(Track aTrack in tracks.Where(aTrack => aTrack.Type == TrackType.Audio))
                            audioExtents.Add(aTrack.StartSector, aTrack.EndSector);

                        continue;
                    }
                }
                else
                {
                    if(supportsLongSectors)
                        outputFormat.WriteSectorsLong(cmdBuf, i, blocksToRead);
                    else
                    {
                        var cooked = new MemoryStream();
                        var sector = new byte[sectorSize];

                        for(var b = 0; b < blocksToRead; b++)
                        {
                            Array.Copy(cmdBuf, (int)(b * sectorSize), sector, 0, sectorSize);
                            byte[] cookedSector = Sector.GetUserData(sector);
                            cooked.Write(cookedSector, 0, cookedSector.Length);
                        }

                        outputFormat.WriteSectors(cooked.ToArray(), i, blocksToRead);
                    }
                }

                _mediaGraph?.PaintSectorsGood(i, blocksToRead);

                imageWriteDuration += _writeStopwatch.Elapsed.TotalSeconds;
            }
            else
            {
                if(crossingLeadOut && Sense.Decode(senseBuf)?.ASC == 0x21)
                {
                    if(failedCrossingLeadOut) break;

                    failedCrossingLeadOut = true;
                    blocksToRead          = 0;

                    continue;
                }

                _errorLog?.WriteLine(firstSectorToRead, _dev.Error, _dev.LastError, senseBuf);

                // TODO: Reset device after X errors
                if(_stopOnError) return; // TODO: Return more cleanly

                if(i + _skip > blocks) _skip = (uint)(blocks - i);

                // Write empty data
                _writeStopwatch.Restart();

                if(supportedSubchannel != MmcSubchannel.None)
                {
                    outputFormat.WriteSectorsLong(new byte[sectorSize * _skip], i, _skip);

                    if(desiredSubchannel != MmcSubchannel.None)
                    {
                        outputFormat.WriteSectorsTag(new byte[subSize * _skip],
                                                     i,
                                                     _skip,
                                                     SectorTagType.CdSectorSubchannel);
                    }
                }
                else
                {
                    if(supportsLongSectors)
                        outputFormat.WriteSectorsLong(new byte[blockSize * _skip], i, _skip);
                    else
                    {
                        if(cmdBuf.Length % sectorSize == 0)
                            outputFormat.WriteSectors(new byte[2048 * _skip], i, _skip);
                        else
                            outputFormat.WriteSectorsLong(new byte[blockSize * _skip], i, _skip);
                    }
                }

                imageWriteDuration += _writeStopwatch.Elapsed.TotalSeconds;

                for(ulong b = i; b < i + _skip; b++) _resume.BadBlocks.Add(b);

                AaruConsole.DebugWriteLine(MODULE_NAME, Localization.Core.READ_error_0, Sense.PrettifySense(senseBuf));
                mhddLog.Write(i, cmdDuration < 500 ? 65535 : cmdDuration, _skip);

                ibgLog.Write(i, 0);
                _dumpLog.WriteLine(Localization.Core.Skipping_0_blocks_from_errored_block_1, _skip, i);
                i       += _skip - blocksToRead;
                newTrim =  true;
            }

            _writeStopwatch.Stop();

            sectorSpeedStart += blocksToRead;

            _resume.NextBlock = i + blocksToRead;

            elapsed = _speedStopwatch.Elapsed.TotalSeconds;

            if(elapsed <= 0 || sectorSpeedStart * blockSize < 524288) continue;

            currentSpeed     = sectorSpeedStart * blockSize / (1048576 * elapsed);
            sectorSpeedStart = 0;
            _speedStopwatch.Reset();
        }

        _speedStopwatch.Stop();
        EndProgress?.Invoke();

        _resume.BadBlocks = _resume.BadBlocks.Distinct().ToList();

        if(!failedCrossingLeadOut) return;

        _dumpLog.WriteLine(Localization.Core.Failed_crossing_into_Lead_Out);
        UpdateStatus?.Invoke(Localization.Core.Failed_crossing_into_Lead_Out);
    }
}