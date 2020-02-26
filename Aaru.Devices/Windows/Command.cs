// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : Command.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Windows direct device access.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains a high level representation of the Windows syscalls used to
//     directly interface devices.
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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using DiscImageChef.Decoders.ATA;
using Microsoft.Win32.SafeHandles;

namespace DiscImageChef.Devices.Windows
{
    [SuppressMessage("ReSharper", "UnusedParameter.Global")]
    internal static class Command
    {
        /// <summary>
        ///     Sends a SCSI command
        /// </summary>
        /// <returns>0 if no error occurred, otherwise, errno</returns>
        /// <param name="fd">File handle</param>
        /// <param name="cdb">SCSI CDB</param>
        /// <param name="buffer">Buffer for SCSI command response</param>
        /// <param name="senseBuffer">Buffer with the SCSI sense</param>
        /// <param name="timeout">Timeout in seconds</param>
        /// <param name="direction">SCSI command transfer direction</param>
        /// <param name="duration">Time it took to execute the command in milliseconds</param>
        /// <param name="sense">
        ///     <c>True</c> if SCSI error returned non-OK status and <paramref name="senseBuffer" /> contains SCSI
        ///     sense
        /// </param>
        internal static int SendScsiCommand(SafeFileHandle fd, byte[] cdb, ref byte[] buffer,
            out byte[] senseBuffer,
            uint timeout, ScsiIoctlDirection direction, out double duration,
            out bool sense)
        {
            senseBuffer = null;
            duration = 0;
            sense = false;

            if (buffer == null) return -1;

            var sptdSb = new ScsiPassThroughDirectAndSenseBuffer
            {
                SenseBuf = new byte[32],
                sptd = new ScsiPassThroughDirect
                {
                    Cdb = new byte[16],
                    CdbLength = (byte) cdb.Length,
                    SenseInfoLength = 32,
                    DataIn = direction,
                    DataTransferLength = (uint) buffer.Length,
                    TimeOutValue = timeout,
                    DataBuffer = Marshal.AllocHGlobal(buffer.Length)
                }
            };
            sptdSb.sptd.Length = (ushort) Marshal.SizeOf(sptdSb.sptd);
            sptdSb.sptd.SenseInfoOffset = (uint) Marshal.SizeOf(sptdSb.sptd);
            Array.Copy(cdb, sptdSb.sptd.Cdb, cdb.Length);

            uint k = 0;
            var error = 0;

            Marshal.Copy(buffer, 0, sptdSb.sptd.DataBuffer, buffer.Length);

            var start = DateTime.Now;
            var hasError = !Extern.DeviceIoControlScsi(fd, WindowsIoctl.IoctlScsiPassThroughDirect, ref sptdSb,
                (uint) Marshal.SizeOf(sptdSb), ref sptdSb,
                (uint) Marshal.SizeOf(sptdSb), ref k, IntPtr.Zero);
            var end = DateTime.Now;

            if (hasError) error = Marshal.GetLastWin32Error();

            Marshal.Copy(sptdSb.sptd.DataBuffer, buffer, 0, buffer.Length);

            sense |= sptdSb.sptd.ScsiStatus != 0;

            senseBuffer = new byte[32];
            Array.Copy(sptdSb.SenseBuf, senseBuffer, 32);

            duration = (end - start).TotalMilliseconds;

            Marshal.FreeHGlobal(sptdSb.sptd.DataBuffer);

            return error;
        }

        /// <summary>
        ///     Sends an ATA command in CHS mode
        /// </summary>
        /// <returns>0 if no error occurred, otherwise, errno</returns>
        /// <param name="fd">File handle</param>
        /// <param name="buffer">Buffer for SCSI command response</param>
        /// <param name="timeout">Timeout in seconds</param>
        /// <param name="duration">Time it took to execute the command in milliseconds</param>
        /// <param name="sense"><c>True</c> if ATA error returned non-OK status</param>
        /// <param name="registers">Registers to send to drive</param>
        /// <param name="errorRegisters">Registers returned by drive</param>
        /// <param name="protocol">ATA protocol to use</param>
        internal static int SendAtaCommand(SafeFileHandle fd, AtaRegistersChs registers,
            out AtaErrorRegistersChs errorRegisters, AtaProtocol protocol,
            ref byte[] buffer, uint timeout,
            out double duration, out bool sense)
        {
            duration = 0;
            sense = false;
            errorRegisters = new AtaErrorRegistersChs();

            if (buffer == null) return -1;

            var offsetForBuffer = (uint) (Marshal.SizeOf(typeof(AtaPassThroughEx)) + Marshal.SizeOf(typeof(uint)));

            var aptdBuf = new AtaPassThroughExBuffer
            {
                aptd = new AtaPassThroughEx
                {
                    TimeOutValue = timeout,
                    DataBufferOffset = (IntPtr) offsetForBuffer,
                    Length = (ushort) Marshal.SizeOf(typeof(AtaPassThroughEx)),
                    DataTransferLength = (uint) buffer.Length,
                    PreviousTaskFile = new AtaTaskFile(),
                    CurrentTaskFile = new AtaTaskFile
                    {
                        Command = registers.Command,
                        CylinderHigh = registers.CylinderHigh,
                        CylinderLow = registers.CylinderLow,
                        DeviceHead = registers.DeviceHead,
                        Features = registers.Feature,
                        SectorCount = registers.SectorCount,
                        SectorNumber = registers.Sector
                    }
                },
                dataBuffer = new byte[64 * 512]
            };

            switch (protocol)
            {
                case AtaProtocol.PioIn:
                case AtaProtocol.UDmaIn:
                case AtaProtocol.Dma:
                    aptdBuf.aptd.AtaFlags = AtaFlags.DataIn;
                    break;
                case AtaProtocol.PioOut:
                case AtaProtocol.UDmaOut:
                    aptdBuf.aptd.AtaFlags = AtaFlags.DataOut;
                    break;
            }

            switch (protocol)
            {
                case AtaProtocol.Dma:
                case AtaProtocol.DmaQueued:
                case AtaProtocol.FpDma:
                case AtaProtocol.UDmaIn:
                case AtaProtocol.UDmaOut:
                    aptdBuf.aptd.AtaFlags |= AtaFlags.Dma;
                    break;
            }

            // Unknown if needed
            aptdBuf.aptd.AtaFlags |= AtaFlags.DrdyRequired;

            uint k = 0;
            var error = 0;

            Array.Copy(buffer, 0, aptdBuf.dataBuffer, 0, buffer.Length);

            var start = DateTime.Now;
            sense = !Extern.DeviceIoControlAta(fd, WindowsIoctl.IoctlAtaPassThrough, ref aptdBuf,
                (uint) Marshal.SizeOf(aptdBuf), ref aptdBuf,
                (uint) Marshal.SizeOf(aptdBuf), ref k, IntPtr.Zero);
            var end = DateTime.Now;

            if (sense) error = Marshal.GetLastWin32Error();

            Array.Copy(aptdBuf.dataBuffer, 0, buffer, 0, buffer.Length);

            duration = (end - start).TotalMilliseconds;

            errorRegisters.CylinderHigh = aptdBuf.aptd.CurrentTaskFile.CylinderHigh;
            errorRegisters.CylinderLow = aptdBuf.aptd.CurrentTaskFile.CylinderLow;
            errorRegisters.DeviceHead = aptdBuf.aptd.CurrentTaskFile.DeviceHead;
            errorRegisters.Error = aptdBuf.aptd.CurrentTaskFile.Error;
            errorRegisters.Sector = aptdBuf.aptd.CurrentTaskFile.SectorNumber;
            errorRegisters.SectorCount = aptdBuf.aptd.CurrentTaskFile.SectorCount;
            errorRegisters.Status = aptdBuf.aptd.CurrentTaskFile.Status;

            sense = errorRegisters.Error != 0 || (errorRegisters.Status & 0xA5) != 0;

            return error;
        }

        /// <summary>
        ///     Sends an ATA command in 28-bit LBA mode
        /// </summary>
        /// <returns>0 if no error occurred, otherwise, errno</returns>
        /// <param name="fd">File handle</param>
        /// <param name="buffer">Buffer for SCSI command response</param>
        /// <param name="timeout">Timeout in seconds</param>
        /// <param name="duration">Time it took to execute the command in milliseconds</param>
        /// <param name="sense"><c>True</c> if ATA error returned non-OK status</param>
        /// <param name="registers">Registers to send to drive</param>
        /// <param name="errorRegisters">Registers returned by drive</param>
        /// <param name="protocol">ATA protocol to use</param>
        internal static int SendAtaCommand(SafeFileHandle fd, AtaRegistersLba28 registers,
            out AtaErrorRegistersLba28 errorRegisters, AtaProtocol protocol,
            ref byte[] buffer, uint timeout,
            out double duration, out bool sense)
        {
            duration = 0;
            sense = false;
            errorRegisters = new AtaErrorRegistersLba28();

            if (buffer == null) return -1;

            var offsetForBuffer = (uint) (Marshal.SizeOf(typeof(AtaPassThroughEx)) + Marshal.SizeOf(typeof(uint)));

            var aptdBuf = new AtaPassThroughExBuffer
            {
                aptd = new AtaPassThroughEx
                {
                    TimeOutValue = timeout,
                    DataBufferOffset = (IntPtr) offsetForBuffer,
                    Length = (ushort) Marshal.SizeOf(typeof(AtaPassThroughEx)),
                    DataTransferLength = (uint) buffer.Length,
                    PreviousTaskFile = new AtaTaskFile(),
                    CurrentTaskFile = new AtaTaskFile
                    {
                        Command = registers.Command,
                        CylinderHigh = registers.LbaHigh,
                        CylinderLow = registers.LbaMid,
                        DeviceHead = registers.DeviceHead,
                        Features = registers.Feature,
                        SectorCount = registers.SectorCount,
                        SectorNumber = registers.LbaLow
                    }
                },
                dataBuffer = new byte[64 * 512]
            };

            switch (protocol)
            {
                case AtaProtocol.PioIn:
                case AtaProtocol.UDmaIn:
                case AtaProtocol.Dma:
                    aptdBuf.aptd.AtaFlags = AtaFlags.DataIn;
                    break;
                case AtaProtocol.PioOut:
                case AtaProtocol.UDmaOut:
                    aptdBuf.aptd.AtaFlags = AtaFlags.DataOut;
                    break;
            }

            switch (protocol)
            {
                case AtaProtocol.Dma:
                case AtaProtocol.DmaQueued:
                case AtaProtocol.FpDma:
                case AtaProtocol.UDmaIn:
                case AtaProtocol.UDmaOut:
                    aptdBuf.aptd.AtaFlags |= AtaFlags.Dma;
                    break;
            }

            // Unknown if needed
            aptdBuf.aptd.AtaFlags |= AtaFlags.DrdyRequired;

            uint k = 0;
            var error = 0;

            Array.Copy(buffer, 0, aptdBuf.dataBuffer, 0, buffer.Length);

            var start = DateTime.Now;
            sense = !Extern.DeviceIoControlAta(fd, WindowsIoctl.IoctlAtaPassThrough, ref aptdBuf,
                (uint) Marshal.SizeOf(aptdBuf), ref aptdBuf,
                (uint) Marshal.SizeOf(aptdBuf), ref k, IntPtr.Zero);
            var end = DateTime.Now;

            if (sense) error = Marshal.GetLastWin32Error();

            Array.Copy(aptdBuf.dataBuffer, 0, buffer, 0, buffer.Length);

            duration = (end - start).TotalMilliseconds;

            errorRegisters.LbaHigh = aptdBuf.aptd.CurrentTaskFile.CylinderHigh;
            errorRegisters.LbaMid = aptdBuf.aptd.CurrentTaskFile.CylinderLow;
            errorRegisters.DeviceHead = aptdBuf.aptd.CurrentTaskFile.DeviceHead;
            errorRegisters.Error = aptdBuf.aptd.CurrentTaskFile.Error;
            errorRegisters.LbaLow = aptdBuf.aptd.CurrentTaskFile.SectorNumber;
            errorRegisters.SectorCount = aptdBuf.aptd.CurrentTaskFile.SectorCount;
            errorRegisters.Status = aptdBuf.aptd.CurrentTaskFile.Status;

            sense = errorRegisters.Error != 0 || (errorRegisters.Status & 0xA5) != 0;

            return error;
        }

        /// <summary>
        ///     Sends an ATA command in 48-bit LBA mode
        /// </summary>
        /// <returns>0 if no error occurred, otherwise, errno</returns>
        /// <param name="fd">File handle</param>
        /// <param name="buffer">Buffer for SCSI command response</param>
        /// <param name="timeout">Timeout in seconds</param>
        /// <param name="duration">Time it took to execute the command in milliseconds</param>
        /// <param name="sense"><c>True</c> if ATA error returned non-OK status</param>
        /// <param name="registers">Registers to send to drive</param>
        /// <param name="errorRegisters">Registers returned by drive</param>
        /// <param name="protocol">ATA protocol to use</param>
        internal static int SendAtaCommand(SafeFileHandle fd, AtaRegistersLba48 registers,
            out AtaErrorRegistersLba48 errorRegisters, AtaProtocol protocol,
            ref byte[] buffer, uint timeout,
            out double duration, out bool sense)
        {
            duration = 0;
            sense = false;
            errorRegisters = new AtaErrorRegistersLba48();

            if (buffer == null) return -1;

            var offsetForBuffer = (uint) (Marshal.SizeOf(typeof(AtaPassThroughEx)) + Marshal.SizeOf(typeof(uint)));

            var aptdBuf = new AtaPassThroughExBuffer
            {
                aptd = new AtaPassThroughEx
                {
                    TimeOutValue = timeout,
                    DataBufferOffset = (IntPtr) offsetForBuffer,
                    Length = (ushort) Marshal.SizeOf(typeof(AtaPassThroughEx)),
                    DataTransferLength = (uint) buffer.Length,
                    PreviousTaskFile =
                        new AtaTaskFile
                        {
                            CylinderHigh = (byte) ((registers.LbaHigh & 0xFF00) >> 8),
                            CylinderLow = (byte) ((registers.LbaMid & 0xFF00) >> 8),
                            Features = (byte) ((registers.Feature & 0xFF00) >> 8),
                            SectorCount = (byte) ((registers.SectorCount & 0xFF00) >> 8),
                            SectorNumber = (byte) ((registers.LbaLow & 0xFF00) >> 8)
                        },
                    CurrentTaskFile = new AtaTaskFile
                    {
                        Command = registers.Command,
                        CylinderHigh = (byte) (registers.LbaHigh & 0xFF),
                        CylinderLow = (byte) (registers.LbaMid & 0xFF),
                        DeviceHead = registers.DeviceHead,
                        Features = (byte) (registers.Feature & 0xFF),
                        SectorCount = (byte) (registers.SectorCount & 0xFF),
                        SectorNumber = (byte) (registers.LbaLow & 0xFF)
                    }
                },
                dataBuffer = new byte[64 * 512]
            };

            switch (protocol)
            {
                case AtaProtocol.PioIn:
                case AtaProtocol.UDmaIn:
                case AtaProtocol.Dma:
                    aptdBuf.aptd.AtaFlags = AtaFlags.DataIn;
                    break;
                case AtaProtocol.PioOut:
                case AtaProtocol.UDmaOut:
                    aptdBuf.aptd.AtaFlags = AtaFlags.DataOut;
                    break;
            }

            switch (protocol)
            {
                case AtaProtocol.Dma:
                case AtaProtocol.DmaQueued:
                case AtaProtocol.FpDma:
                case AtaProtocol.UDmaIn:
                case AtaProtocol.UDmaOut:
                    aptdBuf.aptd.AtaFlags |= AtaFlags.Dma;
                    break;
            }

            aptdBuf.aptd.AtaFlags |= AtaFlags.ExtendedCommand;

            // Unknown if needed
            aptdBuf.aptd.AtaFlags |= AtaFlags.DrdyRequired;

            uint k = 0;
            var error = 0;

            Array.Copy(buffer, 0, aptdBuf.dataBuffer, 0, buffer.Length);

            var start = DateTime.Now;
            sense = !Extern.DeviceIoControlAta(fd, WindowsIoctl.IoctlAtaPassThrough, ref aptdBuf,
                (uint) Marshal.SizeOf(aptdBuf), ref aptdBuf,
                (uint) Marshal.SizeOf(aptdBuf), ref k, IntPtr.Zero);
            var end = DateTime.Now;

            if (sense) error = Marshal.GetLastWin32Error();

            Array.Copy(aptdBuf.dataBuffer, 0, buffer, 0, buffer.Length);

            duration = (end - start).TotalMilliseconds;

            errorRegisters.SectorCount = (ushort) ((aptdBuf.aptd.PreviousTaskFile.SectorCount << 8) +
                                                   aptdBuf.aptd.CurrentTaskFile.SectorCount);
            errorRegisters.LbaLow = (ushort) ((aptdBuf.aptd.PreviousTaskFile.SectorNumber << 8) +
                                              aptdBuf.aptd.CurrentTaskFile.SectorNumber);
            errorRegisters.LbaMid = (ushort) ((aptdBuf.aptd.PreviousTaskFile.CylinderLow << 8) +
                                              aptdBuf.aptd.CurrentTaskFile.CylinderLow);
            errorRegisters.LbaHigh = (ushort) ((aptdBuf.aptd.PreviousTaskFile.CylinderHigh << 8) +
                                               aptdBuf.aptd.CurrentTaskFile.CylinderHigh);
            errorRegisters.DeviceHead = aptdBuf.aptd.CurrentTaskFile.DeviceHead;
            errorRegisters.Error = aptdBuf.aptd.CurrentTaskFile.Error;
            errorRegisters.Status = aptdBuf.aptd.CurrentTaskFile.Status;

            sense = errorRegisters.Error != 0 || (errorRegisters.Status & 0xA5) != 0;

            return error;
        }

        /// <summary>
        ///     Gets the device number for a specified handle
        /// </summary>
        /// <param name="deviceHandle">Device handle</param>
        /// <returns>Device number</returns>
        private static uint GetDeviceNumber(SafeFileHandle deviceHandle)
        {
            var sdn = new StorageDeviceNumber {deviceNumber = -1};
            uint k = 0;
            if (!Extern.DeviceIoControlGetDeviceNumber(deviceHandle, WindowsIoctl.IoctlStorageGetDeviceNumber,
                IntPtr.Zero, 0, ref sdn, (uint) Marshal.SizeOf(sdn), ref k,
                IntPtr.Zero)) return uint.MaxValue;

            return (uint) sdn.deviceNumber;
        }

        /// <summary>
        ///     Returns true if the specified handle is controlled by a SFFDISK (aka SDHCI) driver
        /// </summary>
        /// <param name="fd">Device handle</param>
        /// <returns><c>true</c> if SDHCI, false otherwise</returns>
        internal static bool IsSdhci(SafeFileHandle fd)
        {
            var queryData1 = new SffdiskQueryDeviceProtocolData();
            queryData1.size = (ushort) Marshal.SizeOf(queryData1);
            Extern.DeviceIoControl(fd, WindowsIoctl.IoctlSffdiskQueryDeviceProtocol, IntPtr.Zero, 0, ref queryData1,
                queryData1.size, out _, IntPtr.Zero);
            return queryData1.protocolGuid.Equals(Consts.GuidSffProtocolSd) ||
                   queryData1.protocolGuid.Equals(Consts.GuidSffProtocolMmc);
        }

        /// <summary>
        ///     Sends a MMC/SD command
        /// </summary>
        /// <returns>The result of the command.</returns>
        /// <param name="fd">File handle</param>
        /// <param name="command">MMC/SD opcode</param>
        /// <param name="buffer">Buffer for MMC/SD command response</param>
        /// <param name="timeout">Timeout in seconds</param>
        /// <param name="duration">Time it took to execute the command in milliseconds</param>
        /// <param name="sense"><c>True</c> if MMC/SD returned non-OK status</param>
        /// <param name="write"><c>True</c> if data is sent from host to card</param>
        /// <param name="isApplication"><c>True</c> if command should be preceded with CMD55</param>
        /// <param name="flags">Flags indicating kind and place of response</param>
        /// <param name="blocks">How many blocks to transfer</param>
        /// <param name="argument">Command argument</param>
        /// <param name="response">Response registers</param>
        /// <param name="blockSize">Size of block in bytes</param>
        internal static int SendMmcCommand(SafeFileHandle fd, MmcCommands command, bool write,
            bool isApplication,
            MmcFlags flags, uint argument, uint blockSize,
            uint blocks,
            ref byte[] buffer, out uint[] response, out double duration,
            out bool sense,
            uint timeout = 0)
        {
            var commandData = new SffdiskDeviceCommandData();
            var commandDescriptor = new SdCmdDescriptor();
            commandData.size = (ushort) Marshal.SizeOf(commandData);
            commandData.command = SffdiskDcmd.DeviceCommand;
            commandData.protocolArgumentSize = (ushort) Marshal.SizeOf(commandDescriptor);
            commandData.deviceDataBufferSize = blockSize * blocks;
            commandDescriptor.commandCode = (byte) command;
            commandDescriptor.cmdClass = isApplication ? SdCommandClass.AppCmd : SdCommandClass.Standard;
            commandDescriptor.transferDirection = write ? SdTransferDirection.Write : SdTransferDirection.Read;
            commandDescriptor.transferType = flags.HasFlag(MmcFlags.CommandAdtc)
                ? SdTransferType.SingleBlock
                : SdTransferType.CmdOnly;
            commandDescriptor.responseType = 0;

            if (flags.HasFlag(MmcFlags.ResponseR1) || flags.HasFlag(MmcFlags.ResponseSpiR1))
                commandDescriptor.responseType = SdResponseType.R1;
            if (flags.HasFlag(MmcFlags.ResponseR1B) || flags.HasFlag(MmcFlags.ResponseSpiR1B))
                commandDescriptor.responseType = SdResponseType.R1b;
            if (flags.HasFlag(MmcFlags.ResponseR2) || flags.HasFlag(MmcFlags.ResponseSpiR2))
                commandDescriptor.responseType = SdResponseType.R2;
            if (flags.HasFlag(MmcFlags.ResponseR3) || flags.HasFlag(MmcFlags.ResponseSpiR3))
                commandDescriptor.responseType = SdResponseType.R3;
            if (flags.HasFlag(MmcFlags.ResponseR4) || flags.HasFlag(MmcFlags.ResponseSpiR4))
                commandDescriptor.responseType = SdResponseType.R4;
            if (flags.HasFlag(MmcFlags.ResponseR5) || flags.HasFlag(MmcFlags.ResponseSpiR5))
                commandDescriptor.responseType = SdResponseType.R5;
            if (flags.HasFlag(MmcFlags.ResponseR6)) commandDescriptor.responseType = SdResponseType.R6;

            var commandB = new byte[commandData.size + commandData.protocolArgumentSize +
                                    commandData.deviceDataBufferSize];

            Array.Copy(buffer, 0, commandB, commandData.size + commandData.protocolArgumentSize, buffer.Length);
            var hBuf = Marshal.AllocHGlobal(commandB.Length);
            Marshal.StructureToPtr(commandData, hBuf, true);
            var descriptorOffset = IntPtr.Add(hBuf, commandData.size);
            Marshal.StructureToPtr(commandDescriptor, descriptorOffset, true);
            Marshal.Copy(hBuf, commandB, 0, commandB.Length);
            Marshal.FreeHGlobal(hBuf);

            var error = 0;
            var start = DateTime.Now;
            sense = !Extern.DeviceIoControl(fd, WindowsIoctl.IoctlSffdiskDeviceCommand, commandB,
                (uint) commandB.Length,
                commandB, (uint) commandB.Length, out _, IntPtr.Zero);
            var end = DateTime.Now;

            if (sense) error = Marshal.GetLastWin32Error();

            buffer = new byte[blockSize * blocks];
            Buffer.BlockCopy(commandB, commandB.Length - buffer.Length, buffer, 0, buffer.Length);

            response = new uint[4];
            duration = (end - start).TotalMilliseconds;

            return error;
        }
    }
}