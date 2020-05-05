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
// Copyright © 2011-2020 Natalia Portillo
// ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Extents;
using Aaru.CommonTypes.Structs;
using Aaru.Console;
using Aaru.Core.Logging;
using Aaru.Decoders.CD;
using Aaru.Decoders.SCSI;
using Aaru.Devices;
using Schemas;

// ReSharper disable JoinDeclarationAndInitializer
// ReSharper disable InlineOutVariableDeclaration
// ReSharper disable TooWideLocalVariableScope

namespace Aaru.Core.Devices.Dumping
{
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
        void ReadCdData(ExtentsULong audioExtents, ulong blocks, uint blockSize, ref double currentSpeed,
                        DumpHardwareType currentTry, ExtentsULong extents, IbgLog ibgLog, ref double imageWriteDuration,
                        long lastSector, ExtentsULong leadOutExtents, ref double maxSpeed, MhddLog mhddLog,
                        ref double minSpeed, out bool newTrim, bool nextData, int offsetBytes, bool read6, bool read10,
                        bool read12, bool read16, bool readcd, int sectorsForOffset, uint subSize,
                        MmcSubchannel supportedSubchannel, bool supportsLongSectors, ref double totalDuration,
                        Track[] tracks, SubchannelLog subLog, MmcSubchannel desiredSubchannel,
                        Dictionary<byte, string> isrcs)
        {
            ulong      sectorSpeedStart = 0;               // Used to calculate correct speed
            DateTime   timeSpeedStart   = DateTime.UtcNow; // Time of start for speed calculation
            uint       blocksToRead     = 0;               // How many sectors to read at once
            bool       sense            = true;            // Sense indicator
            byte[]     cmdBuf           = null;            // Data buffer
            byte[]     senseBuf         = null;            // Sense buffer
            double     cmdDuration      = 0;               // Command execution time
            const uint sectorSize       = 2352;            // Full sector size
            newTrim = false;
            PlextorSubchannel supportedPlextorSubchannel;

            switch(supportedSubchannel)
            {
                case MmcSubchannel.None:
                    supportedPlextorSubchannel = PlextorSubchannel.None;

                    break;
                case MmcSubchannel.Raw:
                    supportedPlextorSubchannel = PlextorSubchannel.All;

                    break;
                case MmcSubchannel.Q16:
                    supportedPlextorSubchannel = PlextorSubchannel.Q16;

                    break;
                case MmcSubchannel.Rw:
                    supportedPlextorSubchannel = PlextorSubchannel.Pack;

                    break;
                default:
                    supportedPlextorSubchannel = PlextorSubchannel.None;

                    break;
            }

            InitProgress?.Invoke();

            int  currentReadSpeed      = _speed;
            bool crossingLeadOut       = false;
            bool failedCrossingLeadOut = false;

            for(ulong i = _resume.NextBlock; (long)i <= lastSector; i += blocksToRead)
            {
                if(_aborted)
                {
                    currentTry.Extents = ExtentsConverter.ToMetadata(extents);
                    UpdateStatus?.Invoke("Aborted!");
                    _dumpLog.WriteLine("Aborted!");

                    break;
                }

                while(leadOutExtents.Contains(i))
                {
                    i++;
                }

                if((long)i > lastSector)
                    break;

                uint firstSectorToRead = (uint)i;

                Track track = tracks.OrderBy(t => t.TrackStartSector).LastOrDefault(t => i >= t.TrackStartSector);

                blocksToRead = 0;
                bool inData = nextData;

                for(ulong j = i; j < i + _maximumReadable; j++)
                {
                    if(j > (ulong)lastSector)
                    {
                        if(!failedCrossingLeadOut)
                            blocksToRead += (uint)sectorsForOffset;

                        if(sectorsForOffset > 0)
                            crossingLeadOut = true;

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

                if(track.TrackSequence                          != 0 &&
                   (i + blocksToRead) - (ulong)sectorsForOffset > track.TrackEndSector + 1)
                    blocksToRead = (uint)(((track.TrackEndSector + 1) - i) + (ulong)sectorsForOffset);

                if(blocksToRead == 1 &&
                   !inData)
                    blocksToRead += (uint)sectorsForOffset;

                if(_fixOffset && !inData)
                {
                    // TODO: FreeBSD bug
                    if(offsetBytes < 0)
                    {
                        if(i == 0)
                            firstSectorToRead = uint.MaxValue - (uint)(sectorsForOffset - 1); // -1
                        else
                            firstSectorToRead -= (uint)sectorsForOffset;
                    }
                }

                if(!inData &&
                   currentReadSpeed == 0xFFFF)
                {
                    _dumpLog.WriteLine("Setting speed to 8x for audio reading.");
                    UpdateStatus?.Invoke("Setting speed to 8x for audio reading.");

                    _dev.SetCdSpeed(out _, RotationalControl.ClvAndImpureCav, 1416, 0, _dev.Timeout, out _);

                    currentReadSpeed = 1200;
                }

                if(inData && currentReadSpeed != _speed)
                {
                    _dumpLog.WriteLine($"Setting speed to {(_speed == 0xFFFF ? "MAX for data reading" : $"{_speed}x")}.");

                    UpdateStatus?.
                        Invoke($"Setting speed to {(_speed == 0xFFFF ? "MAX for data reading" : $"{_speed}x")}.");

                    _speed *= _speedMultiplier;

                    if(_speed == 0 ||
                       _speed > 0xFFFF)
                        _speed = 0xFFFF;

                    currentReadSpeed = _speed;

                    _dev.SetCdSpeed(out _, RotationalControl.ClvAndImpureCav, (ushort)_speed, 0, _dev.Timeout, out _);
                }

                if(inData && crossingLeadOut)
                {
                    firstSectorToRead = (uint)i;
                    blocksToRead      = (uint)(lastSector - firstSectorToRead) + 1;
                    crossingLeadOut   = false;
                }

                #pragma warning disable RECS0018 // Comparison of floating point numbers with equality operator

                // ReSharper disable CompareOfFloatsByEqualityOperator
                if(currentSpeed > maxSpeed &&
                   currentSpeed != 0)
                    maxSpeed = currentSpeed;

                if(currentSpeed < minSpeed &&
                   currentSpeed != 0)
                    minSpeed = currentSpeed;

                // ReSharper restore CompareOfFloatsByEqualityOperator

                #pragma warning restore RECS0018 // Comparison of floating point numbers with equality operator

                UpdateProgress?.Invoke($"Reading sector {i} of {blocks} ({currentSpeed:F3} MiB/sec.)", (long)i,
                                       (long)blocks);

                if(_supportsPlextorD8 && !inData)
                {
                    sense = ReadPlextorWithSubchannel(out cmdBuf, out senseBuf, firstSectorToRead, blockSize,
                                                      blocksToRead, supportedPlextorSubchannel, out cmdDuration);

                    totalDuration += cmdDuration;
                }
                else if(readcd)
                {
                    sense = _dev.ReadCd(out cmdBuf, out senseBuf, firstSectorToRead, blockSize, blocksToRead,
                                        MmcSectorTypes.AllTypes, false, false, true, MmcHeaderCodes.AllHeaders, true,
                                        true, MmcErrorField.None, supportedSubchannel, _dev.Timeout, out cmdDuration);

                    totalDuration += cmdDuration;
                }
                else if(read16)
                {
                    sense = _dev.Read16(out cmdBuf, out senseBuf, 0, false, true, false, firstSectorToRead, blockSize,
                                        0, blocksToRead, false, _dev.Timeout, out cmdDuration);
                }
                else if(read12)
                {
                    sense = _dev.Read12(out cmdBuf, out senseBuf, 0, false, true, false, false, firstSectorToRead,
                                        blockSize, 0, blocksToRead, false, _dev.Timeout, out cmdDuration);
                }
                else if(read10)
                {
                    sense = _dev.Read10(out cmdBuf, out senseBuf, 0, false, true, false, false, firstSectorToRead,
                                        blockSize, 0, (ushort)blocksToRead, _dev.Timeout, out cmdDuration);
                }
                else if(read6)
                {
                    sense = _dev.Read6(out cmdBuf, out senseBuf, firstSectorToRead, blockSize, (byte)blocksToRead,
                                       _dev.Timeout, out cmdDuration);
                }

                double elapsed;

                // Overcome the track mode change drive error
                if(inData    &&
                   !nextData &&
                   sense)
                {
                    for(uint r = 0; r < blocksToRead; r++)
                    {
                        UpdateProgress?.Invoke($"Reading sector {i} of {blocks} ({currentSpeed:F3} MiB/sec.)",
                                               (long)i + r, (long)blocks);

                        if(_supportsPlextorD8)
                        {
                            int adjustment = 0;

                            if(offsetBytes < 0)
                                adjustment = -sectorsForOffset;

                            sense = ReadPlextorWithSubchannel(out cmdBuf, out senseBuf,
                                                              (uint)(firstSectorToRead + r + adjustment), blockSize,
                                                              (uint)sectorsForOffset + 1, supportedPlextorSubchannel,
                                                              out cmdDuration);

                            totalDuration += cmdDuration;

                            if(!sense)
                            {
                                uint sectorsForFix = (uint)(1 + sectorsForOffset);

                                FixOffsetData(offsetBytes, sectorSize, sectorsForOffset, supportedSubchannel,
                                              ref sectorsForFix, subSize, ref cmdBuf, blockSize, false);

                                // TODO: Implement sector validity
                                cmdBuf = Sector.Scramble(cmdBuf);
                            }
                        }
                        else if(readcd)
                        {
                            sense = _dev.ReadCd(out cmdBuf, out senseBuf, (uint)(i + r), blockSize, 1,
                                                MmcSectorTypes.AllTypes, false, false, true, MmcHeaderCodes.AllHeaders,
                                                true, true, MmcErrorField.None, supportedSubchannel, _dev.Timeout,
                                                out cmdDuration);

                            totalDuration += cmdDuration;
                        }
                        else if(read16)
                        {
                            sense = _dev.Read16(out cmdBuf, out senseBuf, 0, false, true, false, i + r, blockSize, 0, 1,
                                                false, _dev.Timeout, out cmdDuration);
                        }
                        else if(read12)
                        {
                            sense = _dev.Read12(out cmdBuf, out senseBuf, 0, false, true, false, false, (uint)(i + r),
                                                blockSize, 0, 1, false, _dev.Timeout, out cmdDuration);
                        }
                        else if(read10)
                        {
                            sense = _dev.Read10(out cmdBuf, out senseBuf, 0, false, true, false, false, (uint)(i + r),
                                                blockSize, 0, 1, _dev.Timeout, out cmdDuration);
                        }
                        else if(read6)
                        {
                            sense = _dev.Read6(out cmdBuf, out senseBuf, (uint)(i + r), blockSize, 1, _dev.Timeout,
                                               out cmdDuration);
                        }

                        if(!sense &&
                           !_dev.Error)
                        {
                            mhddLog.Write(i + r, cmdDuration);
                            ibgLog.Write(i  + r, currentSpeed * 1024);
                            extents.Add(i   + r, 1, true);
                            DateTime writeStart = DateTime.Now;

                            if(supportedSubchannel != MmcSubchannel.None)
                            {
                                byte[] data = new byte[sectorSize];
                                byte[] sub  = new byte[subSize];

                                Array.Copy(cmdBuf, 0, data, 0, sectorSize);

                                Array.Copy(cmdBuf, sectorSize, sub, 0, subSize);

                                _outputPlugin.WriteSectorsLong(data, i + r, 1);

                                WriteSubchannelToImage(supportedSubchannel, desiredSubchannel, sub, i + r, 1, subLog,
                                                       isrcs, (byte)track.TrackSequence);
                            }
                            else
                            {
                                if(supportsLongSectors)
                                {
                                    _outputPlugin.WriteSectorsLong(cmdBuf, i + r, 1);
                                }
                                else
                                {
                                    if(cmdBuf.Length % sectorSize == 0)
                                    {
                                        byte[] data = new byte[2048];

                                        Array.Copy(cmdBuf, 16, data, 2048, 2048);

                                        _outputPlugin.WriteSectors(data, i + r, 1);
                                    }
                                    else
                                    {
                                        _outputPlugin.WriteSectorsLong(cmdBuf, i + r, 1);
                                    }
                                }
                            }

                            imageWriteDuration += (DateTime.Now - writeStart).TotalSeconds;
                        }
                        else
                        {
                            // Write empty data
                            DateTime writeStart = DateTime.Now;

                            if(supportedSubchannel != MmcSubchannel.None)
                            {
                                _outputPlugin.WriteSectorsLong(new byte[sectorSize], i + r, 1);

                                if(desiredSubchannel != MmcSubchannel.None)
                                    _outputPlugin.WriteSectorsTag(new byte[subSize], i + r, 1,
                                                                  SectorTagType.CdSectorSubchannel);
                            }
                            else
                            {
                                if(supportsLongSectors)
                                {
                                    _outputPlugin.WriteSectorsLong(new byte[blockSize], i + r, 1);
                                }
                                else
                                {
                                    if(cmdBuf.Length % sectorSize == 0)
                                        _outputPlugin.WriteSectors(new byte[2048], i + r, 1);
                                    else
                                        _outputPlugin.WriteSectorsLong(new byte[blockSize], i + r, 1);
                                }
                            }

                            imageWriteDuration += (DateTime.Now - writeStart).TotalSeconds;

                            _resume.BadBlocks.Add(i + r);

                            AaruConsole.DebugWriteLine("Dump-Media", "READ error:\n{0}", Sense.PrettifySense(senseBuf));
                            mhddLog.Write(i + r, cmdDuration < 500 ? 65535 : cmdDuration);

                            ibgLog.Write(i                                                         + r, 0);
                            _dumpLog.WriteLine("Skipping {0} blocks from errored block {1}.", 1, i + r);
                            newTrim = true;
                        }

                        sectorSpeedStart += blocksToRead;

                        _resume.NextBlock = i + blocksToRead;

                        elapsed = (DateTime.UtcNow - timeSpeedStart).TotalSeconds;

                        if(elapsed < 1)
                            continue;

                        currentSpeed     = (sectorSpeedStart * blockSize) / (1048576 * elapsed);
                        sectorSpeedStart = 0;
                        timeSpeedStart   = DateTime.UtcNow;
                    }

                    continue;
                }

                if(!sense &&
                   !_dev.Error)
                {
                    // Because one block has been partially used to fix the offset
                    if(_fixOffset &&
                       !inData    &&
                       offsetBytes != 0)
                        FixOffsetData(offsetBytes, sectorSize, sectorsForOffset, supportedSubchannel, ref blocksToRead,
                                      subSize, ref cmdBuf, blockSize, failedCrossingLeadOut);

                    mhddLog.Write(i, cmdDuration);
                    ibgLog.Write(i, currentSpeed * 1024);
                    extents.Add(i, blocksToRead, true);
                    DateTime writeStart = DateTime.Now;

                    if(supportedSubchannel != MmcSubchannel.None)
                    {
                        byte[] data = new byte[sectorSize * blocksToRead];
                        byte[] sub  = new byte[subSize    * blocksToRead];

                        for(int b = 0; b < blocksToRead; b++)
                        {
                            Array.Copy(cmdBuf, (int)(0 + (b * blockSize)), data, sectorSize * b, sectorSize);

                            Array.Copy(cmdBuf, (int)(sectorSize + (b * blockSize)), sub, subSize * b, subSize);
                        }

                        _outputPlugin.WriteSectorsLong(data, i, blocksToRead);

                        WriteSubchannelToImage(supportedSubchannel, desiredSubchannel, sub, i, blocksToRead, subLog,
                                               isrcs, (byte)track.TrackSequence);
                    }
                    else
                    {
                        if(supportsLongSectors)
                        {
                            _outputPlugin.WriteSectorsLong(cmdBuf, i, blocksToRead);
                        }
                        else
                        {
                            if(cmdBuf.Length % sectorSize == 0)
                            {
                                byte[] data = new byte[2048 * blocksToRead];

                                for(int b = 0; b < blocksToRead; b++)
                                    Array.Copy(cmdBuf, (int)(16 + (b * blockSize)), data, 2048 * b, 2048);

                                _outputPlugin.WriteSectors(data, i, blocksToRead);
                            }
                            else
                            {
                                _outputPlugin.WriteSectorsLong(cmdBuf, i, blocksToRead);
                            }
                        }
                    }

                    imageWriteDuration += (DateTime.Now - writeStart).TotalSeconds;
                }
                else
                {
                    if(crossingLeadOut && Sense.DecodeFixed(senseBuf)?.ASC == 0x21)
                    {
                        failedCrossingLeadOut = true;
                        blocksToRead          = 0;

                        continue;
                    }

                    // TODO: Reset device after X errors
                    if(_stopOnError)
                        return; // TODO: Return more cleanly

                    if(i + _skip > blocks)
                        _skip = (uint)(blocks - i);

                    // Write empty data
                    DateTime writeStart = DateTime.Now;

                    if(supportedSubchannel != MmcSubchannel.None)
                    {
                        _outputPlugin.WriteSectorsLong(new byte[sectorSize * _skip], i, _skip);

                        if(desiredSubchannel != MmcSubchannel.None)
                            _outputPlugin.WriteSectorsTag(new byte[subSize * _skip], i, _skip,
                                                          SectorTagType.CdSectorSubchannel);
                    }
                    else
                    {
                        if(supportsLongSectors)
                        {
                            _outputPlugin.WriteSectorsLong(new byte[blockSize * _skip], i, _skip);
                        }
                        else
                        {
                            if(cmdBuf.Length % sectorSize == 0)
                                _outputPlugin.WriteSectors(new byte[2048 * _skip], i, _skip);
                            else
                                _outputPlugin.WriteSectorsLong(new byte[blockSize * _skip], i, _skip);
                        }
                    }

                    imageWriteDuration += (DateTime.Now - writeStart).TotalSeconds;

                    for(ulong b = i; b < i + _skip; b++)
                        _resume.BadBlocks.Add(b);

                    AaruConsole.DebugWriteLine("Dump-Media", "READ error:\n{0}", Sense.PrettifySense(senseBuf));
                    mhddLog.Write(i, cmdDuration < 500 ? 65535 : cmdDuration);

                    ibgLog.Write(i, 0);
                    _dumpLog.WriteLine("Skipping {0} blocks from errored block {1}.", _skip, i);
                    i       += _skip - blocksToRead;
                    newTrim =  true;
                }

                sectorSpeedStart += blocksToRead;

                _resume.NextBlock = i + blocksToRead;

                elapsed = (DateTime.UtcNow - timeSpeedStart).TotalSeconds;

                if(elapsed < 1)
                    continue;

                currentSpeed     = (sectorSpeedStart * blockSize) / (1048576 * elapsed);
                sectorSpeedStart = 0;
                timeSpeedStart   = DateTime.UtcNow;
            }

            EndProgress?.Invoke();

            if(!failedCrossingLeadOut)
                return;

            _dumpLog.WriteLine("Failed crossing into Lead-Out, dump may not be correct.");
            UpdateStatus?.Invoke("Failed crossing into Lead-Out, dump may not be correct.");
        }
    }
}