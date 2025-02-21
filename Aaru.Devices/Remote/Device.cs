// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Device.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Remote device access.
//
// --[ Description ] ----------------------------------------------------------
//
//     Prepares a remote device for direct access.
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
// Copyright © 2011-2024 Natalia Portillo
// ****************************************************************************/

using System;
using System.Net.Sockets;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interop;
using Aaru.CommonTypes.Structs.Devices.SCSI;
using Aaru.Decoders.SecureDigital;

namespace Aaru.Devices.Remote;

/// <inheritdoc />
public sealed partial class Device : Devices.Device
{
    bool?  _isRemoteAdmin;
    Remote _remote;

    Device() {}

    /// <summary>Returns if remote is running under administrative (aka root) privileges</summary>
    public bool IsAdmin
    {
        get
        {
            _isRemoteAdmin ??= _remote.IsRoot;

            return _isRemoteAdmin == true;
        }
    }

    /// <summary>Current device is remote</summary>

    // ReSharper disable once UnusedMember.Global
    public bool IsRemote => _remote != null;

    /// <summary>Remote application</summary>
    public string RemoteApplication => _remote?.ServerApplication;

    /// <summary>Remote application server</summary>
    public string RemoteVersion => _remote?.ServerVersion;

    /// <summary>Remote operating system name</summary>
    public string RemoteOperatingSystem => _remote?.ServerOperatingSystem;

    /// <summary>Remote operating system version</summary>
    public string RemoteOperatingSystemVersion => _remote?.ServerOperatingSystemVersion;

    /// <summary>Remote architecture</summary>
    public string RemoteArchitecture => _remote?.ServerArchitecture;

    /// <summary>Remote protocol version</summary>
    public int RemoteProtocolVersion => _remote?.ServerProtocolVersion ?? 0;

    /// <summary>Opens the device for sending direct commands</summary>
    /// <param name="aaruUri">AaruRemote URI</param>
    /// <param name="errno">Sets the error if a device cannot be opened</param>
    /// <returns>Device</returns>
    internal static Device Create(Uri aaruUri, out ErrorNumber errno)
    {
        errno = ErrorNumber.NoError;

        var dev = new Device
        {
            PlatformId  = DetectOS.GetRealPlatformID(),
            Timeout     = 15,
            Error       = false,
            IsRemovable = false
        };

        if(aaruUri.Scheme is not ("dic" or "aaru")) return null;

        string devicePath = aaruUri.AbsolutePath;

        if(devicePath.StartsWith('/')) devicePath = devicePath[1..];

        if(devicePath.StartsWith("dev", StringComparison.Ordinal)) devicePath = $"/{devicePath}";

        dev.DevicePath = devicePath;

        try
        {
            dev._remote = new Remote(aaruUri);
        }
        catch(Exception ex)
        {
            if(ex is SocketException sockEx)
                errno = (ErrorNumber)(-1 * sockEx.ErrorCode);
            else
                errno = ErrorNumber.NoSuchDeviceOrAddress;

            return null;
        }

        dev.Error     = !dev._remote.Open(devicePath, out int remoteErrno);
        dev.LastError = remoteErrno;

        // TODO: Convert error codes
        if(dev.Error)
        {
            errno = (ErrorNumber)remoteErrno;

            return null;
        }

        if(dev._remote.ServerOperatingSystem == "Linux") _readMultipleBlockCannotSetBlockCount = true;

        dev.Type     = DeviceType.Unknown;
        dev.ScsiType = PeripheralDeviceTypes.UnknownDevice;

        if(dev.Error)
        {
            errno = (ErrorNumber)dev.LastError;

            return null;
        }

        dev.Type = dev._remote.GetDeviceType();

        switch(dev.Type)
        {
            case DeviceType.SecureDigital:
            case DeviceType.MMC:
                if(!dev._remote.GetSdhciRegisters(out dev.CachedCsd,
                                                  out dev.CachedCid,
                                                  out dev.CachedOcr,
                                                  out dev.CachedScr))
                {
                    dev.Type     = DeviceType.SCSI;
                    dev.ScsiType = PeripheralDeviceTypes.DirectAccess;
                }

                break;
        }

#region SecureDigital / MultiMediaCard

        if(dev.CachedCid != null)
        {
            dev.ScsiType    = PeripheralDeviceTypes.DirectAccess;
            dev.IsRemovable = false;

            if(dev.CachedScr != null)
            {
                dev.Type = DeviceType.SecureDigital;
                CID decoded = Decoders.SecureDigital.Decoders.DecodeCID(dev.CachedCid);
                dev.Manufacturer = VendorString.Prettify(decoded.Manufacturer);
                dev.Model        = decoded.ProductName;

                dev.FirmwareRevision =
                    $"{(decoded.ProductRevision & 0xF0) >> 4:X2}.{decoded.ProductRevision & 0x0F:X2}";

                dev.Serial = $"{decoded.ProductSerialNumber}";
            }
            else
            {
                dev.Type = DeviceType.MMC;
                Decoders.MMC.CID decoded = Decoders.MMC.Decoders.DecodeCID(dev.CachedCid);
                dev.Manufacturer = Decoders.MMC.VendorString.Prettify(decoded.Manufacturer);
                dev.Model        = decoded.ProductName;

                dev.FirmwareRevision =
                    $"{(decoded.ProductRevision & 0xF0) >> 4:X2}.{decoded.ProductRevision & 0x0F:X2}";

                dev.Serial = $"{decoded.ProductSerialNumber}";
            }

            return dev;
        }

#endregion SecureDigital / MultiMediaCard

#region USB

        if(dev._remote.GetUsbData(out byte[] remoteUsbDescriptors,
                                  out ushort remoteUsbVendor,
                                  out ushort remoteUsbProduct,
                                  out string remoteUsbManufacturer,
                                  out string remoteUsbProductString,
                                  out string remoteUsbSerial))
        {
            dev.IsUsb                 = true;
            dev.UsbDescriptors        = remoteUsbDescriptors;
            dev.UsbVendor             = remoteUsbVendor;
            dev.UsbProduct            = remoteUsbProduct;
            dev.UsbManufacturerString = remoteUsbManufacturer;
            dev.UsbProductString      = remoteUsbProductString;
            dev.UsbSerialString       = remoteUsbSerial;
        }

#endregion USB

#region FireWire

        if(dev._remote.GetFireWireData(out dev.FirewireVendor,
                                       out dev.FirewireModel,
                                       out dev.FirewireGuid,
                                       out string remoteFireWireVendorName,
                                       out string remoteFireWireModelName))
        {
            dev.IsFireWire         = true;
            dev.FireWireVendorName = remoteFireWireVendorName;
            dev.FireWireModelName  = remoteFireWireModelName;
        }

#endregion FireWire

#region PCMCIA

        if(!dev._remote.GetPcmciaData(out byte[] cisBuf)) return dev;

        dev.IsPcmcia = true;
        dev.Cis      = cisBuf;

#endregion PCMCIA

        return dev;
    }

    /// <inheritdoc />
    public override void Close()
    {
        if(_remote == null) return;

        _remote.Close();
        _remote.Disconnect();
    }
}