// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : DeviceReport.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Core algorithms.
//
// --[ Description ] ----------------------------------------------------------
//
//     Creates reports from devices.
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

using Aaru.Devices;

namespace Aaru.Core.Devices.Report;

public sealed partial class DeviceReport
{
    const    string GDROM_MODULE_NAME = "GD-ROM reporter";
    const    string ATA_MODULE_NAME   = "ATA Report";
    const    string SCSI_MODULE_NAME  = "SCSI Report";
    readonly Device _dev;

    /// <summary>Initializes a device report for the specified device (must be opened)</summary>
    /// <param name="device">Device</param>
    public DeviceReport(Device device) => _dev = device;
}