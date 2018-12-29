﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : 2A.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Device structures decoders.
//
// --[ Description ] ----------------------------------------------------------
//
//     Decodes SCSI MODE PAGE 2Ah: CD-ROM capabilities page.
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
// Copyright © 2011-2019 Natalia Portillo
// ****************************************************************************/

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace DiscImageChef.Decoders.SCSI
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "MemberCanBeInternal")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "NotAccessedField.Global")]
    public static partial class Modes
    {
        #region Mode Page 0x2A: CD-ROM capabilities page
        /// <summary>
        ///     CD-ROM capabilities page
        ///     Page code 0x2A
        ///     16 bytes in OB-U0077C
        ///     20 bytes in SFF-8020i
        ///     22 bytes in MMC-1
        ///     26 bytes in MMC-2
        ///     Variable bytes in MMC-3
        /// </summary>
        public class ModePage_2A
        {
            public ModePage_2A_WriteDescriptor[] WriteSpeedPerformanceDescriptors;
            /// <summary>
            ///     Parameters can be saved
            /// </summary>
            public bool PS { get; set; }
            /// <summary>
            ///     Drive supports multi-session and/or Photo-CD
            /// </summary>
            public bool MultiSession { get; set; }
            /// <summary>
            ///     Drive is capable of reading sectors in Mode 2 Form 2 format
            /// </summary>
            public bool Mode2Form2 { get; set; }
            /// <summary>
            ///     Drive is capable of reading sectors in Mode 2 Form 1 format
            /// </summary>
            public bool Mode2Form1 { get; set; }
            /// <summary>
            ///     Drive is capable of playing audio
            /// </summary>
            public bool AudioPlay { get; set; }
            /// <summary>
            ///     Drive can return the ISRC
            /// </summary>
            public bool ISRC { get; set; }
            /// <summary>
            ///     Drive can return the media catalogue number
            /// </summary>
            public bool UPC { get; set; }
            /// <summary>
            ///     Drive can return C2 pointers
            /// </summary>
            public bool C2Pointer { get; set; }
            /// <summary>
            ///     Drive can read, deinterlave and correct R-W subchannels
            /// </summary>
            public bool DeinterlaveSubchannel { get; set; }
            /// <summary>
            ///     Drive can read interleaved and uncorrected R-W subchannels
            /// </summary>
            public bool Subchannel { get; set; }
            /// <summary>
            ///     Drive can continue from a loss of streaming on audio reading
            /// </summary>
            public bool AccurateCDDA { get; set; }
            /// <summary>
            ///     Audio can be read as digital data
            /// </summary>
            public bool CDDACommand { get; set; }
            /// <summary>
            ///     Loading Mechanism Type
            /// </summary>
            public byte LoadingMechanism { get; set; }
            /// <summary>
            ///     Drive can eject discs
            /// </summary>
            public bool Eject { get; set; }
            /// <summary>
            ///     Drive's optional prevent jumper status
            /// </summary>
            public bool PreventJumper { get; set; }
            /// <summary>
            ///     Current lock status
            /// </summary>
            public bool LockState { get; set; }
            /// <summary>
            ///     Drive can lock media
            /// </summary>
            public bool Lock { get; set; }
            /// <summary>
            ///     Each channel can be muted independently
            /// </summary>
            public bool SeparateChannelMute { get; set; }
            /// <summary>
            ///     Each channel's volume can be controlled independently
            /// </summary>
            public bool SeparateChannelVolume { get; set; }
            /// <summary>
            ///     Maximum drive speed in Kbytes/second
            /// </summary>
            public ushort MaximumSpeed { get; set; }
            /// <summary>
            ///     Supported volume levels
            /// </summary>
            public ushort SupportedVolumeLevels { get; set; }
            /// <summary>
            ///     Buffer size in Kbytes
            /// </summary>
            public ushort BufferSize { get; set; }
            /// <summary>
            ///     Current drive speed in Kbytes/second
            /// </summary>
            public ushort CurrentSpeed { get; set; }

            public bool Method2      { get; set; }
            public bool ReadCDRW     { get; set; }
            public bool ReadCDR      { get; set; }
            public bool WriteCDRW    { get; set; }
            public bool WriteCDR     { get; set; }
            public bool DigitalPort2 { get; set; }
            public bool DigitalPort1 { get; set; }
            public bool Composite    { get; set; }
            public bool SSS          { get; set; }
            public bool SDP          { get; set; }
            public byte Length       { get; set; }
            public bool LSBF         { get; set; }
            public bool RCK          { get; set; }
            public bool BCK          { get; set; }

            public bool   TestWrite         { get; set; }
            public ushort MaxWriteSpeed     { get; set; }
            public ushort CurrentWriteSpeed { get; set; }

            public bool ReadBarcode { get; set; }

            public bool   ReadDVDRAM   { get; set; }
            public bool   ReadDVDR     { get; set; }
            public bool   ReadDVDROM   { get; set; }
            public bool   WriteDVDRAM  { get; set; }
            public bool   WriteDVDR    { get; set; }
            public bool   LeadInPW     { get; set; }
            public bool   SCC          { get; set; }
            public ushort CMRSupported { get; set; }

            public bool   BUF                       { get; set; }
            public byte   RotationControlSelected   { get; set; }
            public ushort CurrentWriteSpeedSelected { get; set; }

            [JsonIgnore]
            [Key]
            public int Id { get; set; }
        }

        public struct ModePage_2A_WriteDescriptor
        {
            public byte   RotationControl;
            public ushort WriteSpeed;
        }

        public static ModePage_2A DecodeModePage_2A(byte[] pageResponse)
        {
            if((pageResponse?[0] & 0x40) == 0x40) return null;

            if((pageResponse?[0] & 0x3F) != 0x2A) return null;

            if(pageResponse[1] + 2 != pageResponse.Length) return null;

            if(pageResponse.Length < 16) return null;

            ModePage_2A decoded = new ModePage_2A();

            decoded.PS |= (pageResponse[0] & 0x80) == 0x80;

            decoded.AudioPlay    |= (pageResponse[4] & 0x01) == 0x01;
            decoded.Mode2Form1   |= (pageResponse[4] & 0x10) == 0x10;
            decoded.Mode2Form2   |= (pageResponse[4] & 0x20) == 0x20;
            decoded.MultiSession |= (pageResponse[4] & 0x40) == 0x40;

            decoded.CDDACommand           |= (pageResponse[5] & 0x01) == 0x01;
            decoded.AccurateCDDA          |= (pageResponse[5] & 0x02) == 0x02;
            decoded.Subchannel            |= (pageResponse[5] & 0x04) == 0x04;
            decoded.DeinterlaveSubchannel |= (pageResponse[5] & 0x08) == 0x08;
            decoded.C2Pointer             |= (pageResponse[5] & 0x10) == 0x10;
            decoded.UPC                   |= (pageResponse[5] & 0x20) == 0x20;
            decoded.ISRC                  |= (pageResponse[5] & 0x40) == 0x40;

            decoded.LoadingMechanism =  (byte)((pageResponse[6] & 0xE0) >> 5);
            decoded.Lock             |= (pageResponse[6] & 0x01) == 0x01;
            decoded.LockState        |= (pageResponse[6] & 0x02) == 0x02;
            decoded.PreventJumper    |= (pageResponse[6] & 0x04) == 0x04;
            decoded.Eject            |= (pageResponse[6] & 0x08) == 0x08;

            decoded.SeparateChannelVolume |= (pageResponse[7] & 0x01) == 0x01;
            decoded.SeparateChannelMute   |= (pageResponse[7] & 0x02) == 0x02;

            decoded.MaximumSpeed          = (ushort)((pageResponse[8]  << 8) + pageResponse[9]);
            decoded.SupportedVolumeLevels = (ushort)((pageResponse[10] << 8) + pageResponse[11]);
            decoded.BufferSize            = (ushort)((pageResponse[12] << 8) + pageResponse[13]);
            decoded.CurrentSpeed          = (ushort)((pageResponse[14] << 8) + pageResponse[15]);

            if(pageResponse.Length < 20) return decoded;

            decoded.Method2  |= (pageResponse[2] & 0x04) == 0x04;
            decoded.ReadCDRW |= (pageResponse[2] & 0x02) == 0x02;
            decoded.ReadCDR  |= (pageResponse[2] & 0x01) == 0x01;

            decoded.WriteCDRW |= (pageResponse[3] & 0x02) == 0x02;
            decoded.WriteCDR  |= (pageResponse[3] & 0x01) == 0x01;

            decoded.Composite    |= (pageResponse[4] & 0x02) == 0x02;
            decoded.DigitalPort1 |= (pageResponse[4] & 0x04) == 0x04;
            decoded.DigitalPort2 |= (pageResponse[4] & 0x08) == 0x08;

            decoded.SDP |= (pageResponse[7] & 0x04) == 0x04;
            decoded.SSS |= (pageResponse[7] & 0x08) == 0x08;

            decoded.Length =  (byte)((pageResponse[17] & 0x30) >> 4);
            decoded.LSBF   |= (pageResponse[17] & 0x08) == 0x08;
            decoded.RCK    |= (pageResponse[17] & 0x04) == 0x04;
            decoded.BCK    |= (pageResponse[17] & 0x02) == 0x02;

            if(pageResponse.Length < 22) return decoded;

            decoded.TestWrite         |= (pageResponse[3] & 0x04) == 0x04;
            decoded.MaxWriteSpeed     =  (ushort)((pageResponse[18] << 8) + pageResponse[19]);
            decoded.CurrentWriteSpeed =  (ushort)((pageResponse[20] << 8) + pageResponse[21]);

            decoded.ReadBarcode |= (pageResponse[5] & 0x80) == 0x80;

            if(pageResponse.Length < 26) return decoded;

            decoded.ReadDVDRAM |= (pageResponse[2] & 0x20) == 0x20;
            decoded.ReadDVDR   |= (pageResponse[2] & 0x10) == 0x10;
            decoded.ReadDVDROM |= (pageResponse[2] & 0x08) == 0x08;

            decoded.WriteDVDRAM |= (pageResponse[3] & 0x20) == 0x20;
            decoded.WriteDVDR   |= (pageResponse[3] & 0x10) == 0x10;

            decoded.LeadInPW |= (pageResponse[3] & 0x20) == 0x20;
            decoded.SCC      |= (pageResponse[3] & 0x10) == 0x10;

            decoded.CMRSupported = (ushort)((pageResponse[22] << 8) + pageResponse[23]);

            if(pageResponse.Length < 32) return decoded;

            decoded.BUF                       |= (pageResponse[4]        & 0x80) == 0x80;
            decoded.RotationControlSelected   =  (byte)(pageResponse[27] & 0x03);
            decoded.CurrentWriteSpeedSelected =  (ushort)((pageResponse[28] << 8) + pageResponse[29]);

            ushort descriptors = (ushort)((pageResponse.Length - 32) / 4);
            decoded.WriteSpeedPerformanceDescriptors = new ModePage_2A_WriteDescriptor[descriptors];

            for(int i = 0; i < descriptors; i++)
                decoded.WriteSpeedPerformanceDescriptors[i] = new ModePage_2A_WriteDescriptor
                {
                    RotationControl =
                        (byte)(pageResponse[1 + 32 + i * 4] & 0x07),
                    WriteSpeed = (ushort)((pageResponse[2 + 32 + i * 4] << 8) + pageResponse[3 + 32 + i * 4])
                };

            return decoded;
        }

        public static byte[] EncodeModePage_2A(ModePage_2A decoded)
        {
            byte[] pageResponse = new byte[512];
            byte   length       = 16;

            pageResponse[0] = 0x2A;

            if(decoded.PS) pageResponse[0] += 0x80;

            if(decoded.AudioPlay) pageResponse[4]    += 0x01;
            if(decoded.Mode2Form1) pageResponse[4]   += 0x10;
            if(decoded.Mode2Form2) pageResponse[4]   += 0x20;
            if(decoded.MultiSession) pageResponse[4] += 0x40;

            if(decoded.CDDACommand) pageResponse[5]           += 0x01;
            if(decoded.AccurateCDDA) pageResponse[5]          += 0x02;
            if(decoded.Subchannel) pageResponse[5]            += 0x04;
            if(decoded.DeinterlaveSubchannel) pageResponse[5] += 0x08;
            if(decoded.C2Pointer) pageResponse[5]             += 0x10;
            if(decoded.UPC) pageResponse[5]                   += 0x20;
            if(decoded.ISRC) pageResponse[5]                  += 0x40;

            decoded.LoadingMechanism = (byte)((pageResponse[6] & 0xE0) >> 5);
            if(decoded.Lock) pageResponse[6]          += 0x01;
            if(decoded.LockState) pageResponse[6]     += 0x02;
            if(decoded.PreventJumper) pageResponse[6] += 0x04;
            if(decoded.Eject) pageResponse[6]         += 0x08;

            if(decoded.SeparateChannelVolume) pageResponse[7] += 0x01;
            if(decoded.SeparateChannelMute) pageResponse[7]   += 0x02;

            decoded.MaximumSpeed          = (ushort)((pageResponse[8]  << 8) + pageResponse[9]);
            decoded.SupportedVolumeLevels = (ushort)((pageResponse[10] << 8) + pageResponse[11]);
            decoded.BufferSize            = (ushort)((pageResponse[12] << 8) + pageResponse[13]);
            decoded.CurrentSpeed          = (ushort)((pageResponse[14] << 8) + pageResponse[15]);

            if(decoded.Method2    || decoded.ReadCDRW || decoded.ReadCDR || decoded.WriteCDRW ||
               decoded.WriteCDR   ||
               decoded.Composite  || decoded.DigitalPort1 || decoded.DigitalPort2 || decoded.SDP ||
               decoded.SSS        ||
               decoded.Length > 0 || decoded.LSBF || decoded.RCK || decoded.BCK)
            {
                length = 20;
                if(decoded.Method2) pageResponse[2]  += 0x04;
                if(decoded.ReadCDRW) pageResponse[2] += 0x02;
                if(decoded.ReadCDR) pageResponse[2]  += 0x01;

                if(decoded.WriteCDRW) pageResponse[3] += 0x02;
                if(decoded.WriteCDR) pageResponse[3]  += 0x01;

                if(decoded.Composite) pageResponse[4]    += 0x02;
                if(decoded.DigitalPort1) pageResponse[4] += 0x04;
                if(decoded.DigitalPort2) pageResponse[4] += 0x08;

                if(decoded.SDP) pageResponse[7] += 0x04;
                if(decoded.SSS) pageResponse[7] += 0x08;

                pageResponse[17] = (byte)(decoded.Length << 4);
                if(decoded.LSBF) pageResponse[17] += 0x08;
                if(decoded.RCK) pageResponse[17]  += 0x04;
                if(decoded.BCK) pageResponse[17]  += 0x02;
            }

            if(decoded.TestWrite || decoded.MaxWriteSpeed > 0 || decoded.CurrentWriteSpeed > 0 || decoded.ReadBarcode)
            {
                length = 22;
                if(decoded.TestWrite) pageResponse[3] += 0x04;
                pageResponse[18] = (byte)((decoded.MaxWriteSpeed & 0xFF00) >> 8);
                pageResponse[19] = (byte)(decoded.MaxWriteSpeed & 0xFF);
                pageResponse[20] = (byte)((decoded.CurrentWriteSpeed & 0xFF00) >> 8);
                pageResponse[21] = (byte)(decoded.CurrentWriteSpeed & 0xFF);
                if(decoded.ReadBarcode) pageResponse[5] += 0x80;
            }

            if(decoded.ReadDVDRAM || decoded.ReadDVDR || decoded.ReadDVDROM || decoded.WriteDVDRAM ||
               decoded.WriteDVDR  || decoded.LeadInPW || decoded.SCC        || decoded.CMRSupported > 0)

            {
                length = 26;
                if(decoded.ReadDVDRAM) pageResponse[2] += 0x20;
                if(decoded.ReadDVDR) pageResponse[2]   += 0x10;
                if(decoded.ReadDVDROM) pageResponse[2] += 0x08;

                if(decoded.WriteDVDRAM) pageResponse[3] += 0x20;
                if(decoded.WriteDVDR) pageResponse[3]   += 0x10;

                if(decoded.LeadInPW) pageResponse[3] += 0x20;
                if(decoded.SCC) pageResponse[3]      += 0x10;

                pageResponse[22] = (byte)((decoded.CMRSupported & 0xFF00) >> 8);
                pageResponse[23] = (byte)(decoded.CMRSupported & 0xFF);
            }

            if(decoded.BUF || decoded.RotationControlSelected > 0 || decoded.CurrentWriteSpeedSelected > 0)
            {
                length = 32;
                if(decoded.BUF) pageResponse[4] += 0x80;
                pageResponse[27] += decoded.RotationControlSelected;
                pageResponse[28] =  (byte)((decoded.CurrentWriteSpeedSelected & 0xFF00) >> 8);
                pageResponse[29] =  (byte)(decoded.CurrentWriteSpeedSelected & 0xFF);
            }

            if(decoded.WriteSpeedPerformanceDescriptors != null)
            {
                length = 32;
                for(int i = 0; i < decoded.WriteSpeedPerformanceDescriptors.Length; i++)
                {
                    length                       += 4;
                    pageResponse[1 + 32 + i * 4] =  decoded.WriteSpeedPerformanceDescriptors[i].RotationControl;
                    pageResponse[2 + 32 + i * 4] =
                        (byte)((decoded.WriteSpeedPerformanceDescriptors[i].WriteSpeed & 0xFF00) >> 8);
                    pageResponse[3 + 32 + i * 4] =
                        (byte)(decoded.WriteSpeedPerformanceDescriptors[i].WriteSpeed & 0xFF);
                }
            }

            pageResponse[1] = (byte)(length - 2);
            byte[] buf = new byte[length];
            Array.Copy(pageResponse, 0, buf, 0, length);
            return buf;
        }

        public static string PrettifyModePage_2A(byte[] pageResponse)
        {
            return PrettifyModePage_2A(DecodeModePage_2A(pageResponse));
        }

        public static string PrettifyModePage_2A(ModePage_2A modePage)
        {
            if(modePage is null) return null;

            ModePage_2A   page = modePage;
            StringBuilder sb   = new StringBuilder();

            sb.AppendLine("SCSI CD-ROM capabilities page:");

            if(page.PS) sb.AppendLine("\tParameters can be saved");

            if(page.AudioPlay) sb.AppendLine("\tDrive can play audio");
            if(page.Mode2Form1) sb.AppendLine("\tDrive can read sectors in Mode 2 Form 1 format");
            if(page.Mode2Form2) sb.AppendLine("\tDrive can read sectors in Mode 2 Form 2 format");
            if(page.MultiSession) sb.AppendLine("\tDrive supports multi-session discs and/or Photo-CD");

            if(page.CDDACommand) sb.AppendLine("\tDrive can read digital audio");
            if(page.AccurateCDDA) sb.AppendLine("\tDrive can continue from streaming loss");
            if(page.Subchannel) sb.AppendLine("\tDrive can read uncorrected and interleaved R-W subchannels");
            if(page.DeinterlaveSubchannel) sb.AppendLine("\tDrive can read, deinterleave and correct R-W subchannels");
            if(page.C2Pointer) sb.AppendLine("\tDrive supports C2 pointers");
            if(page.UPC) sb.AppendLine("\tDrive can read Media Catalogue Number");
            if(page.ISRC) sb.AppendLine("\tDrive can read ISRC");

            switch(page.LoadingMechanism)
            {
                case 0:
                    sb.AppendLine("\tDrive uses media caddy");
                    break;
                case 1:
                    sb.AppendLine("\tDrive uses a tray");
                    break;
                case 2:
                    sb.AppendLine("\tDrive is pop-up");
                    break;
                case 4:
                    sb.AppendLine("\tDrive is a changer with individually changeable discs");
                    break;
                case 5:
                    sb.AppendLine("\tDrive is a changer using cartridges");
                    break;
                default:
                    sb.AppendFormat("\tDrive uses unknown loading mechanism type {0}", page.LoadingMechanism)
                      .AppendLine();
                    break;
            }

            if(page.Lock) sb.AppendLine("\tDrive can lock media");
            if(page.PreventJumper)
            {
                sb.AppendLine("\tDrive power ups locked");
                sb.AppendLine(page.LockState
                                  ? "\tDrive is locked, media cannot be ejected or inserted"
                                  : "\tDrive is not locked, media can be ejected and inserted");
            }
            else
                sb.AppendLine(page.LockState
                                  ? "\tDrive is locked, media cannot be ejected, but if empty, can be inserted"
                                  : "\tDrive is not locked, media can be ejected and inserted");

            if(page.Eject) sb.AppendLine("\tDrive can eject media");

            if(page.SeparateChannelMute) sb.AppendLine("\tEach channel can be muted independently");
            if(page.SeparateChannelVolume) sb.AppendLine("\tEach channel's volume can be controlled independently");

            if(page.SupportedVolumeLevels > 0)
                sb.AppendFormat("\tDrive supports {0} volume levels", page.SupportedVolumeLevels).AppendLine();
            if(page.BufferSize > 0) sb.AppendFormat("\tDrive has {0} Kbyte of buffer", page.BufferSize).AppendLine();
            if(page.MaximumSpeed > 0)
                sb.AppendFormat("\tDrive's maximum reading speed is {0} Kbyte/sec.", page.MaximumSpeed).AppendLine();
            if(page.CurrentSpeed > 0)
                sb.AppendFormat("\tDrive's current reading speed is {0} Kbyte/sec.", page.CurrentSpeed).AppendLine();

            if(page.ReadCDR)
            {
                sb.AppendLine(page.WriteCDR ? "\tDrive can read and write CD-R" : "\tDrive can read CD-R");

                if(page.Method2) sb.AppendLine("\tDrive supports reading CD-R packet media");
            }

            if(page.ReadCDRW)
                sb.AppendLine(page.WriteCDRW ? "\tDrive can read and write CD-RW" : "\tDrive can read CD-RW");

            if(page.ReadDVDROM) sb.AppendLine("\tDrive can read DVD-ROM");
            if(page.ReadDVDR)
                sb.AppendLine(page.WriteDVDR ? "\tDrive can read and write DVD-R" : "\tDrive can read DVD-R");
            if(page.ReadDVDRAM)
                sb.AppendLine(page.WriteDVDRAM ? "\tDrive can read and write DVD-RAM" : "\tDrive can read DVD-RAM");

            if(page.Composite) sb.AppendLine("\tDrive can deliver a composite audio and video data stream");
            if(page.DigitalPort1) sb.AppendLine("\tDrive supports IEC-958 digital output on port 1");
            if(page.DigitalPort2) sb.AppendLine("\tDrive supports IEC-958 digital output on port 2");

            if(page.SDP) sb.AppendLine("\tDrive contains a changer that can report the exact contents of the slots");
            if(page.CurrentWriteSpeedSelected > 0)
            {
                if(page.RotationControlSelected == 0)
                    sb.AppendFormat("\tDrive's current writing speed is {0} Kbyte/sec. in CLV mode",
                                    page.CurrentWriteSpeedSelected).AppendLine();
                else if(page.RotationControlSelected == 1)
                    sb.AppendFormat("\tDrive's current writing speed is {0} Kbyte/sec. in pure CAV mode",
                                    page.CurrentWriteSpeedSelected).AppendLine();
            }
            else
            {
                if(page.MaxWriteSpeed > 0)
                    sb.AppendFormat("\tDrive's maximum writing speed is {0} Kbyte/sec.", page.MaxWriteSpeed)
                      .AppendLine();
                if(page.CurrentWriteSpeed > 0)
                    sb.AppendFormat("\tDrive's current writing speed is {0} Kbyte/sec.", page.CurrentWriteSpeed)
                      .AppendLine();
            }

            if(page.WriteSpeedPerformanceDescriptors != null)
                foreach(ModePage_2A_WriteDescriptor descriptor in
                    page.WriteSpeedPerformanceDescriptors.Where(descriptor => descriptor.WriteSpeed > 0))
                    if(descriptor.RotationControl == 0)
                        sb.AppendFormat("\tDrive supports writing at {0} Kbyte/sec. in CLV mode", descriptor.WriteSpeed)
                          .AppendLine();
                    else if(descriptor.RotationControl == 1)
                        sb.AppendFormat("\tDrive supports writing at is {0} Kbyte/sec. in pure CAV mode",
                                        descriptor.WriteSpeed).AppendLine();

            if(page.TestWrite) sb.AppendLine("\tDrive supports test writing");

            if(page.ReadBarcode) sb.AppendLine("\tDrive can read barcode");

            if(page.SCC) sb.AppendLine("\tDrive can read both sides of a disc");
            if(page.LeadInPW) sb.AppendLine("\tDrive an read raw R-W subchannel from the Lead-In");

            if(page.CMRSupported == 1) sb.AppendLine("\tDrive supports DVD CSS and/or DVD CPPM");

            if(page.BUF) sb.AppendLine("\tDrive supports buffer under-run free recording");

            return sb.ToString();
        }
        #endregion Mode Page 0x2A: CD-ROM capabilities page
    }
}