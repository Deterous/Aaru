// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Entropy.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Core algorithms.
//
// --[ Description ] ----------------------------------------------------------
//
//     Calculates the entropy of an image
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
// Copyright © 2011-2020 Natalia Portillo
// ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using Aaru.Checksums;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Console;

namespace Aaru.Core
{
    public sealed class Entropy
    {
        readonly bool        _debug;
        readonly IMediaImage _inputFormat;

        public Entropy(bool debug, IMediaImage inputFormat)
        {
            _debug       = debug;
            _inputFormat = inputFormat;
        }

        public event InitProgressHandler   InitProgressEvent;
        public event UpdateProgressHandler UpdateProgressEvent;
        public event EndProgressHandler    EndProgressEvent;
        public event InitProgressHandler   InitProgress2Event;
        public event UpdateProgressHandler UpdateProgress2Event;
        public event EndProgressHandler    EndProgress2Event;

        public EntropyResults[] CalculateTracksEntropy(bool duplicatedSectors)
        {
            List<EntropyResults> entropyResults = new List<EntropyResults>();

            if(!(_inputFormat is IOpticalMediaImage opticalMediaImage))
            {
                AaruConsole.ErrorWriteLine("The selected image does not support tracks.");

                return entropyResults.ToArray();
            }

            try
            {
                List<Track> inputTracks = opticalMediaImage.Tracks;

                InitProgressEvent?.Invoke();

                foreach(Track currentTrack in inputTracks)
                {
                    var trackEntropy = new EntropyResults
                    {
                        Track   = currentTrack.TrackSequence,
                        Entropy = 0
                    };

                    UpdateProgressEvent?.
                        Invoke($"Entropying track {currentTrack.TrackSequence} of {inputTracks.Max(t => t.TrackSequence)}",
                               currentTrack.TrackSequence, inputTracks.Max(t => t.TrackSequence));

                    ulong[]      entTable              = new ulong[256];
                    ulong        trackSize             = 0;
                    List<string> uniqueSectorsPerTrack = new List<string>();

                    trackEntropy.Sectors = (currentTrack.TrackEndSector - currentTrack.TrackStartSector) + 1;

                    AaruConsole.VerboseWriteLine("Track {0} has {1} sectors", currentTrack.TrackSequence,
                                                 trackEntropy.Sectors);

                    InitProgress2Event?.Invoke();

                    for(ulong i = currentTrack.TrackStartSector; i <= currentTrack.TrackEndSector; i++)
                    {
                        UpdateProgress2Event?.Invoke($"Entropying sector {i + 1} of track {currentTrack.TrackSequence}",
                                                     (long)(i               + 1), (long)currentTrack.TrackEndSector);

                        byte[] sector = opticalMediaImage.ReadSector(i, currentTrack.TrackSequence);

                        if(duplicatedSectors)
                        {
                            string sectorHash = Sha1Context.Data(sector, out _);

                            if(!uniqueSectorsPerTrack.Contains(sectorHash))
                                uniqueSectorsPerTrack.Add(sectorHash);
                        }

                        foreach(byte b in sector)
                            entTable[b]++;

                        trackSize += (ulong)sector.LongLength;
                    }

                    EndProgress2Event?.Invoke();

                    trackEntropy.Entropy += entTable.Select(l => (double)l / (double)trackSize).
                                                     Select(frequency => -(frequency * Math.Log(frequency, 2))).Sum();

                    if(duplicatedSectors)
                        trackEntropy.UniqueSectors = uniqueSectorsPerTrack.Count;

                    entropyResults.Add(trackEntropy);
                }

                EndProgressEvent?.Invoke();
            }
            catch(Exception ex)
            {
                if(_debug)
                    AaruConsole.DebugWriteLine("Could not get tracks because {0}", ex.Message);
                else
                    AaruConsole.ErrorWriteLine("Unable to get separate tracks, not calculating their entropy");
            }

            return entropyResults.ToArray();
        }

        public EntropyResults CalculateMediaEntropy(bool duplicatedSectors)
        {
            var entropy = new EntropyResults
            {
                Entropy = 0
            };

            ulong[]      entTable      = new ulong[256];
            ulong        diskSize      = 0;
            List<string> uniqueSectors = new List<string>();

            entropy.Sectors = _inputFormat.Info.Sectors;
            AaruConsole.WriteLine("Sectors {0}", entropy.Sectors);
            InitProgressEvent?.Invoke();

            for(ulong i = 0; i < entropy.Sectors; i++)
            {
                UpdateProgressEvent?.Invoke($"Entropying sector {i + 1}", (long)(i + 1), (long)entropy.Sectors);
                byte[] sector = _inputFormat.ReadSector(i);

                if(duplicatedSectors)
                {
                    string sectorHash = Sha1Context.Data(sector, out _);

                    if(!uniqueSectors.Contains(sectorHash))
                        uniqueSectors.Add(sectorHash);
                }

                foreach(byte b in sector)
                    entTable[b]++;

                diskSize += (ulong)sector.LongLength;
            }

            EndProgressEvent?.Invoke();

            entropy.Entropy += entTable.Select(l => (double)l / (double)diskSize).
                                        Select(frequency => -(frequency * Math.Log(frequency, 2))).Sum();

            if(duplicatedSectors)
                entropy.UniqueSectors = uniqueSectors.Count;

            return entropy;
        }
    }

    public struct EntropyResults
    {
        public uint   Track;
        public double Entropy;
        public int?   UniqueSectors;
        public ulong  Sectors;
    }
}