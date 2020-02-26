// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : ListDevices.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Windows direct device access.
//
// --[ Description ] ----------------------------------------------------------
//
//     Gets a list of known physical devices.
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace DiscImageChef.Devices.Windows
{
    internal static class ListDevices
    {
        /// <summary>
        ///     Converts a hex dump string to the ASCII string it represents
        /// </summary>
        /// <param name="hex">Hex dump</param>
        /// <returns>Decoded string</returns>
        private static string HexStringToString(string hex)
        {
            var result = new StringBuilder();
            const string HEXTABLE = "0123456789abcdef";

            for (var i = 0; i < hex.Length / 2; i++)
                result.Append((char) (16 * HEXTABLE.IndexOf(hex[2 * i]) + HEXTABLE.IndexOf(hex[2 * i + 1])));

            return result.ToString();
        }

        /// <summary>
        ///     Gets a list of all known storage devices on Windows
        /// </summary>
        /// <returns>List of devices</returns>
        [SuppressMessage("ReSharper", "RedundantCatchClause")]
        internal static DeviceInfo[] GetList()
        {
            var deviceIDs = new List<string>();

            try
            {
                var mgmtObjSearcher =
                    new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
                var objCol = mgmtObjSearcher.Get();

                deviceIDs.AddRange(from ManagementObject drive in objCol select (string) drive["DeviceID"]);

                mgmtObjSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_TapeDrive");
                objCol = mgmtObjSearcher.Get();

                deviceIDs.AddRange(from ManagementObject drive in objCol select (string) drive["DeviceID"]);

                mgmtObjSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_CDROMDrive");
                objCol = mgmtObjSearcher.Get();

                deviceIDs.AddRange(from ManagementObject drive in objCol select (string) drive["Drive"]);
            }
            catch (Exception)
            {
#if DEBUG
                throw;
#else
                return null;
#endif
            }

            var devList = new List<DeviceInfo>();

            foreach (var devId in deviceIDs)
            {
                if (devId is null) continue;

                var physId = devId;
                // TODO: This can be done better
                if (devId.Length == 2 && devId[1] == ':') physId = "\\\\?\\" + devId;
                var fd = Extern.CreateFile(physId, 0, FileShare.Read | FileShare.Write, IntPtr.Zero,
                    FileMode.OpenExisting, 0, IntPtr.Zero);
                if (fd.IsInvalid) continue;

                var query = new StoragePropertyQuery
                {
                    PropertyId = StoragePropertyId.Device,
                    QueryType = StorageQueryType.Standard,
                    AdditionalParameters = new byte[1]
                };

                //StorageDeviceDescriptor descriptor = new StorageDeviceDescriptor();
                //descriptor.RawDeviceProperties = new byte[16384];

                var descriptorPtr = Marshal.AllocHGlobal(1000);
                var descriptorB = new byte[1000];

                uint returned = 0;
                var error = 0;

                var hasError = !Extern.DeviceIoControlStorageQuery(fd, WindowsIoctl.IoctlStorageQueryProperty,
                    ref query, (uint) Marshal.SizeOf(query),
                    descriptorPtr, 1000, ref returned, IntPtr.Zero);

                if (hasError) error = Marshal.GetLastWin32Error();

                Marshal.Copy(descriptorPtr, descriptorB, 0, 1000);

                if (hasError && error != 0) continue;

                var descriptor = new StorageDeviceDescriptor
                {
                    Version = BitConverter.ToUInt32(descriptorB, 0),
                    Size = BitConverter.ToUInt32(descriptorB, 4),
                    DeviceType = descriptorB[8],
                    DeviceTypeModifier = descriptorB[9],
                    RemovableMedia = BitConverter.ToBoolean(descriptorB, 10),
                    CommandQueueing = BitConverter.ToBoolean(descriptorB, 11),
                    VendorIdOffset = BitConverter.ToInt32(descriptorB, 12),
                    ProductIdOffset = BitConverter.ToInt32(descriptorB, 16),
                    ProductRevisionOffset = BitConverter.ToInt32(descriptorB, 20),
                    SerialNumberOffset = BitConverter.ToInt32(descriptorB, 24),
                    BusType = (StorageBusType) BitConverter.ToUInt32(descriptorB, 28),
                    RawPropertiesLength = BitConverter.ToUInt32(descriptorB, 32)
                };

                var info = new DeviceInfo {Path = physId, Bus = descriptor.BusType.ToString()};

                if (descriptor.VendorIdOffset > 0)
                    info.Vendor =
                        StringHandlers.CToString(descriptorB, Encoding.ASCII, start: descriptor.VendorIdOffset);
                if (descriptor.ProductIdOffset > 0)
                    info.Model =
                        StringHandlers.CToString(descriptorB, Encoding.ASCII, start: descriptor.ProductIdOffset);
                // TODO: Get serial number of SCSI and USB devices, probably also FireWire (untested)
                if (descriptor.SerialNumberOffset > 0)
                {
                    info.Serial =
                        StringHandlers.CToString(descriptorB, Encoding.ASCII, start: descriptor.SerialNumberOffset);

                    // fix any serial numbers that are returned as hex-strings
                    if (Array.TrueForAll(info.Serial.ToCharArray(), c => "0123456789abcdef".IndexOf(c) >= 0) &&
                        info.Serial.Length == 40) info.Serial = HexStringToString(info.Serial).Trim();
                }

                if ((string.IsNullOrEmpty(info.Vendor) || info.Vendor == "ATA") && info.Model != null)
                {
                    var pieces = info.Model.Split(' ');
                    if (pieces.Length > 1)
                    {
                        info.Vendor = pieces[0];
                        info.Model = info.Model.Substring(pieces[0].Length + 1);
                    }
                }

                switch (descriptor.BusType)
                {
                    case StorageBusType.SCSI:
                    case StorageBusType.ATAPI:
                    case StorageBusType.ATA:
                    case StorageBusType.FireWire:
                    case StorageBusType.SSA:
                    case StorageBusType.Fibre:
                    case StorageBusType.USB:
                    case StorageBusType.iSCSI:
                    case StorageBusType.SAS:
                    case StorageBusType.SATA:
                    case StorageBusType.SecureDigital:
                    case StorageBusType.MultiMediaCard:
                        info.Supported = true;
                        break;
                }

                Marshal.FreeHGlobal(descriptorPtr);
                devList.Add(info);
            }

            var devices = devList.ToArray();

            return devices;
        }
    }
}