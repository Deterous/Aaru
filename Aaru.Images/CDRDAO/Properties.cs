﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : Properties.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains properties for cdrdao cuesheets (toc/bin).
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
using DiscImageChef.CommonTypes;
using DiscImageChef.CommonTypes.Enums;
using DiscImageChef.CommonTypes.Structs;
using Schemas;

namespace DiscImageChef.DiscImages
{
    public partial class Cdrdao
    {
        public ImageInfo       Info       => imageInfo;
        public string          Name       => "CDRDAO tocfile";
        public Guid            Id         => new Guid("04D7BA12-1BE8-44D4-97A4-1B48A505463E");
        public string          Format     => "CDRDAO tocfile";
        public string          Author     => "Natalia Portillo";
        public List<Partition> Partitions { get; private set; }
        public List<Session>   Sessions   => throw new NotImplementedException();

        public List<Track> Tracks
        {
            get
            {
                List<Track> tracks = new List<Track>();

                foreach(CdrdaoTrack cdrTrack in discimage.Tracks)
                {
                    Track dicTrack = new Track
                    {
                        Indexes                = cdrTrack.Indexes,
                        TrackDescription       = cdrTrack.Title,
                        TrackStartSector       = cdrTrack.StartSector,
                        TrackPregap            = cdrTrack.Pregap,
                        TrackSession           = 1,
                        TrackSequence          = cdrTrack.Sequence,
                        TrackType              = CdrdaoTrackTypeToTrackType(cdrTrack.Tracktype),
                        TrackFilter            = cdrTrack.Trackfile.Datafilter,
                        TrackFile              = cdrTrack.Trackfile.Datafilter.GetFilename(),
                        TrackFileOffset        = cdrTrack.Trackfile.Offset,
                        TrackFileType          = cdrTrack.Trackfile.Filetype,
                        TrackRawBytesPerSector = cdrTrack.Bps,
                        TrackBytesPerSector    = CdrdaoTrackTypeToCookedBytesPerSector(cdrTrack.Tracktype)
                    };

                    dicTrack.TrackEndSector = dicTrack.TrackStartSector + cdrTrack.Sectors - 1;
                    if(!cdrTrack.Indexes.TryGetValue(0, out dicTrack.TrackStartSector))
                        cdrTrack.Indexes.TryGetValue(1, out dicTrack.TrackStartSector);
                    if(cdrTrack.Subchannel)
                    {
                        dicTrack.TrackSubchannelType = cdrTrack.Packedsubchannel
                                                           ? TrackSubchannelType.PackedInterleaved
                                                           : TrackSubchannelType.RawInterleaved;
                        dicTrack.TrackSubchannelFilter = cdrTrack.Trackfile.Datafilter;
                        dicTrack.TrackSubchannelFile   = cdrTrack.Trackfile.Datafilter.GetFilename();
                        dicTrack.TrackSubchannelOffset = cdrTrack.Trackfile.Offset;
                    }
                    else dicTrack.TrackSubchannelType = TrackSubchannelType.None;

                    tracks.Add(dicTrack);
                }

                return tracks;
            }
        }
        public List<DumpHardwareType> DumpHardware => null;
        public CICMMetadataType       CicmMetadata => null;
        // TODO: Decode CD-Text to text
        public IEnumerable<MediaTagType> SupportedMediaTags => new[] {MediaTagType.CD_MCN};
        public IEnumerable<SectorTagType> SupportedSectorTags =>
            new[]
            {
                SectorTagType.CdSectorEcc, SectorTagType.CdSectorEccP, SectorTagType.CdSectorEccQ,
                SectorTagType.CdSectorEdc, SectorTagType.CdSectorHeader, SectorTagType.CdSectorSubchannel,
                SectorTagType.CdSectorSubHeader, SectorTagType.CdSectorSync, SectorTagType.CdTrackFlags,
                SectorTagType.CdTrackIsrc
            };
        public IEnumerable<MediaType> SupportedMediaTypes =>
            new[]
            {
                MediaType.CD, MediaType.CDDA, MediaType.CDEG, MediaType.CDG, MediaType.CDI, MediaType.CDMIDI,
                MediaType.CDMRW, MediaType.CDPLUS, MediaType.CDR, MediaType.CDROM, MediaType.CDROMXA,
                MediaType.CDRW, MediaType.CDV, MediaType.DDCD, MediaType.DDCDR, MediaType.DDCDRW, MediaType.MEGACD,
                MediaType.PS1CD, MediaType.PS2CD, MediaType.SuperCDROM2, MediaType.SVCD, MediaType.SATURNCD,
                MediaType.ThreeDO, MediaType.VCD, MediaType.VCDHD, MediaType.NeoGeoCD, MediaType.PCFX,
                MediaType.CDTV, MediaType.CD32, MediaType.Nuon, MediaType.Playdia, MediaType.Pippin,
                MediaType.FMTOWNS, MediaType.MilCD, MediaType.VideoNow, MediaType.VideoNowColor,
                MediaType.VideoNowXp
            };
        public IEnumerable<(string name, Type type, string description, object @default)> SupportedOptions =>
            new[] {("separate", typeof(bool), "Write each track to a separate file.", (object)false)};
        public IEnumerable<string> KnownExtensions => new[] {".toc"};
        public bool                IsWriting       { get; private set; }
        public string              ErrorMessage    { get; private set; }
    }
}