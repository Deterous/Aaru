﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : SSC.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Core algorithms.
//
// --[ Description ] ----------------------------------------------------------
//
//     Creates reports from SCSI Streaming devices.
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

using System.Linq;
using DiscImageChef.CommonTypes.Metadata;
using DiscImageChef.Console;
using DiscImageChef.Decoders.SCSI;
using DiscImageChef.Decoders.SCSI.SSC;
using DiscImageChef.Devices;

namespace DiscImageChef.Core.Devices.Report
{
    public partial class DeviceReport
    {
        public Ssc ReportScsiSsc()
        {
            var report = new Ssc();
            DicConsole.WriteLine("Querying SCSI READ BLOCK LIMITS...");
            bool sense = _dev.ReadBlockLimits(out byte[] buffer, out byte[] _, _dev.Timeout, out _);

            if(!sense)
            {
                BlockLimits.BlockLimitsData? decBl = BlockLimits.Decode(buffer);

                if(decBl.HasValue)
                {
                    if(decBl.Value.granularity > 0)
                        report.BlockSizeGranularity = decBl.Value.granularity;

                    if(decBl.Value.maxBlockLen > 0)
                        report.MaxBlockLength = decBl.Value.maxBlockLen;

                    if(decBl.Value.minBlockLen > 0)
                        report.MinBlockLength = decBl.Value.minBlockLen;
                }
            }

            DicConsole.WriteLine("Querying SCSI REPORT DENSITY SUPPORT...");
            sense = _dev.ReportDensitySupport(out buffer, out byte[] _, false, false, _dev.Timeout, out _);

            if(!sense)
            {
                DensitySupport.DensitySupportHeader? dsh = DensitySupport.DecodeDensity(buffer);

                if(dsh.HasValue)
                {
                    SupportedDensity[] array = new SupportedDensity[dsh.Value.descriptors.Length];

                    for(int i = 0; i < dsh.Value.descriptors.Length; i++)
                        array[i] = new SupportedDensity
                        {
                            BitsPerMm      = dsh.Value.descriptors[i].bpmm,
                            Capacity       = dsh.Value.descriptors[i].capacity,
                            DefaultDensity = dsh.Value.descriptors[i].defaultDensity,
                            Description    = dsh.Value.descriptors[i].description,
                            Duplicate      = dsh.Value.descriptors[i].duplicate, Name = dsh.Value.descriptors[i].name,
                            Organization   = dsh.Value.descriptors[i].organization,
                            PrimaryCode    = dsh.Value.descriptors[i].primaryCode,
                            SecondaryCode  = dsh.Value.descriptors[i].secondaryCode,
                            Tracks         = dsh.Value.descriptors[i].tracks, Width = dsh.Value.descriptors[i].width,
                            Writable       = dsh.Value.descriptors[i].writable
                        };

                    report.SupportedDensities = array.ToList();
                }
            }

            DicConsole.WriteLine("Querying SCSI REPORT DENSITY SUPPORT for medium types...");
            sense = _dev.ReportDensitySupport(out buffer, out byte[] _, true, false, _dev.Timeout, out _);

            if(sense)
                return report;

            DensitySupport.MediaTypeSupportHeader? mtsh = DensitySupport.DecodeMediumType(buffer);

            if(!mtsh.HasValue)
                return report;

            SscSupportedMedia[] array2 = new SscSupportedMedia[mtsh.Value.descriptors.Length];

            for(int i = 0; i < mtsh.Value.descriptors.Length; i++)
            {
                array2[i] = new SscSupportedMedia
                {
                    Description  = mtsh.Value.descriptors[i].description, Length = mtsh.Value.descriptors[i].length,
                    MediumType   = mtsh.Value.descriptors[i].mediumType, Name    = mtsh.Value.descriptors[i].name,
                    Organization = mtsh.Value.descriptors[i].organization, Width = mtsh.Value.descriptors[i].width
                };

                if(mtsh.Value.descriptors[i].densityCodes == null)
                    continue;

                DensityCode[] array3 = new DensityCode[mtsh.Value.descriptors[i].densityCodes.Length];

                for(int j = 0; j < mtsh.Value.descriptors[i].densityCodes.Length; j++)
                    array3[j] = new DensityCode
                    {
                        Code = mtsh.Value.descriptors[i].densityCodes[j]
                    };

                array2[i].DensityCodes = array3.Distinct().ToList();
            }

            report.SupportedMediaTypes = array2.ToList();

            return report;
        }

        public TestedSequentialMedia ReportSscMedia()
        {
            var seqTest = new TestedSequentialMedia();

            Modes.DecodedMode? decMode = null;

            DicConsole.WriteLine("Querying SCSI MODE SENSE (10)...");

            bool sense = _dev.ModeSense10(out byte[] buffer, out byte[] _, false, true,
                                          ScsiModeSensePageControl.Current, 0x3F, 0x00, _dev.Timeout, out _);

            if(!sense &&
               !_dev.Error)
            {
                decMode                 = Modes.DecodeMode10(buffer, _dev.ScsiType);
                seqTest.ModeSense10Data = buffer;
            }

            DicConsole.WriteLine("Querying SCSI MODE SENSE...");
            sense = _dev.ModeSense(out buffer, out byte[] _, _dev.Timeout, out _);

            if(!sense &&
               !_dev.Error)
            {
                if(!decMode.HasValue)
                    decMode = Modes.DecodeMode6(buffer, _dev.ScsiType);

                seqTest.ModeSense6Data = buffer;
            }

            if(decMode.HasValue)
            {
                seqTest.MediumType = (byte)decMode.Value.Header.MediumType;

                if(decMode.Value.Header.BlockDescriptors        != null &&
                   decMode.Value.Header.BlockDescriptors.Length > 0)
                    seqTest.Density = (byte)decMode.Value.Header.BlockDescriptors[0].Density;
            }

            DicConsole.WriteLine("Querying SCSI REPORT DENSITY SUPPORT for current media...");
            sense = _dev.ReportDensitySupport(out buffer, out byte[] _, false, true, _dev.Timeout, out _);

            if(!sense)
            {
                DensitySupport.DensitySupportHeader? dsh = DensitySupport.DecodeDensity(buffer);

                if(dsh.HasValue)
                {
                    SupportedDensity[] array = new SupportedDensity[dsh.Value.descriptors.Length];

                    for(int i = 0; i < dsh.Value.descriptors.Length; i++)
                        array[i] = new SupportedDensity
                        {
                            BitsPerMm      = dsh.Value.descriptors[i].bpmm,
                            Capacity       = dsh.Value.descriptors[i].capacity,
                            DefaultDensity = dsh.Value.descriptors[i].defaultDensity,
                            Description    = dsh.Value.descriptors[i].description,
                            Duplicate      = dsh.Value.descriptors[i].duplicate, Name = dsh.Value.descriptors[i].name,
                            Organization   = dsh.Value.descriptors[i].organization,
                            PrimaryCode    = dsh.Value.descriptors[i].primaryCode,
                            SecondaryCode  = dsh.Value.descriptors[i].secondaryCode,
                            Tracks         = dsh.Value.descriptors[i].tracks, Width = dsh.Value.descriptors[i].width,
                            Writable       = dsh.Value.descriptors[i].writable
                        };

                    seqTest.SupportedDensities = array.ToList();
                }
            }

            DicConsole.WriteLine("Querying SCSI REPORT DENSITY SUPPORT for medium types for current media...");
            sense = _dev.ReportDensitySupport(out buffer, out byte[] _, true, true, _dev.Timeout, out _);

            if(!sense)
            {
                DensitySupport.MediaTypeSupportHeader? mtsh = DensitySupport.DecodeMediumType(buffer);

                if(mtsh.HasValue)
                {
                    SscSupportedMedia[] array = new SscSupportedMedia[mtsh.Value.descriptors.Length];

                    for(int i = 0; i < mtsh.Value.descriptors.Length; i++)
                    {
                        array[i] = new SscSupportedMedia
                        {
                            Description  = mtsh.Value.descriptors[i].description,
                            Length       = mtsh.Value.descriptors[i].length,
                            MediumType   = mtsh.Value.descriptors[i].mediumType, Name = mtsh.Value.descriptors[i].name,
                            Organization = mtsh.Value.descriptors[i].organization,
                            Width        = mtsh.Value.descriptors[i].width
                        };

                        if(mtsh.Value.descriptors[i].densityCodes == null)
                            continue;

                        DensityCode[] array2 = new DensityCode[mtsh.Value.descriptors[i].densityCodes.Length];

                        for(int j = 0; j < mtsh.Value.descriptors[i].densityCodes.Length; j++)
                            array2[j] = new DensityCode
                            {
                                Code = mtsh.Value.descriptors[i].densityCodes[j]
                            };

                        array[i].DensityCodes = array2.Distinct().ToList();
                    }

                    seqTest.SupportedMediaTypes = array.ToList();
                }
            }

            DicConsole.WriteLine("Trying SCSI READ MEDIA SERIAL NUMBER...");
            seqTest.CanReadMediaSerial = !_dev.ReadMediaSerialNumber(out buffer, out byte[] _, _dev.Timeout, out _);

            return seqTest;
        }
    }
}