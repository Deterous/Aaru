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
//     Contains helpers for Aaru Format disk images.
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
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs.Devices.ATA;
using Aaru.CommonTypes.Structs.Devices.SCSI;
using Aaru.Decoders.SecureDigital;
using Aaru.Helpers;

namespace Aaru.Images;

public sealed partial class AaruFormat
{
    /// <summary>Checks for media tags that may contain metadata and sets it up if not already set</summary>
    void SetMetadataFromTags()
    {
        // Search for SecureDigital CID
        if(_mediaTags.TryGetValue(MediaTagType.SD_CID, out byte[] sdCid))
        {
            CID decoded = Decoders.SecureDigital.Decoders.DecodeCID(sdCid);

            if(string.IsNullOrWhiteSpace(_imageInfo.DriveManufacturer))
                _imageInfo.DriveManufacturer = VendorString.Prettify(decoded.Manufacturer);

            if(string.IsNullOrWhiteSpace(_imageInfo.DriveModel)) _imageInfo.DriveModel = decoded.ProductName;

            if(string.IsNullOrWhiteSpace(_imageInfo.DriveFirmwareRevision))
            {
                _imageInfo.DriveFirmwareRevision =
                    $"{(decoded.ProductRevision & 0xF0) >> 4:X2}.{decoded.ProductRevision & 0x0F:X2}";
            }

            if(string.IsNullOrWhiteSpace(_imageInfo.DriveSerialNumber))
                _imageInfo.DriveSerialNumber = $"{decoded.ProductSerialNumber}";
        }

        // Search for MultiMediaCard CID
        if(_mediaTags.TryGetValue(MediaTagType.MMC_CID, out byte[] mmcCid))
        {
            Decoders.MMC.CID decoded = Decoders.MMC.Decoders.DecodeCID(mmcCid);

            if(string.IsNullOrWhiteSpace(_imageInfo.DriveManufacturer))
                _imageInfo.DriveManufacturer = Decoders.MMC.VendorString.Prettify(decoded.Manufacturer);

            if(string.IsNullOrWhiteSpace(_imageInfo.DriveModel)) _imageInfo.DriveModel = decoded.ProductName;

            if(string.IsNullOrWhiteSpace(_imageInfo.DriveFirmwareRevision))
            {
                _imageInfo.DriveFirmwareRevision =
                    $"{(decoded.ProductRevision & 0xF0) >> 4:X2}.{decoded.ProductRevision & 0x0F:X2}";
            }

            if(string.IsNullOrWhiteSpace(_imageInfo.DriveSerialNumber))
                _imageInfo.DriveSerialNumber = $"{decoded.ProductSerialNumber}";
        }

        // Search for SCSI INQUIRY
        if(_mediaTags.TryGetValue(MediaTagType.SCSI_INQUIRY, out byte[] scsiInquiry))
        {
            Inquiry? nullableInquiry = Inquiry.Decode(scsiInquiry);

            if(nullableInquiry.HasValue)
            {
                Inquiry inquiry = nullableInquiry.Value;

                if(string.IsNullOrWhiteSpace(_imageInfo.DriveManufacturer))
                    _imageInfo.DriveManufacturer = StringHandlers.CToString(inquiry.VendorIdentification)?.Trim();

                if(string.IsNullOrWhiteSpace(_imageInfo.DriveModel))
                    _imageInfo.DriveModel = StringHandlers.CToString(inquiry.ProductIdentification)?.Trim();

                if(string.IsNullOrWhiteSpace(_imageInfo.DriveFirmwareRevision))
                    _imageInfo.DriveFirmwareRevision = StringHandlers.CToString(inquiry.ProductRevisionLevel)?.Trim();
            }
        }

        // Search for ATA or ATAPI IDENTIFY
        if(!_mediaTags.TryGetValue(MediaTagType.ATA_IDENTIFY,   out byte[] ataIdentify) &&
           !_mediaTags.TryGetValue(MediaTagType.ATAPI_IDENTIFY, out ataIdentify))
            return;

        Identify.IdentifyDevice? nullableIdentify = CommonTypes.Structs.Devices.ATA.Identify.Decode(ataIdentify);

        if(!nullableIdentify.HasValue) return;

        Identify.IdentifyDevice identify = nullableIdentify.Value;

        string[] separated = identify.Model.Split(' ');

        if(separated.Length == 1)
        {
            if(string.IsNullOrWhiteSpace(_imageInfo.DriveModel))
                _imageInfo.DriveModel = separated[0];
            else
            {
                if(string.IsNullOrWhiteSpace(_imageInfo.DriveManufacturer)) _imageInfo.DriveManufacturer = separated[0];

                if(string.IsNullOrWhiteSpace(_imageInfo.DriveModel)) _imageInfo.DriveModel = separated[^1];
            }
        }

        if(string.IsNullOrWhiteSpace(_imageInfo.DriveFirmwareRevision))
            _imageInfo.DriveFirmwareRevision = identify.FirmwareRevision;

        if(string.IsNullOrWhiteSpace(_imageInfo.DriveSerialNumber))
            _imageInfo.DriveSerialNumber = identify.SerialNumber;
    }

    // Get the Aaru Metadata media type from Aaru media type
    static MetadataMediaType GetMetadataMediaType(MediaType type)
    {
        return type switch
               {
                   MediaType.CD
                    or MediaType.CDDA
                    or MediaType.CDG
                    or MediaType.CDEG
                    or MediaType.CDI
                    or MediaType.CDIREADY
                    or MediaType.CDROM
                    or MediaType.CDROMXA
                    or MediaType.CDPLUS
                    or MediaType.CDMO
                    or MediaType.CDR
                    or MediaType.CDRW
                    or MediaType.CDMRW
                    or MediaType.VCD
                    or MediaType.SVCD
                    or MediaType.PCD
                    or MediaType.SACD
                    or MediaType.DDCD
                    or MediaType.DDCDR
                    or MediaType.DDCDRW
                    or MediaType.DTSCD
                    or MediaType.CDMIDI
                    or MediaType.CDV
                    or MediaType.DVDROM
                    or MediaType.DVDR
                    or MediaType.DVDRW
                    or MediaType.DVDPR
                    or MediaType.DVDPRW
                    or MediaType.DVDPRWDL
                    or MediaType.DVDRDL
                    or MediaType.DVDPRDL
                    or MediaType.DVDRAM
                    or MediaType.DVDRWDL
                    or MediaType.DVDDownload
                    or MediaType.HDDVDROM
                    or MediaType.HDDVDRAM
                    or MediaType.HDDVDR
                    or MediaType.HDDVDRW
                    or MediaType.HDDVDRDL
                    or MediaType.HDDVDRWDL
                    or MediaType.BDROM
                    or MediaType.UHDBD
                    or MediaType.BDR
                    or MediaType.BDRE
                    or MediaType.BDRXL
                    or MediaType.BDREXL
                    or MediaType.EVD
                    or MediaType.FVD
                    or MediaType.HVD
                    or MediaType.CBHD
                    or MediaType.HDVMD
                    or MediaType.VCDHD
                    or MediaType.SVOD
                    or MediaType.FDDVD
                    or MediaType.LD
                    or MediaType.LDROM
                    or MediaType.CRVdisc
                    or MediaType.LDROM2
                    or MediaType.LVROM
                    or MediaType.MegaLD
                    or MediaType.PS1CD
                    or MediaType.PS2CD
                    or MediaType.PS2DVD
                    or MediaType.PS3DVD
                    or MediaType.PS3BD
                    or MediaType.PS4BD
                    or MediaType.PS5BD
                    or MediaType.UMD
                    or MediaType.XGD
                    or MediaType.XGD2
                    or MediaType.XGD3
                    or MediaType.XGD4
                    or MediaType.MEGACD
                    or MediaType.SATURNCD
                    or MediaType.GDROM
                    or MediaType.GDR
                    or MediaType.SuperCDROM2
                    or MediaType.JaguarCD
                    or MediaType.ThreeDO
                    or MediaType.PCFX
                    or MediaType.NeoGeoCD
                    or MediaType.GOD
                    or MediaType.WOD
                    or MediaType.WUOD
                    or MediaType.CDTV
                    or MediaType.CD32
                    or MediaType.Nuon
                    or MediaType.Playdia
                    or MediaType.Pippin
                    or MediaType.FMTOWNS
                    or MediaType.MilCD
                    or MediaType.VideoNow
                    or MediaType.VideoNowColor
                    or MediaType.VideoNowXp
                    or MediaType.CVD => MetadataMediaType.OpticalDisc,
                   _ => MetadataMediaType.BlockMedia
               };
    }

    // Gets a DDT entry
    ulong GetDdtEntry(ulong sectorAddress)
    {
        if(_inMemoryDdt) return _userDataDdt[sectorAddress];

        if(_ddtEntryCache.TryGetValue(sectorAddress, out ulong entry)) return entry;

        long oldPosition = _imageStream.Position;
        _imageStream.Position =  _outMemoryDdtPosition + Marshal.SizeOf<DdtHeader>();
        _imageStream.Position += (long)(sectorAddress * sizeof(ulong));
        var temp = new byte[sizeof(ulong)];
        _imageStream.EnsureRead(temp, 0, sizeof(ulong));
        _imageStream.Position = oldPosition;
        entry                 = BitConverter.ToUInt64(temp, 0);

        if(_ddtEntryCache.Count >= MAX_DDT_ENTRY_CACHE) _ddtEntryCache.Clear();

        _ddtEntryCache.Add(sectorAddress, entry);

        return entry;
    }

    // Sets a DDT entry
    void SetDdtEntry(ulong sectorAddress, ulong pointer)
    {
        if(_inMemoryDdt)
        {
            if(IsTape)
                _tapeDdt[sectorAddress] = pointer;
            else
                _userDataDdt[sectorAddress] = pointer;

            return;
        }

        long oldPosition = _imageStream.Position;
        _imageStream.Position =  _outMemoryDdtPosition + Marshal.SizeOf<DdtHeader>();
        _imageStream.Position += (long)(sectorAddress * sizeof(ulong));
        _imageStream.Write(BitConverter.GetBytes(pointer), 0, sizeof(ulong));
        _imageStream.Position = oldPosition;
    }

    // Converts between image data type and Aaru media tag type
    static MediaTagType GetMediaTagTypeForDataType(DataType type) => type switch
                                                                     {
                                                                         DataType.CompactDiscPartialToc => MediaTagType
                                                                            .CD_TOC,
                                                                         DataType.CompactDiscSessionInfo => MediaTagType
                                                                            .CD_SessionInfo,
                                                                         DataType.CompactDiscToc => MediaTagType
                                                                            .CD_FullTOC,
                                                                         DataType.CompactDiscPma => MediaTagType.CD_PMA,
                                                                         DataType.CompactDiscAtip => MediaTagType
                                                                            .CD_ATIP,
                                                                         DataType.CompactDiscLeadInCdText =>
                                                                             MediaTagType.CD_TEXT,
                                                                         DataType.DvdPfi       => MediaTagType.DVD_PFI,
                                                                         DataType.DvdLeadInCmi => MediaTagType.DVD_CMI,
                                                                         DataType.DvdDiscKey =>
                                                                             MediaTagType.DVD_DiscKey,
                                                                         DataType.DvdBca => MediaTagType.DVD_BCA,
                                                                         DataType.DvdDmi => MediaTagType.DVD_DMI,
                                                                         DataType.DvdMediaIdentifier => MediaTagType
                                                                            .DVD_MediaIdentifier,
                                                                         DataType.DvdMediaKeyBlock => MediaTagType
                                                                            .DVD_MKB,
                                                                         DataType.DvdRamDds => MediaTagType.DVDRAM_DDS,
                                                                         DataType.DvdRamMediumStatus => MediaTagType
                                                                            .DVDRAM_MediumStatus,
                                                                         DataType.DvdRamSpareArea => MediaTagType
                                                                            .DVDRAM_SpareArea,
                                                                         DataType.DvdRRmd => MediaTagType.DVDR_RMD,
                                                                         DataType.DvdRPrerecordedInfo => MediaTagType
                                                                            .DVDR_PreRecordedInfo,
                                                                         DataType.DvdRMediaIdentifier => MediaTagType
                                                                            .DVDR_MediaIdentifier,
                                                                         DataType.DvdRPfi  => MediaTagType.DVDR_PFI,
                                                                         DataType.DvdAdip  => MediaTagType.DVD_ADIP,
                                                                         DataType.HdDvdCpi => MediaTagType.HDDVD_CPI,
                                                                         DataType.HdDvdMediumStatus => MediaTagType
                                                                            .HDDVD_MediumStatus,
                                                                         DataType.DvdDlLayerCapacity => MediaTagType
                                                                            .DVDDL_LayerCapacity,
                                                                         DataType.DvdDlMiddleZoneAddress => MediaTagType
                                                                            .DVDDL_MiddleZoneAddress,
                                                                         DataType.DvdDlJumpIntervalSize => MediaTagType
                                                                            .DVDDL_JumpIntervalSize,
                                                                         DataType.DvdDlManualLayerJumpLba =>
                                                                             MediaTagType.DVDDL_ManualLayerJumpLBA,
                                                                         DataType.BlurayDi  => MediaTagType.BD_DI,
                                                                         DataType.BlurayBca => MediaTagType.BD_BCA,
                                                                         DataType.BlurayDds => MediaTagType.BD_DDS,
                                                                         DataType.BlurayCartridgeStatus => MediaTagType
                                                                            .BD_CartridgeStatus,
                                                                         DataType.BluraySpareArea => MediaTagType
                                                                            .BD_SpareArea,
                                                                         DataType.AacsVolumeIdentifier => MediaTagType
                                                                            .AACS_VolumeIdentifier,
                                                                         DataType.AacsSerialNumber => MediaTagType
                                                                            .AACS_SerialNumber,
                                                                         DataType.AacsMediaIdentifier => MediaTagType
                                                                            .AACS_MediaIdentifier,
                                                                         DataType.AacsMediaKeyBlock => MediaTagType
                                                                            .AACS_MKB,
                                                                         DataType.AacsDataKeys => MediaTagType
                                                                            .AACS_DataKeys,
                                                                         DataType.AacsLbaExtents => MediaTagType
                                                                            .AACS_LBAExtents,
                                                                         DataType.CprmMediaKeyBlock => MediaTagType
                                                                            .AACS_CPRM_MKB,
                                                                         DataType.HybridRecognizedLayers => MediaTagType
                                                                            .Hybrid_RecognizedLayers,
                                                                         DataType.ScsiMmcWriteProtection => MediaTagType
                                                                            .MMC_WriteProtection,
                                                                         DataType.ScsiMmcDiscInformation => MediaTagType
                                                                            .MMC_DiscInformation,
                                                                         DataType.ScsiMmcTrackResourcesInformation =>
                                                                             MediaTagType.MMC_TrackResourcesInformation,
                                                                         DataType.ScsiMmcPowResourcesInformation =>
                                                                             MediaTagType.MMC_POWResourcesInformation,
                                                                         DataType.ScsiInquiry => MediaTagType
                                                                            .SCSI_INQUIRY,
                                                                         DataType.ScsiModePage2A => MediaTagType
                                                                            .SCSI_MODEPAGE_2A,
                                                                         DataType.AtaIdentify => MediaTagType
                                                                            .ATA_IDENTIFY,
                                                                         DataType.AtapiIdentify => MediaTagType
                                                                            .ATAPI_IDENTIFY,
                                                                         DataType.PcmciaCis => MediaTagType.PCMCIA_CIS,
                                                                         DataType.SecureDigitalCid => MediaTagType
                                                                            .SD_CID,
                                                                         DataType.SecureDigitalCsd => MediaTagType
                                                                            .SD_CSD,
                                                                         DataType.SecureDigitalScr => MediaTagType
                                                                            .SD_SCR,
                                                                         DataType.SecureDigitalOcr => MediaTagType
                                                                            .SD_OCR,
                                                                         DataType.MultiMediaCardCid => MediaTagType
                                                                            .MMC_CID,
                                                                         DataType.MultiMediaCardCsd => MediaTagType
                                                                            .MMC_CSD,
                                                                         DataType.MultiMediaCardOcr => MediaTagType
                                                                            .MMC_OCR,
                                                                         DataType.MultiMediaCardExtendedCsd =>
                                                                             MediaTagType.MMC_ExtendedCSD,
                                                                         DataType.XboxSecuritySector => MediaTagType
                                                                            .Xbox_SecuritySector,
                                                                         DataType.FloppyLeadOut => MediaTagType
                                                                            .Floppy_LeadOut,
                                                                         DataType.DvdDiscControlBlock => MediaTagType
                                                                            .DCB,
                                                                         DataType.CompactDiscFirstTrackPregap =>
                                                                             MediaTagType.CD_FirstTrackPregap,
                                                                         DataType.CompactDiscLeadOut => MediaTagType
                                                                            .CD_LeadOut,
                                                                         DataType.ScsiModeSense6 => MediaTagType
                                                                            .SCSI_MODESENSE_6,
                                                                         DataType.ScsiModeSense10 => MediaTagType
                                                                            .SCSI_MODESENSE_10,
                                                                         DataType.UsbDescriptors => MediaTagType
                                                                            .USB_Descriptors,
                                                                         DataType.XboxDmi => MediaTagType.Xbox_DMI,
                                                                         DataType.XboxPfi => MediaTagType.Xbox_PFI,
                                                                         DataType.CompactDiscMediaCatalogueNumber =>
                                                                             MediaTagType.CD_MCN,
                                                                         DataType.CompactDiscLeadIn => MediaTagType
                                                                            .CD_LeadIn,
                                                                         DataType.DvdDiscKeyDecrypted => MediaTagType
                                                                            .DVD_DiscKey_Decrypted,
                                                                         _ => throw new ArgumentOutOfRangeException()
                                                                     };

    // Converts between Aaru media tag type and image data type
    static DataType GetDataTypeForMediaTag(MediaTagType tag) => tag switch
                                                                {
                                                                    MediaTagType.CD_TOC => DataType
                                                                       .CompactDiscPartialToc,
                                                                    MediaTagType.CD_SessionInfo => DataType
                                                                       .CompactDiscSessionInfo,
                                                                    MediaTagType.CD_FullTOC => DataType.CompactDiscToc,
                                                                    MediaTagType.CD_PMA     => DataType.CompactDiscPma,
                                                                    MediaTagType.CD_ATIP    => DataType.CompactDiscAtip,
                                                                    MediaTagType.CD_TEXT => DataType
                                                                       .CompactDiscLeadInCdText,
                                                                    MediaTagType.DVD_PFI     => DataType.DvdPfi,
                                                                    MediaTagType.DVD_CMI     => DataType.DvdLeadInCmi,
                                                                    MediaTagType.DVD_DiscKey => DataType.DvdDiscKey,
                                                                    MediaTagType.DVD_BCA     => DataType.DvdBca,
                                                                    MediaTagType.DVD_DMI     => DataType.DvdDmi,
                                                                    MediaTagType.DVD_MediaIdentifier => DataType
                                                                       .DvdMediaIdentifier,
                                                                    MediaTagType.DVD_MKB => DataType.DvdMediaKeyBlock,
                                                                    MediaTagType.DVDRAM_DDS => DataType.DvdRamDds,
                                                                    MediaTagType.DVDRAM_MediumStatus => DataType
                                                                       .DvdRamMediumStatus,
                                                                    MediaTagType.DVDRAM_SpareArea => DataType
                                                                       .DvdRamSpareArea,
                                                                    MediaTagType.DVDR_RMD => DataType.DvdRRmd,
                                                                    MediaTagType.DVDR_PreRecordedInfo => DataType
                                                                       .DvdRPrerecordedInfo,
                                                                    MediaTagType.DVDR_MediaIdentifier => DataType
                                                                       .DvdRMediaIdentifier,
                                                                    MediaTagType.DVDR_PFI  => DataType.DvdRPfi,
                                                                    MediaTagType.DVD_ADIP  => DataType.DvdAdip,
                                                                    MediaTagType.HDDVD_CPI => DataType.HdDvdCpi,
                                                                    MediaTagType.HDDVD_MediumStatus => DataType
                                                                       .HdDvdMediumStatus,
                                                                    MediaTagType.DVDDL_LayerCapacity => DataType
                                                                       .DvdDlLayerCapacity,
                                                                    MediaTagType.DVDDL_MiddleZoneAddress => DataType
                                                                       .DvdDlMiddleZoneAddress,
                                                                    MediaTagType.DVDDL_JumpIntervalSize => DataType
                                                                       .DvdDlJumpIntervalSize,
                                                                    MediaTagType.DVDDL_ManualLayerJumpLBA => DataType
                                                                       .DvdDlManualLayerJumpLba,
                                                                    MediaTagType.BD_DI  => DataType.BlurayDi,
                                                                    MediaTagType.BD_BCA => DataType.BlurayBca,
                                                                    MediaTagType.BD_DDS => DataType.BlurayDds,
                                                                    MediaTagType.BD_CartridgeStatus => DataType
                                                                       .BlurayCartridgeStatus,
                                                                    MediaTagType.BD_SpareArea => DataType
                                                                       .BluraySpareArea,
                                                                    MediaTagType.AACS_VolumeIdentifier => DataType
                                                                       .AacsVolumeIdentifier,
                                                                    MediaTagType.AACS_SerialNumber => DataType
                                                                       .AacsSerialNumber,
                                                                    MediaTagType.AACS_MediaIdentifier => DataType
                                                                       .AacsMediaIdentifier,
                                                                    MediaTagType.AACS_MKB => DataType.AacsMediaKeyBlock,
                                                                    MediaTagType.AACS_DataKeys => DataType.AacsDataKeys,
                                                                    MediaTagType.AACS_LBAExtents => DataType
                                                                       .AacsLbaExtents,
                                                                    MediaTagType.AACS_CPRM_MKB => DataType
                                                                       .CprmMediaKeyBlock,
                                                                    MediaTagType.Hybrid_RecognizedLayers => DataType
                                                                       .HybridRecognizedLayers,
                                                                    MediaTagType.MMC_WriteProtection => DataType
                                                                       .ScsiMmcWriteProtection,
                                                                    MediaTagType.MMC_DiscInformation => DataType
                                                                       .ScsiMmcDiscInformation,
                                                                    MediaTagType.MMC_TrackResourcesInformation =>
                                                                        DataType.ScsiMmcTrackResourcesInformation,
                                                                    MediaTagType.MMC_POWResourcesInformation => DataType
                                                                       .ScsiMmcPowResourcesInformation,
                                                                    MediaTagType.SCSI_INQUIRY => DataType.ScsiInquiry,
                                                                    MediaTagType.SCSI_MODEPAGE_2A => DataType
                                                                       .ScsiModePage2A,
                                                                    MediaTagType.ATA_IDENTIFY => DataType.AtaIdentify,
                                                                    MediaTagType.ATAPI_IDENTIFY => DataType
                                                                       .AtapiIdentify,
                                                                    MediaTagType.PCMCIA_CIS => DataType.PcmciaCis,
                                                                    MediaTagType.SD_CID => DataType.SecureDigitalCid,
                                                                    MediaTagType.SD_CSD => DataType.SecureDigitalCsd,
                                                                    MediaTagType.SD_SCR => DataType.SecureDigitalScr,
                                                                    MediaTagType.SD_OCR => DataType.SecureDigitalOcr,
                                                                    MediaTagType.MMC_CID => DataType.MultiMediaCardCid,
                                                                    MediaTagType.MMC_CSD => DataType.MultiMediaCardCsd,
                                                                    MediaTagType.MMC_OCR => DataType.MultiMediaCardOcr,
                                                                    MediaTagType.MMC_ExtendedCSD => DataType
                                                                       .MultiMediaCardExtendedCsd,
                                                                    MediaTagType.Xbox_SecuritySector => DataType
                                                                       .XboxSecuritySector,
                                                                    MediaTagType.Floppy_LeadOut => DataType
                                                                       .FloppyLeadOut,
                                                                    MediaTagType.DCB => DataType.DvdDiscControlBlock,
                                                                    MediaTagType.CD_FirstTrackPregap => DataType
                                                                       .CompactDiscFirstTrackPregap,
                                                                    MediaTagType.CD_LeadOut => DataType
                                                                       .CompactDiscLeadOut,
                                                                    MediaTagType.SCSI_MODESENSE_6 => DataType
                                                                       .ScsiModeSense6,
                                                                    MediaTagType.SCSI_MODESENSE_10 => DataType
                                                                       .ScsiModeSense10,
                                                                    MediaTagType.USB_Descriptors => DataType
                                                                       .UsbDescriptors,
                                                                    MediaTagType.Xbox_DMI => DataType.XboxDmi,
                                                                    MediaTagType.Xbox_PFI => DataType.XboxPfi,
                                                                    MediaTagType.CD_MCN => DataType
                                                                       .CompactDiscMediaCatalogueNumber,
                                                                    MediaTagType.CD_LeadIn =>
                                                                        DataType.CompactDiscLeadIn,
                                                                    MediaTagType.DVD_DiscKey_Decrypted => DataType
                                                                       .DvdDiscKeyDecrypted,
                                                                    _ => throw new
                                                                             ArgumentOutOfRangeException(nameof(tag),
                                                                                 tag,
                                                                                 null)
                                                                };
}