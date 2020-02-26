// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : CompactDisc.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Core algorithms.
//
// --[ Description ] ----------------------------------------------------------
//
//     Dumps CDs and DDCDs.
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
using DiscImageChef.CommonTypes.Enums;
using DiscImageChef.CommonTypes.Extents;
using DiscImageChef.Core.Logging;
using DiscImageChef.Devices;
using Schemas;

// ReSharper disable JoinDeclarationAndInitializer
// ReSharper disable InlineOutVariableDeclaration
// ReSharper disable TooWideLocalVariableScope

namespace DiscImageChef.Core.Devices.Dumping
{
    partial class Dump
    {
        /// <summary>Dumps inter-session lead-outs</summary>
        /// <param name="blocks">Total number of positive sectors</param>
        /// <param name="blockSize">Size of the read sector in bytes</param>
        /// <param name="currentSpeed">Current read speed</param>
        /// <param name="currentTry">Current dump hardware try</param>
        /// <param name="extents">Extents</param>
        /// <param name="ibgLog">IMGBurn log</param>
        /// <param name="imageWriteDuration">Duration of image write</param>
        /// <param name="leadOutExtents">Lead-out extents</param>
        /// <param name="maxSpeed">Maximum speed</param>
        /// <param name="mhddLog">MHDD log</param>
        /// <param name="minSpeed">Minimum speed</param>
        /// <param name="read6">Device supports READ(6)</param>
        /// <param name="read10">Device supports READ(10)</param>
        /// <param name="read12">Device supports READ(12)</param>
        /// <param name="read16">Device supports READ(16)</param>
        /// <param name="readcd">Device supports READ CD</param>
        /// <param name="supportedSubchannel">Drive's maximum supported subchannel</param>
        /// <param name="subSize">Subchannel size in bytes</param>
        /// <param name="totalDuration">Total commands duration</param>
        void DumpCdLeadOuts(ulong blocks, uint blockSize, ref double currentSpeed, DumpHardwareType currentTry,
                            ExtentsULong extents, IbgLog ibgLog, ref double imageWriteDuration,
                            ExtentsULong leadOutExtents, ref double maxSpeed, MhddLog mhddLog, ref double minSpeed,
                            bool read6, bool read10, bool read12, bool read16, bool readcd,
                            MmcSubchannel supportedSubchannel, uint subSize, ref double totalDuration)
        {
            byte[]     cmdBuf     = null; // Data buffer
            const uint sectorSize = 2352; // Full sector size
            bool       sense      = true; // Sense indicator

            UpdateStatus?.Invoke("Reading lead-outs");
            _dumpLog.WriteLine("Reading lead-outs");

            InitProgress?.Invoke();

            foreach((ulong item1, ulong item2) in leadOutExtents.ToArray())
                for(ulong i = item1; i <= item2; i++)
                {
                    if(_aborted)
                    {
                        currentTry.Extents = ExtentsConverter.ToMetadata(extents);
                        _dumpLog.WriteLine("Aborted!");

                        break;
                    }

                    double cmdDuration = 0;

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

                    PulseProgress?.Invoke($"Reading sector {i} at lead-out ({currentSpeed:F3} MiB/sec.)");

                    if(readcd)
                    {
                        sense = _dev.ReadCd(out cmdBuf, out _, (uint)i, blockSize, 1, MmcSectorTypes.AllTypes, false,
                                            false, true, MmcHeaderCodes.AllHeaders, true, true, MmcErrorField.None,
                                            supportedSubchannel, _dev.Timeout, out cmdDuration);

                        totalDuration += cmdDuration;
                    }
                    else if(read16)
                        sense = _dev.Read16(out cmdBuf, out _, 0, false, true, false, i, blockSize, 0, 1, false,
                                            _dev.Timeout, out cmdDuration);
                    else if(read12)
                        sense = _dev.Read12(out cmdBuf, out _, 0, false, true, false, false, (uint)i, blockSize, 0, 1,
                                            false, _dev.Timeout, out cmdDuration);
                    else if(read10)
                        sense = _dev.Read10(out cmdBuf, out _, 0, false, true, false, false, (uint)i, blockSize, 0, 1,
                                            _dev.Timeout, out cmdDuration);
                    else if(read6)
                        sense = _dev.Read6(out cmdBuf, out _, (uint)i, blockSize, 1, _dev.Timeout, out cmdDuration);

                    if(!sense &&
                       !_dev.Error)
                    {
                        mhddLog.Write(i, cmdDuration);
                        ibgLog.Write(i, currentSpeed * 1024);
                        extents.Add(i, _maximumReadable, true);
                        leadOutExtents.Remove(i);
                        DateTime writeStart = DateTime.Now;

                        if(supportedSubchannel != MmcSubchannel.None)
                        {
                            byte[] data = new byte[sectorSize * _maximumReadable];
                            byte[] sub  = new byte[subSize    * _maximumReadable];

                            for(int b = 0; b < _maximumReadable; b++)
                            {
                                Array.Copy(cmdBuf, (int)(0 + (b * blockSize)), data, sectorSize * b, sectorSize);

                                Array.Copy(cmdBuf, (int)(sectorSize + (b * blockSize)), sub, subSize * b, subSize);
                            }

                            _outputPlugin.WriteSectorsLong(data, i, _maximumReadable);
                            _outputPlugin.WriteSectorsTag(sub, i, _maximumReadable, SectorTagType.CdSectorSubchannel);
                        }
                        else
                            _outputPlugin.WriteSectors(cmdBuf, i, _maximumReadable);

                        imageWriteDuration += (DateTime.Now - writeStart).TotalSeconds;
                    }
                    else
                    {
                        // TODO: Reset device after X errors
                        if(_stopOnError)
                            return; // TODO: Return more cleanly

                        // Write empty data
                        DateTime writeStart = DateTime.Now;

                        if(supportedSubchannel != MmcSubchannel.None)
                        {
                            _outputPlugin.WriteSectorsLong(new byte[sectorSize * _skip], i, 1);

                            _outputPlugin.WriteSectorsTag(new byte[subSize * _skip], i, 1,
                                                          SectorTagType.CdSectorSubchannel);
                        }
                        else
                            _outputPlugin.WriteSectors(new byte[blockSize * _skip], i, 1);

                        imageWriteDuration += (DateTime.Now - writeStart).TotalSeconds;

                        mhddLog.Write(i, cmdDuration < 500 ? 65535 : cmdDuration);

                        ibgLog.Write(i, 0);
                    }

                    double newSpeed = ((double)blockSize * _maximumReadable) / 1048576 / (cmdDuration / 1000);

                    if(!double.IsInfinity(newSpeed))
                        currentSpeed = newSpeed;

                    _resume.NextBlock = i + 1;
                }

            EndProgress?.Invoke();
        }

        /// <summary>Retries inter-session lead-outs</summary>
        /// <param name="blocks">Total number of positive sectors</param>
        /// <param name="blockSize">Size of the read sector in bytes</param>
        /// <param name="currentSpeed">Current read speed</param>
        /// <param name="currentTry">Current dump hardware try</param>
        /// <param name="extents">Extents</param>
        /// <param name="ibgLog">IMGBurn log</param>
        /// <param name="imageWriteDuration">Duration of image write</param>
        /// <param name="leadOutExtents">Lead-out extents</param>
        /// <param name="maxSpeed">Maximum speed</param>
        /// <param name="mhddLog">MHDD log</param>
        /// <param name="minSpeed">Minimum speed</param>
        /// <param name="read6">Device supports READ(6)</param>
        /// <param name="read10">Device supports READ(10)</param>
        /// <param name="read12">Device supports READ(12)</param>
        /// <param name="read16">Device supports READ(16)</param>
        /// <param name="readcd">Device supports READ CD</param>
        /// <param name="supportedSubchannel">Drive's maximum supported subchannel</param>
        /// <param name="subSize">Subchannel size in bytes</param>
        /// <param name="totalDuration">Total commands duration</param>
        void RetryCdLeadOuts(ulong blocks, uint blockSize, ref double currentSpeed, DumpHardwareType currentTry,
                             ExtentsULong extents, IbgLog ibgLog, ref double imageWriteDuration,
                             ExtentsULong leadOutExtents, ref double maxSpeed, MhddLog mhddLog, ref double minSpeed,
                             bool read6, bool read10, bool read12, bool read16, bool readcd,
                             MmcSubchannel supportedSubchannel, uint subSize, ref double totalDuration)
        {
            byte[]     cmdBuf     = null; // Data buffer
            const uint sectorSize = 2352; // Full sector size
            bool       sense      = true; // Sense indicator

            _dumpLog.WriteLine("Retrying lead-outs");

            InitProgress?.Invoke();

            foreach((ulong item1, ulong item2) in leadOutExtents.ToArray())
                for(ulong i = item1; i <= item2; i++)
                {
                    if(_aborted)
                    {
                        currentTry.Extents = ExtentsConverter.ToMetadata(extents);
                        _dumpLog.WriteLine("Aborted!");

                        break;
                    }

                    double cmdDuration = 0;

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

                    PulseProgress?.Invoke(string.Format("Reading sector {0} at lead-out ({1:F3} MiB/sec.)", i, blocks,
                                                        currentSpeed));

                    if(readcd)
                    {
                        sense = _dev.ReadCd(out cmdBuf, out _, (uint)i, blockSize, 1, MmcSectorTypes.AllTypes, false,
                                            false, true, MmcHeaderCodes.AllHeaders, true, true, MmcErrorField.None,
                                            supportedSubchannel, _dev.Timeout, out cmdDuration);

                        totalDuration += cmdDuration;
                    }
                    else if(read16)
                        sense = _dev.Read16(out cmdBuf, out _, 0, false, true, false, i, blockSize, 0, 1, false,
                                            _dev.Timeout, out cmdDuration);
                    else if(read12)
                        sense = _dev.Read12(out cmdBuf, out _, 0, false, true, false, false, (uint)i, blockSize, 0, 1,
                                            false, _dev.Timeout, out cmdDuration);
                    else if(read10)
                        sense = _dev.Read10(out cmdBuf, out _, 0, false, true, false, false, (uint)i, blockSize, 0, 1,
                                            _dev.Timeout, out cmdDuration);
                    else if(read6)
                        sense = _dev.Read6(out cmdBuf, out _, (uint)i, blockSize, 1, _dev.Timeout, out cmdDuration);

                    if(!sense &&
                       !_dev.Error)
                    {
                        mhddLog.Write(i, cmdDuration);
                        ibgLog.Write(i, currentSpeed * 1024);
                        extents.Add(i, _maximumReadable, true);
                        leadOutExtents.Remove(i);
                        DateTime writeStart = DateTime.Now;

                        if(supportedSubchannel != MmcSubchannel.None)
                        {
                            byte[] data = new byte[sectorSize * _maximumReadable];
                            byte[] sub  = new byte[subSize    * _maximumReadable];

                            for(int b = 0; b < _maximumReadable; b++)
                            {
                                Array.Copy(cmdBuf, (int)(0 + (b * blockSize)), data, sectorSize * b,
                                           sectorSize);

                                Array.Copy(cmdBuf, (int)(sectorSize + (b * blockSize)), sub, subSize * b, subSize);
                            }

                            _outputPlugin.WriteSectorsLong(data, i, _maximumReadable);
                            _outputPlugin.WriteSectorsTag(sub, i, _maximumReadable, SectorTagType.CdSectorSubchannel);
                        }
                        else
                            _outputPlugin.WriteSectors(cmdBuf, i, _maximumReadable);

                        imageWriteDuration += (DateTime.Now - writeStart).TotalSeconds;
                    }
                    else
                    {
                        // TODO: Reset device after X errors
                        if(_stopOnError)
                            return; // TODO: Return more cleanly

                        // Write empty data
                        DateTime writeStart = DateTime.Now;

                        if(supportedSubchannel != MmcSubchannel.None)
                        {
                            _outputPlugin.WriteSectorsLong(new byte[sectorSize * _skip], i, 1);

                            _outputPlugin.WriteSectorsTag(new byte[subSize * _skip], i, 1,
                                                          SectorTagType.CdSectorSubchannel);
                        }
                        else
                            _outputPlugin.WriteSectors(new byte[blockSize * _skip], i, 1);

                        imageWriteDuration += (DateTime.Now - writeStart).TotalSeconds;

                        mhddLog.Write(i, cmdDuration < 500 ? 65535 : cmdDuration);

                        ibgLog.Write(i, 0);
                    }

                    double newSpeed = ((double)blockSize * _maximumReadable) / 1048576 / (cmdDuration / 1000);

                    if(!double.IsInfinity(newSpeed))
                        currentSpeed = newSpeed;
                }

            EndProgress?.Invoke();
        }
    }
}