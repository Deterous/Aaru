﻿// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Helpers.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains helpers for CPCEMU disk images.
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

using Aaru.Decoders.Floppy;

namespace Aaru.Images;

public sealed partial class Cpcdsk
{
    static int SizeCodeToBytes(IBMSectorSizeCode code) => code switch
                                                          {
                                                              IBMSectorSizeCode.EighthKilo       => 128,
                                                              IBMSectorSizeCode.QuarterKilo      => 256,
                                                              IBMSectorSizeCode.HalfKilo         => 512,
                                                              IBMSectorSizeCode.Kilo             => 1024,
                                                              IBMSectorSizeCode.TwiceKilo        => 2048,
                                                              IBMSectorSizeCode.FriceKilo        => 4096,
                                                              IBMSectorSizeCode.TwiceFriceKilo   => 8192,
                                                              IBMSectorSizeCode.FricelyFriceKilo => 16384,
                                                              _                                  => 0
                                                          };
}