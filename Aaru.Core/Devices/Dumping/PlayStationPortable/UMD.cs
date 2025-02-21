// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : UMD.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Dumping with a jail-broken PlayStation Portable thru USB.
//
// --[ Description ] ----------------------------------------------------------
//
//     Handles dumping UMD using a jail-broken PlayStation Portable thru USB.
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

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Aaru.CommonTypes;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Extents;
using Aaru.CommonTypes.Interfaces;
using Aaru.Console;
using Aaru.Core.Graphics;
using Aaru.Core.Logging;
using Aaru.Decoders.SCSI;
using Aaru.Devices;
using Humanizer;
using Humanizer.Bytes;
using Humanizer.Localisation;
using Track = Aaru.CommonTypes.Structs.Track;
using TrackType = Aaru.CommonTypes.Enums.TrackType;
using Version = Aaru.CommonTypes.Interop.Version;

namespace Aaru.Core.Devices.Dumping;

public partial class Dump
{
    [SuppressMessage("ReSharper", "JoinDeclarationAndInitializer")]
    void DumpUmd()
    {
        const uint      blockSize     = 2048;
        const MediaType dskType       = MediaType.UMD;
        uint            blocksToRead  = 16;
        double          totalDuration = 0;
        double          currentSpeed  = 0;
        double          maxSpeed      = double.MinValue;
        double          minSpeed      = double.MaxValue;
        byte[]          senseBuf;

        if(_outputPlugin is not IWritableOpticalImage outputOptical)
        {
            StoppingErrorMessage?.Invoke(Localization.Core.Image_is_not_writable_aborting);

            return;
        }

        bool sense = _dev.Read12(out byte[] readBuffer,
                                 out _,
                                 0,
                                 false,
                                 true,
                                 false,
                                 false,
                                 0,
                                 512,
                                 0,
                                 1,
                                 false,
                                 _dev.Timeout,
                                 out _);

        if(sense)
        {
            _dumpLog.WriteLine(Localization.Core.Could_not_read);
            StoppingErrorMessage?.Invoke(Localization.Core.Could_not_read);

            return;
        }

        var fatStart      = (ushort)((readBuffer[0x0F] << 8)                          + readBuffer[0x0E]);
        var sectorsPerFat = (ushort)((readBuffer[0x17] << 8)                          + readBuffer[0x16]);
        var rootStart     = (ushort)(sectorsPerFat                                * 2 + fatStart);
        var rootSize      = (ushort)(((readBuffer[0x12] << 8) + readBuffer[0x11]) * 32 / 512);
        var umdStart      = (ushort)(rootStart + rootSize);

        UpdateStatus?.Invoke(string.Format(Localization.Core.Reading_root_directory_in_sector_0, rootStart));
        _dumpLog.WriteLine(Localization.Core.Reading_root_directory_in_sector_0, rootStart);

        sense = _dev.Read12(out readBuffer,
                            out _,
                            0,
                            false,
                            true,
                            false,
                            false,
                            rootStart,
                            512,
                            0,
                            1,
                            false,
                            _dev.Timeout,
                            out _);

        if(sense)
        {
            _dumpLog.WriteLine(Localization.Core.Could_not_read);
            StoppingErrorMessage?.Invoke(Localization.Core.Could_not_read);

            return;
        }

        var    umdSizeInBytes  = BitConverter.ToUInt32(readBuffer, 0x3C);
        ulong  blocks          = umdSizeInBytes / blockSize;
        string mediaPartNumber = Encoding.ASCII.GetString(readBuffer, 0, 11).Trim();

        UpdateStatus?.Invoke(string.Format(Localization.Core.Media_has_0_blocks_of_1_bytes_each_for_a_total_of_2,
                                           blocks,
                                           blockSize,
                                           ByteSize.FromBytes(blocks * blockSize).ToString("0.000")));

        UpdateStatus?.Invoke(string.Format(Localization.Core.Device_reports_0_blocks_1_bytes,
                                           blocks,
                                           blocks * blockSize));

        UpdateStatus?.Invoke(string.Format(Localization.Core.Device_can_read_0_blocks_at_a_time, blocksToRead));
        UpdateStatus?.Invoke(string.Format(Localization.Core.Device_reports_0_bytes_per_logical_block, blockSize));
        UpdateStatus?.Invoke(string.Format(Localization.Core.Device_reports_0_bytes_per_physical_block, blockSize));
        UpdateStatus?.Invoke(string.Format(Localization.Core.SCSI_device_type_0, _dev.ScsiType));
        UpdateStatus?.Invoke(string.Format(Localization.Core.Media_identified_as_0, dskType));
        UpdateStatus?.Invoke(string.Format(Localization.Core.Media_part_number_is_0, mediaPartNumber));
        _dumpLog.WriteLine(Localization.Core.Device_reports_0_blocks_1_bytes,           blocks, blocks * blockSize);
        _dumpLog.WriteLine(Localization.Core.Device_can_read_0_blocks_at_a_time,        blocksToRead);
        _dumpLog.WriteLine(Localization.Core.Device_reports_0_bytes_per_logical_block,  blockSize);
        _dumpLog.WriteLine(Localization.Core.Device_reports_0_bytes_per_physical_block, blockSize);
        _dumpLog.WriteLine(Localization.Core.SCSI_device_type_0,                        _dev.ScsiType);
        _dumpLog.WriteLine(Localization.Core.Media_identified_as_0,                     dskType);
        _dumpLog.WriteLine(Localization.Core.Media_part_number_is_0,                    mediaPartNumber);

        bool ret;

        var mhddLog = new MhddLog(_outputPrefix + ".mhddlog.bin",
                                  _dev,
                                  blocks,
                                  blockSize,
                                  blocksToRead,
                                  _private,
                                  _dimensions);

        var ibgLog = new IbgLog(_outputPrefix + ".ibg", 0x0010);
        ret = outputOptical.Create(_outputPath, dskType, _formatOptions, blocks, blockSize);

        // Cannot create image
        if(!ret)
        {
            _dumpLog.WriteLine(Localization.Core.Error_creating_output_image_not_continuing);
            _dumpLog.WriteLine(outputOptical.ErrorMessage);

            StoppingErrorMessage?.Invoke(Localization.Core.Error_creating_output_image_not_continuing +
                                         Environment.NewLine                                          +
                                         outputOptical.ErrorMessage);

            return;
        }

        _dumpStopwatch.Restart();
        double imageWriteDuration = 0;

        outputOptical.SetTracks([
                                    new Track
                                    {
                                        BytesPerSector    = (int)blockSize,
                                        EndSector         = blocks - 1,
                                        Sequence          = 1,
                                        RawBytesPerSector = (int)blockSize,
                                        SubchannelType    = TrackSubchannelType.None,
                                        Session           = 1,
                                        Type              = TrackType.Data
                                    }
                                ]);

        DumpHardware currentTry = null;
        ExtentsULong extents    = null;

        ResumeSupport.Process(true,
                              _dev.IsRemovable,
                              blocks,
                              _dev.Manufacturer,
                              _dev.Model,
                              _dev.Serial,
                              _dev.PlatformId,
                              ref _resume,
                              ref currentTry,
                              ref extents,
                              _dev.FirmwareRevision,
                              _private,
                              _force);

        if(currentTry == null || extents == null)
        {
            StoppingErrorMessage?.Invoke(Localization.Core.Could_not_process_resume_file_not_continuing);

            return;
        }

        if(_resume.NextBlock > 0) _dumpLog.WriteLine(Localization.Core.Resuming_from_block_0, _resume.NextBlock);

        if(_createGraph)
        {
            Spiral.DiscParameters discSpiralParameters = Spiral.DiscParametersFromMediaType(dskType);

            if(discSpiralParameters is not null)
                _mediaGraph = new Spiral((int)_dimensions, (int)_dimensions, discSpiralParameters, blocks);
            else
                _mediaGraph = new BlockMap((int)_dimensions, (int)_dimensions, blocks);

            if(_mediaGraph is not null)
            {
                foreach(Tuple<ulong, ulong> e in extents.ToArray())
                    _mediaGraph?.PaintSectorsGood(e.Item1, (uint)(e.Item2 - e.Item1 + 2));
            }

            _mediaGraph?.PaintSectorsBad(_resume.BadBlocks);
        }

        var newTrim = false;

        _speedStopwatch.Reset();
        ulong sectorSpeedStart = 0;
        InitProgress?.Invoke();

        for(ulong i = _resume.NextBlock; i < blocks; i += blocksToRead)
        {
            if(_aborted)
            {
                currentTry.Extents = ExtentsConverter.ToMetadata(extents);
                UpdateStatus?.Invoke(Localization.Core.Aborted);
                _dumpLog.WriteLine(Localization.Core.Aborted);

                break;
            }

            if(blocks - i < blocksToRead) blocksToRead = (uint)(blocks - i);

            if(currentSpeed > maxSpeed && currentSpeed > 0) maxSpeed = currentSpeed;

            if(currentSpeed < minSpeed && currentSpeed > 0) minSpeed = currentSpeed;

            UpdateProgress?.Invoke(string.Format(Localization.Core.Reading_sector_0_of_1_2,
                                                 i,
                                                 blocks,
                                                 ByteSize.FromMegabytes(currentSpeed).Per(_oneSecond).Humanize()),
                                   (long)i,
                                   (long)blocks);

            _speedStopwatch.Start();

            sense = _dev.Read12(out readBuffer,
                                out senseBuf,
                                0,
                                false,
                                true,
                                false,
                                false,
                                (uint)(umdStart + i * 4),
                                512,
                                0,
                                blocksToRead * 4,
                                false,
                                _dev.Timeout,
                                out double cmdDuration);

            _speedStopwatch.Stop();

            totalDuration += cmdDuration;

            _writeStopwatch.Restart();

            if(!sense && !_dev.Error)
            {
                mhddLog.Write(i, cmdDuration, blocksToRead);
                ibgLog.Write(i, currentSpeed * 1024);
                outputOptical.WriteSectors(readBuffer, i, blocksToRead);
                imageWriteDuration += _writeStopwatch.Elapsed.TotalSeconds;
                extents.Add(i, blocksToRead, true);
                _mediaGraph?.PaintSectorsGood(i, blocksToRead);
            }
            else
            {
                _errorLog?.WriteLine(i, _dev.Error, _dev.LastError, senseBuf);

                // TODO: Reset device after X errors
                if(_stopOnError) return; // TODO: Return more cleanly

                if(i + _skip > blocks) _skip = (uint)(blocks - i);

                // Write empty data
                outputOptical.WriteSectors(new byte[blockSize * _skip], i, _skip);
                imageWriteDuration += _writeStopwatch.Elapsed.TotalSeconds;

                for(ulong b = i; b < i + _skip; b++) _resume.BadBlocks.Add(b);

                mhddLog.Write(i, cmdDuration < 500 ? 65535 : cmdDuration, _skip);

                ibgLog.Write(i, 0);
                _dumpLog.WriteLine(Localization.Core.Skipping_0_blocks_from_errored_block_1, _skip, i);
                i       += _skip - blocksToRead;
                newTrim =  true;
            }

            _writeStopwatch.Stop();
            sectorSpeedStart  += blocksToRead;
            _resume.NextBlock =  i + blocksToRead;

            double elapsed = _speedStopwatch.Elapsed.TotalSeconds;

            if(elapsed <= 0 || sectorSpeedStart * blockSize < 524288) continue;

            currentSpeed     = sectorSpeedStart * blockSize / (1048576 * elapsed);
            sectorSpeedStart = 0;
            _speedStopwatch.Reset();
        }

        _resume.BadBlocks = _resume.BadBlocks.Distinct().ToList();

        _dumpStopwatch.Stop();
        EndProgress?.Invoke();
        mhddLog.Close();

        ibgLog.Close(_dev,
                     blocks,
                     blockSize,
                     _dumpStopwatch.Elapsed.TotalSeconds,
                     currentSpeed                     * 1024,
                     blockSize * (double)(blocks + 1) / 1024 / (totalDuration / 1000),
                     _devicePath);

        UpdateStatus?.Invoke(string.Format(Localization.Core.Dump_finished_in_0,
                                           _dumpStopwatch.Elapsed.Humanize(minUnit: TimeUnit.Second)));

        UpdateStatus?.Invoke(string.Format(Localization.Core.Average_dump_speed_0,
                                           ByteSize.FromBytes(blockSize * (blocks + 1))
                                                   .Per(totalDuration.Milliseconds())
                                                   .Humanize()));

        UpdateStatus?.Invoke(string.Format(Localization.Core.Average_write_speed_0,
                                           ByteSize.FromBytes(blockSize * (blocks + 1))
                                                   .Per(imageWriteDuration.Seconds())
                                                   .Humanize()));

        _dumpLog.WriteLine(string.Format(Localization.Core.Dump_finished_in_0,
                                         _dumpStopwatch.Elapsed.Humanize(minUnit: TimeUnit.Second)));

        _dumpLog.WriteLine(string.Format(Localization.Core.Average_dump_speed_0,
                                         ByteSize.FromBytes(blockSize * (blocks + 1))
                                                 .Per(totalDuration.Milliseconds())
                                                 .Humanize()));

        _dumpLog.WriteLine(string.Format(Localization.Core.Average_write_speed_0,
                                         ByteSize.FromBytes(blockSize * (blocks + 1))
                                                 .Per(imageWriteDuration.Seconds())
                                                 .Humanize()));

#region Trimming

        if(_resume.BadBlocks.Count > 0 && !_aborted && _trim && newTrim)
        {
            _trimStopwatch.Restart();
            _dumpLog.WriteLine(Localization.Core.Trimming_skipped_sectors);

            ulong[] tmpArray = _resume.BadBlocks.ToArray();
            InitProgress?.Invoke();

            foreach(ulong badSector in tmpArray)
            {
                if(_aborted)
                {
                    currentTry.Extents = ExtentsConverter.ToMetadata(extents);
                    _dumpLog.WriteLine(Localization.Core.Aborted);

                    break;
                }

                PulseProgress?.Invoke(string.Format(Localization.Core.Trimming_sector_0, badSector));

                sense = _dev.Read12(out readBuffer,
                                    out senseBuf,
                                    0,
                                    false,
                                    true,
                                    false,
                                    false,
                                    (uint)(umdStart + badSector * 4),
                                    512,
                                    0,
                                    4,
                                    false,
                                    _dev.Timeout,
                                    out double _);

                if(sense || _dev.Error)
                {
                    _errorLog?.WriteLine(badSector, _dev.Error, _dev.LastError, senseBuf);

                    continue;
                }

                _resume.BadBlocks.Remove(badSector);
                extents.Add(badSector);
                outputOptical.WriteSector(readBuffer, badSector);
                _mediaGraph?.PaintSectorGood(badSector);
            }

            EndProgress?.Invoke();
            _trimStopwatch.Stop();

            _dumpLog.WriteLine(string.Format(Localization.Core.Trimming_finished_in_0,
                                             _trimStopwatch.Elapsed.Humanize(minUnit: TimeUnit.Second)));
        }

#endregion Trimming

#region Error handling

        if(_resume.BadBlocks.Count > 0 && !_aborted && _retryPasses > 0)
        {
            var pass              = 1;
            var forward           = true;
            var runningPersistent = false;

            Modes.ModePage? currentModePage = null;
            byte[]          md6;

            if(_persistent)
            {
                Modes.ModePage_01 pg;

                sense = _dev.ModeSense6(out readBuffer,
                                        out _,
                                        false,
                                        ScsiModeSensePageControl.Current,
                                        0x01,
                                        _dev.Timeout,
                                        out _);

                if(!sense)
                {
                    Modes.DecodedMode? dcMode6 = Modes.DecodeMode6(readBuffer, _dev.ScsiType);

                    if(dcMode6.HasValue)
                    {
                        foreach(Modes.ModePage modePage in dcMode6.Value.Pages.Where(modePage =>
                                    modePage is { Page: 0x01, Subpage: 0x00 }))
                            currentModePage = modePage;
                    }
                }

                if(currentModePage == null)
                {
                    pg = new Modes.ModePage_01
                    {
                        PS             = false,
                        AWRE           = true,
                        ARRE           = true,
                        TB             = false,
                        RC             = false,
                        EER            = true,
                        PER            = false,
                        DTE            = true,
                        DCR            = false,
                        ReadRetryCount = 32
                    };

                    currentModePage = new Modes.ModePage
                    {
                        Page         = 0x01,
                        Subpage      = 0x00,
                        PageResponse = Modes.EncodeModePage_01(pg)
                    };
                }

                pg = new Modes.ModePage_01
                {
                    PS             = false,
                    AWRE           = false,
                    ARRE           = false,
                    TB             = true,
                    RC             = false,
                    EER            = true,
                    PER            = false,
                    DTE            = false,
                    DCR            = false,
                    ReadRetryCount = 255
                };

                var md = new Modes.DecodedMode
                {
                    Header = new Modes.ModeHeader(),
                    Pages =
                    [
                        new Modes.ModePage
                        {
                            Page         = 0x01,
                            Subpage      = 0x00,
                            PageResponse = Modes.EncodeModePage_01(pg)
                        }
                    ]
                };

                md6 = Modes.EncodeMode6(md, _dev.ScsiType);

                _dumpLog.WriteLine(Localization.Core.Sending_MODE_SELECT_to_drive_return_damaged_blocks);
                sense = _dev.ModeSelect(md6, out senseBuf, true, false, _dev.Timeout, out _);

                if(sense)
                {
                    UpdateStatus?.Invoke(Localization.Core
                                                     .Drive_did_not_accept_MODE_SELECT_command_for_persistent_error_reading);

                    AaruConsole.DebugWriteLine(Localization.Core.Error_0, Sense.PrettifySense(senseBuf));

                    _dumpLog.WriteLine(Localization.Core
                                                   .Drive_did_not_accept_MODE_SELECT_command_for_persistent_error_reading);
                }
                else
                    runningPersistent = true;
            }

            InitProgress?.Invoke();
        repeatRetry:
            ulong[] tmpArray = _resume.BadBlocks.ToArray();

            foreach(ulong badSector in tmpArray)
            {
                if(_aborted)
                {
                    currentTry.Extents = ExtentsConverter.ToMetadata(extents);
                    _dumpLog.WriteLine(Localization.Core.Aborted);

                    break;
                }

                if(forward)
                {
                    PulseProgress?.Invoke(runningPersistent
                                              ? string.Format(Localization.Core
                                                                          .Retrying_sector_0_pass_1_recovering_partial_data_forward,
                                                              badSector,
                                                              pass)
                                              : string.Format(Localization.Core.Retrying_sector_0_pass_1_forward,
                                                              badSector,
                                                              pass));
                }
                else
                {
                    PulseProgress?.Invoke(runningPersistent
                                              ? string.Format(Localization.Core
                                                                          .Retrying_sector_0_pass_1_recovering_partial_data_reverse,
                                                              badSector,
                                                              pass)
                                              : string.Format(Localization.Core.Retrying_sector_0_pass_1_reverse,
                                                              badSector,
                                                              pass));
                }

                sense = _dev.Read12(out readBuffer,
                                    out senseBuf,
                                    0,
                                    false,
                                    true,
                                    false,
                                    false,
                                    (uint)(umdStart + badSector * 4),
                                    512,
                                    0,
                                    4,
                                    false,
                                    _dev.Timeout,
                                    out double cmdDuration);

                totalDuration += cmdDuration;

                if(sense || _dev.Error) _errorLog?.WriteLine(badSector, _dev.Error, _dev.LastError, senseBuf);

                if(!sense && !_dev.Error)
                {
                    _resume.BadBlocks.Remove(badSector);
                    extents.Add(badSector);
                    outputOptical.WriteSector(readBuffer, badSector);
                    _mediaGraph?.PaintSectorGood(badSector);

                    UpdateStatus?.Invoke(string.Format(Localization.Core.Correctly_retried_block_0_in_pass_1,
                                                       badSector,
                                                       pass));

                    _dumpLog.WriteLine(Localization.Core.Correctly_retried_block_0_in_pass_1, badSector, pass);
                }
                else if(runningPersistent) outputOptical.WriteSector(readBuffer, badSector);
            }

            if(pass < _retryPasses && !_aborted && _resume.BadBlocks.Count > 0)
            {
                pass++;
                forward = !forward;
                _resume.BadBlocks.Sort();

                if(!forward) _resume.BadBlocks.Reverse();

                goto repeatRetry;
            }

            if(runningPersistent && currentModePage.HasValue)
            {
                var md = new Modes.DecodedMode
                {
                    Header = new Modes.ModeHeader(),
                    Pages  = [currentModePage.Value]
                };

                md6 = Modes.EncodeMode6(md, _dev.ScsiType);

                _dumpLog.WriteLine(Localization.Core.Sending_MODE_SELECT_to_drive_return_device_to_previous_status);
                _dev.ModeSelect(md6, out _, true, false, _dev.Timeout, out _);
            }

            EndProgress?.Invoke();
            AaruConsole.WriteLine();
        }

#endregion Error handling

        _resume.BadBlocks.Sort();

        foreach(ulong bad in _resume.BadBlocks) _dumpLog.WriteLine(Localization.Core.Sector_0_could_not_be_read, bad);

        currentTry.Extents = ExtentsConverter.ToMetadata(extents);

        var metadata = new CommonTypes.Structs.ImageInfo
        {
            Application        = "Aaru",
            ApplicationVersion = Version.GetVersion(),
            MediaPartNumber    = mediaPartNumber
        };

        if(!outputOptical.SetImageInfo(metadata))
        {
            ErrorMessage?.Invoke(Localization.Core.Error_0_setting_metadata +
                                 Environment.NewLine                        +
                                 outputOptical.ErrorMessage);
        }

        outputOptical.SetDumpHardware(_resume.Tries);

        if(_preSidecar != null) outputOptical.SetMetadata(_preSidecar);

        _dumpLog.WriteLine(Localization.Core.Closing_output_file);
        UpdateStatus?.Invoke(Localization.Core.Closing_output_file);
        _imageCloseStopwatch.Restart();
        outputOptical.Close();
        _imageCloseStopwatch.Stop();

        _dumpLog.WriteLine(Localization.Core.Closed_in_0,
                           _imageCloseStopwatch.Elapsed.Humanize(minUnit: TimeUnit.Second));

        if(_aborted)
        {
            UpdateStatus?.Invoke(Localization.Core.Aborted);
            _dumpLog.WriteLine(Localization.Core.Aborted);

            return;
        }

        double totalChkDuration = 0;

        if(_metadata) WriteOpticalSidecar(blockSize, blocks, dskType, null, null, 1, out totalChkDuration, null);

        UpdateStatus?.Invoke("");

        UpdateStatus?.Invoke(string.Format(Localization.Core
                                                       .Took_a_total_of_0_1_processing_commands_2_checksumming_3_writing_4_closing,
                                           _dumpStopwatch.Elapsed.Humanize(minUnit: TimeUnit.Second),
                                           totalDuration.Milliseconds().Humanize(minUnit: TimeUnit.Second),
                                           totalChkDuration.Milliseconds().Humanize(minUnit: TimeUnit.Second),
                                           imageWriteDuration.Seconds().Humanize(minUnit: TimeUnit.Second),
                                           _imageCloseStopwatch.Elapsed.Humanize(minUnit: TimeUnit.Second)));

        UpdateStatus?.Invoke(string.Format(Localization.Core.Average_speed_0,
                                           ByteSize.FromBytes(blockSize * (blocks + 1))
                                                   .Per(totalDuration.Milliseconds())
                                                   .Humanize()));

        if(maxSpeed > 0)
        {
            UpdateStatus?.Invoke(string.Format(Localization.Core.Fastest_speed_burst_0,
                                               ByteSize.FromMegabytes(maxSpeed).Per(_oneSecond).Humanize()));
        }

        if(minSpeed is > 0 and < double.MaxValue)
        {
            UpdateStatus?.Invoke(string.Format(Localization.Core.Slowest_speed_burst_0,
                                               ByteSize.FromMegabytes(minSpeed).Per(_oneSecond).Humanize()));
        }

        UpdateStatus?.Invoke(string.Format(Localization.Core._0_sectors_could_not_be_read, _resume.BadBlocks.Count));
        UpdateStatus?.Invoke("");

        Statistics.AddMedia(dskType, true);
    }
}