// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : SdMmcInfoViewModel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI view models.
//
// --[ Description ] ----------------------------------------------------------
//
//     View model and code for the SecureDigital / MultiMediaCard information tab.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General public License for more details.
//
//     You should have received a copy of the GNU General public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2020 Natalia Portillo
// ****************************************************************************/

using Aaru.CommonTypes.Enums;

namespace Aaru.Gui.ViewModels.Tabs
{
    public class SdMmcInfoViewModel
    {
        public SdMmcInfoViewModel(DeviceType deviceType, byte[] cid, byte[] csd, byte[] ocr, byte[] extendedCsd,
                                  byte[] scr)
        {
            switch(deviceType)
            {
                case DeviceType.MMC:
                {
                    //Text = "MultiMediaCard";

                    if(cid != null)
                        CidText = Decoders.MMC.Decoders.PrettifyCID(cid);

                    if(csd != null)
                        CsdText = Decoders.MMC.Decoders.PrettifyCSD(csd);

                    if(ocr != null)
                        OcrText = Decoders.MMC.Decoders.PrettifyOCR(ocr);

                    if(extendedCsd != null)
                        ExtendedCsdText = Decoders.MMC.Decoders.PrettifyExtendedCSD(extendedCsd);
                }

                    break;
                case DeviceType.SecureDigital:
                {
                    //Text = "SecureDigital";

                    if(cid != null)
                        CidText = Decoders.SecureDigital.Decoders.PrettifyCID(cid);

                    if(csd != null)
                        CsdText = Decoders.SecureDigital.Decoders.PrettifyCSD(csd);

                    if(ocr != null)
                        OcrText = Decoders.SecureDigital.Decoders.PrettifyOCR(ocr);

                    if(scr != null)
                        ScrText = Decoders.SecureDigital.Decoders.PrettifySCR(scr);
                }

                    break;
            }
        }

        public string CidText         { get; }
        public string CsdText         { get; }
        public string OcrText         { get; }
        public string ExtendedCsdText { get; }
        public string ScrText         { get; }
    }
}