// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Constructor.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Direct device access.
//
// --[ Description ] ----------------------------------------------------------
//
//     Prepares a device for direct access.
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interop;
using Aaru.CommonTypes.Structs.Devices.ATA;
using Aaru.CommonTypes.Structs.Devices.SCSI;
using Aaru.Decoders.SCSI;
using Aaru.Decoders.SCSI.MMC;
using Aaru.Decoders.SecureDigital;
using Aaru.Devices.FreeBSD;
using Aaru.Devices.Windows;
using Microsoft.Win32.SafeHandles;
using Extern = Aaru.Devices.Windows.Extern;
using FileAccess = Aaru.Devices.Windows.FileAccess;
using FileAttributes = Aaru.Devices.Windows.FileAttributes;
using FileFlags = Aaru.Devices.Linux.FileFlags;
using FileMode = Aaru.Devices.Windows.FileMode;
using FileShare = Aaru.Devices.Windows.FileShare;
using Inquiry = Aaru.CommonTypes.Structs.Devices.SCSI.Inquiry;
using PlatformID = Aaru.CommonTypes.Interop.PlatformID;
using VendorString = Aaru.Decoders.SecureDigital.VendorString;

namespace Aaru.Devices
{
    public partial class Device
    {
        /// <summary>Opens the device for sending direct commands</summary>
        /// <param name="devicePath">Device path</param>
        public Device(string devicePath)
        {
            PlatformId  = DetectOS.GetRealPlatformID();
            Timeout     = 15;
            Error       = false;
            IsRemovable = false;

            if(devicePath.StartsWith("dic://") ||
               devicePath.StartsWith("aaru://"))
            {
                if(devicePath.StartsWith("dic://"))
                    devicePath = devicePath.Substring(6);
                else
                    devicePath = devicePath.Substring(7);

                string[] pieces = devicePath.Split('/');
                string   host   = pieces[0];
                devicePath = devicePath.Substring(host.Length);

                _remote = new Remote.Remote(host);

                Error     = !_remote.Open(devicePath, out int errno);
                LastError = errno;
            }
            else
            {
                switch(PlatformId)
                {
                    case PlatformID.Win32NT:
                    {
                        FileHandle = Extern.CreateFile(devicePath, FileAccess.GenericRead | FileAccess.GenericWrite,
                                                       FileShare.Read | FileShare.Write, IntPtr.Zero,
                                                       FileMode.OpenExisting, FileAttributes.Normal, IntPtr.Zero);

                        if(((SafeFileHandle)FileHandle).IsInvalid)
                        {
                            Error     = true;
                            LastError = Marshal.GetLastWin32Error();
                        }

                        break;
                    }
                    case PlatformID.Linux:
                    {
                        FileHandle =
                            Linux.Extern.open(devicePath,
                                              FileFlags.ReadWrite | FileFlags.NonBlocking | FileFlags.CreateNew);

                        if((int)FileHandle < 0)
                        {
                            LastError = Marshal.GetLastWin32Error();

                            if(LastError == 13 ||
                               LastError == 30) // EACCES or EROFS
                            {
                                FileHandle = Linux.Extern.open(devicePath, FileFlags.Readonly | FileFlags.NonBlocking);

                                if((int)FileHandle < 0)
                                {
                                    Error     = true;
                                    LastError = Marshal.GetLastWin32Error();
                                }
                            }
                            else
                            {
                                Error = true;
                            }

                            LastError = Marshal.GetLastWin32Error();
                        }

                        break;
                    }
                    case PlatformID.FreeBSD:
                    {
                        FileHandle = FreeBSD.Extern.cam_open_device(devicePath, FreeBSD.FileFlags.ReadWrite);

                        if(((IntPtr)FileHandle).ToInt64() == 0)
                        {
                            Error     = true;
                            LastError = Marshal.GetLastWin32Error();
                        }

                        var camDevice = (CamDevice)Marshal.PtrToStructure((IntPtr)FileHandle, typeof(CamDevice));

                        if(StringHandlers.CToString(camDevice.SimName) == "ata")
                            throw new
                                DeviceException("Parallel ATA devices are not supported on FreeBSD due to upstream bug #224250.");

                        break;
                    }
                    default: throw new DeviceException($"Platform {PlatformId} not yet supported.");
                }
            }

            if(Error)
                throw new DeviceException(LastError);

            Type     = DeviceType.Unknown;
            ScsiType = PeripheralDeviceTypes.UnknownDevice;

            byte[] ataBuf;
            byte[] inqBuf = null;

            if(Error)
                throw new DeviceException(LastError);

            bool scsiSense = true;

            if(_remote is null)
            {
                // Windows is answering SCSI INQUIRY for all device types so it needs to be detected first
                switch(PlatformId)
                {
                    case PlatformID.Win32NT:
                        var query = new StoragePropertyQuery();
                        query.PropertyId           = StoragePropertyId.Device;
                        query.QueryType            = StorageQueryType.Standard;
                        query.AdditionalParameters = new byte[1];

                        IntPtr descriptorPtr = Marshal.AllocHGlobal(1000);
                        byte[] descriptorB   = new byte[1000];

                        uint returned = 0;
                        int  error    = 0;

                        bool hasError = !Extern.DeviceIoControlStorageQuery((SafeFileHandle)FileHandle,
                                                                            WindowsIoctl.IoctlStorageQueryProperty,
                                                                            ref query, (uint)Marshal.SizeOf(query),
                                                                            descriptorPtr, 1000, ref returned,
                                                                            IntPtr.Zero);

                        if(hasError)
                            error = Marshal.GetLastWin32Error();

                        Marshal.Copy(descriptorPtr, descriptorB, 0, 1000);

                        if(!hasError &&
                           error == 0)
                        {
                            var descriptor = new StorageDeviceDescriptor
                            {
                                Version               = BitConverter.ToUInt32(descriptorB, 0),
                                Size                  = BitConverter.ToUInt32(descriptorB, 4),
                                DeviceType            = descriptorB[8],
                                DeviceTypeModifier    = descriptorB[9],
                                RemovableMedia        = descriptorB[10] > 0,
                                CommandQueueing       = descriptorB[11] > 0,
                                VendorIdOffset        = BitConverter.ToInt32(descriptorB, 12),
                                ProductIdOffset       = BitConverter.ToInt32(descriptorB, 16),
                                ProductRevisionOffset = BitConverter.ToInt32(descriptorB, 20),
                                SerialNumberOffset    = BitConverter.ToInt32(descriptorB, 24),
                                BusType               = (StorageBusType)BitConverter.ToUInt32(descriptorB, 28),
                                RawPropertiesLength   = BitConverter.ToUInt32(descriptorB, 32)
                            };

                            descriptor.RawDeviceProperties = new byte[descriptor.RawPropertiesLength];

                            Array.Copy(descriptorB, 36, descriptor.RawDeviceProperties, 0,
                                       descriptor.RawPropertiesLength);

                            switch(descriptor.BusType)
                            {
                                case StorageBusType.SCSI:
                                case StorageBusType.SSA:
                                case StorageBusType.Fibre:
                                case StorageBusType.iSCSI:
                                case StorageBusType.SAS:
                                    Type = DeviceType.SCSI;

                                    break;
                                case StorageBusType.FireWire:
                                    IsFireWire = true;
                                    Type       = DeviceType.SCSI;

                                    break;
                                case StorageBusType.USB:
                                    IsUsb = true;
                                    Type  = DeviceType.SCSI;

                                    break;
                                case StorageBusType.ATAPI:
                                    Type = DeviceType.ATAPI;

                                    break;
                                case StorageBusType.ATA:
                                case StorageBusType.SATA:
                                    Type = DeviceType.ATA;

                                    break;
                                case StorageBusType.MultiMediaCard:
                                    Type = DeviceType.MMC;

                                    break;
                                case StorageBusType.SecureDigital:
                                    Type = DeviceType.SecureDigital;

                                    break;
                                case StorageBusType.NVMe:
                                    Type = DeviceType.NVMe;

                                    break;
                            }

                            switch(Type)
                            {
                                case DeviceType.SCSI:
                                case DeviceType.ATAPI:
                                    scsiSense = ScsiInquiry(out inqBuf, out _);

                                    break;
                                case DeviceType.ATA:
                                    bool atapiSense = AtapiIdentify(out ataBuf, out _);

                                    if(!atapiSense)
                                    {
                                        Type = DeviceType.ATAPI;
                                        Identify.IdentifyDevice? ataid = Identify.Decode(ataBuf);

                                        if(ataid.HasValue)
                                            scsiSense = ScsiInquiry(out inqBuf, out _);
                                    }
                                    else
                                    {
                                        Manufacturer = "ATA";
                                    }

                                    break;
                            }
                        }

                        Marshal.FreeHGlobal(descriptorPtr);

                        if(Windows.Command.IsSdhci((SafeFileHandle)FileHandle))
                        {
                            byte[] sdBuffer = new byte[16];

                            LastError = Windows.Command.SendMmcCommand((SafeFileHandle)FileHandle, MmcCommands.SendCsd,
                                                                       false, false,
                                                                       MmcFlags.ResponseSpiR2 | MmcFlags.ResponseR2 |
                                                                       MmcFlags.CommandAc, 0, 16, 1, ref sdBuffer,
                                                                       out _, out _, out bool sense);

                            if(!sense)
                            {
                                cachedCsd = new byte[16];
                                Array.Copy(sdBuffer, 0, cachedCsd, 0, 16);
                            }

                            sdBuffer = new byte[16];

                            LastError = Windows.Command.SendMmcCommand((SafeFileHandle)FileHandle, MmcCommands.SendCid,
                                                                       false, false,
                                                                       MmcFlags.ResponseSpiR2 | MmcFlags.ResponseR2 |
                                                                       MmcFlags.CommandAc, 0, 16, 1, ref sdBuffer,
                                                                       out _, out _, out sense);

                            if(!sense)
                            {
                                cachedCid = new byte[16];
                                Array.Copy(sdBuffer, 0, cachedCid, 0, 16);
                            }

                            sdBuffer = new byte[8];

                            LastError = Windows.Command.SendMmcCommand((SafeFileHandle)FileHandle,
                                                                       (MmcCommands)SecureDigitalCommands.SendScr,
                                                                       false, true,
                                                                       MmcFlags.ResponseSpiR1 | MmcFlags.ResponseR1 |
                                                                       MmcFlags.CommandAdtc, 0, 8, 1, ref sdBuffer,
                                                                       out _, out _, out sense);

                            if(!sense)
                            {
                                cachedScr = new byte[8];
                                Array.Copy(sdBuffer, 0, cachedScr, 0, 8);
                            }

                            sdBuffer = new byte[4];

                            LastError = Windows.Command.SendMmcCommand((SafeFileHandle)FileHandle,
                                                                       cachedScr != null
                                                                           ? (MmcCommands)SecureDigitalCommands.
                                                                               SendOperatingCondition
                                                                           : MmcCommands.SendOpCond, false, true,
                                                                       MmcFlags.ResponseSpiR3 | MmcFlags.ResponseR3 |
                                                                       MmcFlags.CommandBcr, 0, 4, 1, ref sdBuffer,
                                                                       out _, out _, out sense);

                            if(!sense)
                            {
                                cachedScr = new byte[4];
                                Array.Copy(sdBuffer, 0, cachedScr, 0, 4);
                            }
                        }

                        break;
                    case PlatformID.Linux:
                        if(devicePath.StartsWith("/dev/sd", StringComparison.Ordinal) ||
                           devicePath.StartsWith("/dev/sr", StringComparison.Ordinal) ||
                           devicePath.StartsWith("/dev/st", StringComparison.Ordinal) ||
                           devicePath.StartsWith("/dev/sg", StringComparison.Ordinal))
                        {
                            scsiSense = ScsiInquiry(out inqBuf, out _);
                        }

                        // MultiMediaCard and SecureDigital go here
                        else if(devicePath.StartsWith("/dev/mmcblk", StringComparison.Ordinal))
                        {
                            string devPath = devicePath.Substring(5);

                            if(File.Exists("/sys/block/" + devPath + "/device/csd"))
                            {
                                int len = ConvertFromHexAscii("/sys/block/" + devPath + "/device/csd", out cachedCsd);

                                if(len == 0)
                                    cachedCsd = null;
                            }

                            if(File.Exists("/sys/block/" + devPath + "/device/cid"))
                            {
                                int len = ConvertFromHexAscii("/sys/block/" + devPath + "/device/cid", out cachedCid);

                                if(len == 0)
                                    cachedCid = null;
                            }

                            if(File.Exists("/sys/block/" + devPath + "/device/scr"))
                            {
                                int len = ConvertFromHexAscii("/sys/block/" + devPath + "/device/scr", out cachedScr);

                                if(len == 0)
                                    cachedScr = null;
                            }

                            if(File.Exists("/sys/block/" + devPath + "/device/ocr"))
                            {
                                int len = ConvertFromHexAscii("/sys/block/" + devPath + "/device/ocr", out cachedOcr);

                                if(len == 0)
                                    cachedOcr = null;
                            }
                        }

                        break;
                    default:
                        scsiSense = ScsiInquiry(out inqBuf, out _);

                        break;
                }
            }
            else
            {
                Type = _remote.GetDeviceType();

                switch(Type)
                {
                    case DeviceType.ATAPI:
                    case DeviceType.SCSI:
                        scsiSense = ScsiInquiry(out inqBuf, out _);

                        break;
                    case DeviceType.SecureDigital:
                    case DeviceType.MMC:
                        if(!_remote.GetSdhciRegisters(out cachedCsd, out cachedCid, out cachedOcr, out cachedScr))
                        {
                            Type     = DeviceType.SCSI;
                            ScsiType = PeripheralDeviceTypes.DirectAccess;
                        }

                        break;
                }
            }

            #region SecureDigital / MultiMediaCard
            if(cachedCid != null)
            {
                ScsiType    = PeripheralDeviceTypes.DirectAccess;
                IsRemovable = false;

                if(cachedScr != null)
                {
                    Type = DeviceType.SecureDigital;
                    CID decoded = Decoders.SecureDigital.Decoders.DecodeCID(cachedCid);
                    Manufacturer = VendorString.Prettify(decoded.Manufacturer);
                    Model        = decoded.ProductName;

                    FirmwareRevision =
                        $"{(decoded.ProductRevision & 0xF0) >> 4:X2}.{decoded.ProductRevision & 0x0F:X2}";

                    Serial = $"{decoded.ProductSerialNumber}";
                }
                else
                {
                    Type = DeviceType.MMC;
                    Decoders.MMC.CID decoded = Decoders.MMC.Decoders.DecodeCID(cachedCid);
                    Manufacturer = Decoders.MMC.VendorString.Prettify(decoded.Manufacturer);
                    Model        = decoded.ProductName;

                    FirmwareRevision =
                        $"{(decoded.ProductRevision & 0xF0) >> 4:X2}.{decoded.ProductRevision & 0x0F:X2}";

                    Serial = $"{decoded.ProductSerialNumber}";
                }

                return;
            }
            #endregion SecureDigital / MultiMediaCard

            #region USB
            if(_remote is null)
            {
                switch(PlatformId)
                {
                    case PlatformID.Linux:
                        if(devicePath.StartsWith("/dev/sd", StringComparison.Ordinal) ||
                           devicePath.StartsWith("/dev/sr", StringComparison.Ordinal) ||
                           devicePath.StartsWith("/dev/st", StringComparison.Ordinal))
                        {
                            string devPath = devicePath.Substring(5);

                            if(Directory.Exists("/sys/block/" + devPath))
                            {
                                string resolvedLink = Linux.Command.ReadLink("/sys/block/" + devPath);

                                if(!string.IsNullOrEmpty(resolvedLink))
                                {
                                    resolvedLink = "/sys" + resolvedLink.Substring(2);

                                    while(resolvedLink.Contains("usb"))
                                    {
                                        resolvedLink = Path.GetDirectoryName(resolvedLink);

                                        if(!File.Exists(resolvedLink + "/descriptors") ||
                                           !File.Exists(resolvedLink + "/idProduct")   ||
                                           !File.Exists(resolvedLink + "/idVendor"))
                                            continue;

                                        var usbFs = new FileStream(resolvedLink + "/descriptors",
                                                                   System.IO.FileMode.Open, System.IO.FileAccess.Read);

                                        byte[] usbBuf   = new byte[65536];
                                        int    usbCount = usbFs.Read(usbBuf, 0, 65536);
                                        UsbDescriptors = new byte[usbCount];
                                        Array.Copy(usbBuf, 0, UsbDescriptors, 0, usbCount);
                                        usbFs.Close();

                                        var    usbSr   = new StreamReader(resolvedLink + "/idProduct");
                                        string usbTemp = usbSr.ReadToEnd();

                                        ushort.TryParse(usbTemp, NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                                                        out usbProduct);

                                        usbSr.Close();

                                        usbSr   = new StreamReader(resolvedLink + "/idVendor");
                                        usbTemp = usbSr.ReadToEnd();

                                        ushort.TryParse(usbTemp, NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                                                        out usbVendor);

                                        usbSr.Close();

                                        if(File.Exists(resolvedLink + "/manufacturer"))
                                        {
                                            usbSr                 = new StreamReader(resolvedLink + "/manufacturer");
                                            UsbManufacturerString = usbSr.ReadToEnd().Trim();
                                            usbSr.Close();
                                        }

                                        if(File.Exists(resolvedLink + "/product"))
                                        {
                                            usbSr            = new StreamReader(resolvedLink + "/product");
                                            UsbProductString = usbSr.ReadToEnd().Trim();
                                            usbSr.Close();
                                        }

                                        if(File.Exists(resolvedLink + "/serial"))
                                        {
                                            usbSr           = new StreamReader(resolvedLink + "/serial");
                                            UsbSerialString = usbSr.ReadToEnd().Trim();
                                            usbSr.Close();
                                        }

                                        IsUsb = true;

                                        break;
                                    }
                                }
                            }
                        }

                        break;
                    case PlatformID.Win32NT:
                        Usb.UsbDevice usbDevice = null;

                        // I have to search for USB disks, floppies and CD-ROMs as separate device types
                        foreach(string devGuid in new[]
                        {
                            Usb.GuidDevinterfaceFloppy, Usb.GuidDevinterfaceCdrom, Usb.GuidDevinterfaceDisk,
                            Usb.GuidDevinterfaceTape
                        })
                        {
                            usbDevice = Usb.FindDrivePath(devicePath, devGuid);

                            if(usbDevice != null)
                                break;
                        }

                        if(usbDevice != null)
                        {
                            UsbDescriptors        = usbDevice.BinaryDescriptors;
                            usbVendor             = (ushort)usbDevice.DeviceDescriptor.idVendor;
                            usbProduct            = (ushort)usbDevice.DeviceDescriptor.idProduct;
                            UsbManufacturerString = usbDevice.Manufacturer;
                            UsbProductString      = usbDevice.Product;

                            UsbSerialString =
                                usbDevice.
                                    SerialNumber; // This is incorrect filled by Windows with SCSI/ATA serial number
                        }

                        break;
                    default:
                        IsUsb = false;

                        break;
                }
            }
            else
            {
                if(_remote.GetUsbData(out byte[] remoteUsbDescriptors, out ushort remoteUsbVendor,
                                      out ushort remoteUsbProduct, out string remoteUsbManufacturer,
                                      out string remoteUsbProductString, out string remoteUsbSerial))
                {
                    IsUsb                 = true;
                    UsbDescriptors        = remoteUsbDescriptors;
                    usbVendor             = remoteUsbVendor;
                    usbProduct            = remoteUsbProduct;
                    UsbManufacturerString = remoteUsbManufacturer;
                    UsbProductString      = remoteUsbProductString;
                    UsbSerialString       = remoteUsbSerial;
                }
            }
            #endregion USB

            #region FireWire
            if(!(_remote is null))
            {
                if(_remote.GetFireWireData(out firewireVendor, out firewireModel, out firewireGuid,
                                           out string remoteFireWireVendorName, out string remoteFireWireModelName))
                {
                    IsFireWire         = true;
                    FireWireVendorName = remoteFireWireVendorName;
                    FireWireModelName  = remoteFireWireModelName;
                }
            }
            else
            {
                if(PlatformId == PlatformID.Linux)
                {
                    if(devicePath.StartsWith("/dev/sd", StringComparison.Ordinal) ||
                       devicePath.StartsWith("/dev/sr", StringComparison.Ordinal) ||
                       devicePath.StartsWith("/dev/st", StringComparison.Ordinal))
                    {
                        string devPath = devicePath.Substring(5);

                        if(Directory.Exists("/sys/block/" + devPath))
                        {
                            string resolvedLink = Linux.Command.ReadLink("/sys/block/" + devPath);
                            resolvedLink = "/sys" + resolvedLink.Substring(2);

                            if(!string.IsNullOrEmpty(resolvedLink))
                                while(resolvedLink.Contains("firewire"))
                                {
                                    resolvedLink = Path.GetDirectoryName(resolvedLink);

                                    if(!File.Exists(resolvedLink + "/model")  ||
                                       !File.Exists(resolvedLink + "/vendor") ||
                                       !File.Exists(resolvedLink + "/guid"))
                                        continue;

                                    var    fwSr   = new StreamReader(resolvedLink + "/model");
                                    string fwTemp = fwSr.ReadToEnd();

                                    uint.TryParse(fwTemp, NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                                                  out firewireModel);

                                    fwSr.Close();

                                    fwSr   = new StreamReader(resolvedLink + "/vendor");
                                    fwTemp = fwSr.ReadToEnd();

                                    uint.TryParse(fwTemp, NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                                                  out firewireVendor);

                                    fwSr.Close();

                                    fwSr   = new StreamReader(resolvedLink + "/guid");
                                    fwTemp = fwSr.ReadToEnd();

                                    ulong.TryParse(fwTemp, NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                                                   out firewireGuid);

                                    fwSr.Close();

                                    if(File.Exists(resolvedLink + "/model_name"))
                                    {
                                        fwSr              = new StreamReader(resolvedLink + "/model_name");
                                        FireWireModelName = fwSr.ReadToEnd().Trim();
                                        fwSr.Close();
                                    }

                                    if(File.Exists(resolvedLink + "/vendor_name"))
                                    {
                                        fwSr               = new StreamReader(resolvedLink + "/vendor_name");
                                        FireWireVendorName = fwSr.ReadToEnd().Trim();
                                        fwSr.Close();
                                    }

                                    IsFireWire = true;

                                    break;
                                }
                        }
                    }
                }

                // TODO: Implement for other operating systems
                else
                {
                    IsFireWire = false;
                }
            }
            #endregion FireWire

            #region PCMCIA
            if(_remote is null)
            {
                if(PlatformId == PlatformID.Linux)
                {
                    if(devicePath.StartsWith("/dev/sd", StringComparison.Ordinal) ||
                       devicePath.StartsWith("/dev/sr", StringComparison.Ordinal) ||
                       devicePath.StartsWith("/dev/st", StringComparison.Ordinal))
                    {
                        string devPath = devicePath.Substring(5);

                        if(Directory.Exists("/sys/block/" + devPath))
                        {
                            string resolvedLink = Linux.Command.ReadLink("/sys/block/" + devPath);
                            resolvedLink = "/sys" + resolvedLink.Substring(2);

                            if(!string.IsNullOrEmpty(resolvedLink))
                                while(resolvedLink.Contains("/sys/devices"))
                                {
                                    resolvedLink = Path.GetDirectoryName(resolvedLink);

                                    if(!Directory.Exists(resolvedLink + "/pcmcia_socket"))
                                        continue;

                                    string[] subdirs = Directory.GetDirectories(resolvedLink + "/pcmcia_socket",
                                                                                "pcmcia_socket*",
                                                                                SearchOption.TopDirectoryOnly);

                                    if(subdirs.Length <= 0)
                                        continue;

                                    string possibleDir = Path.Combine(resolvedLink, "pcmcia_socket", subdirs[0]);

                                    if(!File.Exists(possibleDir + "/card_type") ||
                                       !File.Exists(possibleDir + "/cis"))
                                        continue;

                                    var cisFs = new FileStream(possibleDir + "/cis", System.IO.FileMode.Open,
                                                               System.IO.FileAccess.Read);

                                    byte[] cisBuf   = new byte[65536];
                                    int    cisCount = cisFs.Read(cisBuf, 0, 65536);
                                    Cis = new byte[cisCount];
                                    Array.Copy(cisBuf, 0, Cis, 0, cisCount);
                                    cisFs.Close();

                                    IsPcmcia = true;

                                    break;
                                }
                        }
                    }
                }

                // TODO: Implement for other operating systems
                else
                {
                    IsPcmcia = false;
                }
            }
            else
            {
                if(_remote.GetPcmciaData(out byte[] cisBuf))
                {
                    IsPcmcia = true;
                    Cis      = cisBuf;
                }
            }
            #endregion PCMCIA

            if(!scsiSense)
            {
                Inquiry? inquiry = Inquiry.Decode(inqBuf);

                Type = DeviceType.SCSI;
                bool serialSense = ScsiInquiry(out inqBuf, out _, 0x80);

                if(!serialSense)
                    Serial = EVPD.DecodePage80(inqBuf);

                if(inquiry.HasValue)
                {
                    string tmp = StringHandlers.CToString(inquiry.Value.ProductRevisionLevel);

                    if(tmp != null)
                        FirmwareRevision = tmp.Trim();

                    tmp = StringHandlers.CToString(inquiry.Value.ProductIdentification);

                    if(tmp != null)
                        Model = tmp.Trim();

                    tmp = StringHandlers.CToString(inquiry.Value.VendorIdentification);

                    if(tmp != null)
                        Manufacturer = tmp.Trim();

                    IsRemovable = inquiry.Value.RMB;

                    ScsiType = (PeripheralDeviceTypes)inquiry.Value.PeripheralDeviceType;
                }

                bool atapiSense = AtapiIdentify(out ataBuf, out _);

                if(!atapiSense)
                {
                    Type = DeviceType.ATAPI;
                    Identify.IdentifyDevice? ataId = Identify.Decode(ataBuf);

                    if(ataId.HasValue)
                        Serial = ataId.Value.SerialNumber;
                }

                LastError = 0;
                Error     = false;
            }

            if((scsiSense && !(IsUsb || IsFireWire)) ||
               Manufacturer == "ATA")
            {
                bool ataSense = AtaIdentify(out ataBuf, out _);

                if(!ataSense)
                {
                    Type = DeviceType.ATA;
                    Identify.IdentifyDevice? ataid = Identify.Decode(ataBuf);

                    if(ataid.HasValue)
                    {
                        string[] separated = ataid.Value.Model.Split(' ');

                        if(separated.Length == 1)
                        {
                            Model = separated[0];
                        }
                        else
                        {
                            Manufacturer = separated[0];
                            Model        = separated[^1];
                        }

                        FirmwareRevision = ataid.Value.FirmwareRevision;
                        Serial           = ataid.Value.SerialNumber;

                        ScsiType = PeripheralDeviceTypes.DirectAccess;

                        if((ushort)ataid.Value.GeneralConfiguration != 0x848A)
                            IsRemovable |=
                                (ataid.Value.GeneralConfiguration & Identify.GeneralConfigurationBit.Removable) ==
                                Identify.GeneralConfigurationBit.Removable;
                        else
                            IsCompactFlash = true;
                    }
                }
            }

            if(Type == DeviceType.Unknown)
            {
                Manufacturer     = null;
                Model            = null;
                FirmwareRevision = null;
                Serial           = null;
            }

            if(IsUsb)
            {
                if(string.IsNullOrEmpty(Manufacturer))
                    Manufacturer = UsbManufacturerString;

                if(string.IsNullOrEmpty(Model))
                    Model = UsbProductString;

                if(string.IsNullOrEmpty(Serial))
                    Serial = UsbSerialString;
                else
                    foreach(char c in Serial.Where(char.IsControl))
                        Serial = UsbSerialString;
            }

            if(IsFireWire)
            {
                if(string.IsNullOrEmpty(Manufacturer))
                    Manufacturer = FireWireVendorName;

                if(string.IsNullOrEmpty(Model))
                    Model = FireWireModelName;

                if(string.IsNullOrEmpty(Serial))
                    Serial = $"{firewireGuid:X16}";
                else
                    foreach(char c in Serial.Where(char.IsControl))
                        Serial = $"{firewireGuid:X16}";
            }

            // Some optical drives are not getting the correct serial, and IDENTIFY PACKET DEVICE is blocked without
            // administrator privileges
            if(ScsiType != PeripheralDeviceTypes.MultiMediaDevice)
                return;

            bool featureSense = GetConfiguration(out byte[] featureBuffer, out _, 0x0108, MmcGetConfigurationRt.Single,
                                                 Timeout, out _);

            if(featureSense)
                return;

            Features.SeparatedFeatures features = Features.Separate(featureBuffer);

            if(features.Descriptors?.Length != 1 ||
               features.Descriptors[0].Code != 0x0108)
                return;

            Feature_0108? serialFeature = Features.Decode_0108(features.Descriptors[0].Data);

            if(serialFeature is null)
                return;

            Serial = serialFeature.Value.Serial;
        }

        static int ConvertFromHexAscii(string file, out byte[] outBuf)
        {
            var    sr  = new StreamReader(file);
            string ins = sr.ReadToEnd().Trim();

            if(ins.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
                ins = ins.Substring(2);

            outBuf = new byte[ins.Length / 2];
            int count = 0;

            try
            {
                for(int i = 0; i < ins.Length; i += 2)
                {
                    outBuf[i / 2] = Convert.ToByte(ins.Substring(i, 2), 16);
                    count++;
                }
            }
            catch
            {
                count = 0;
            }

            sr.Close();

            return count;
        }
    }
}