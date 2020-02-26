// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : Checksum.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Verbs.
//
// --[ Description ] ----------------------------------------------------------
//
//     Implements the 'checksum' verb.
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
using System.CommandLine;
using System.CommandLine.Invocation;
using DiscImageChef.CommonTypes;
using DiscImageChef.CommonTypes.Enums;
using DiscImageChef.CommonTypes.Interfaces;
using DiscImageChef.CommonTypes.Structs;
using DiscImageChef.Console;
using DiscImageChef.Core;
using Schemas;

namespace DiscImageChef.Commands.Image
{
    internal class ChecksumCommand : Command
    {
        // How many sectors to read at once
        const uint SECTORS_TO_READ = 256;

        public ChecksumCommand() : base("checksum", "Checksums an image.")
        {
            AddAlias("chk");

            Add(new Option(new[]
                {
                    "--adler32", "-a"
                }, "Calculates Adler32.")
                {
                    Argument = new Argument<bool>(() => false), Required = false
                });

            Add(new Option("--crc16", "Calculates CRC16.")
            {
                Argument = new Argument<bool>(() => true), Required = false
            });

            Add(new Option(new[]
                {
                    "--crc32", "-c"
                }, "Calculates CRC32.")
                {
                    Argument = new Argument<bool>(() => true), Required = false
                });

            Add(new Option("--crc64", "Calculates CRC64.")
            {
                Argument = new Argument<bool>(() => true), Required = false
            });

            Add(new Option("--fletcher16", "Calculates Fletcher-16.")
            {
                Argument = new Argument<bool>(() => false), Required = false
            });

            Add(new Option("--fletcher32", "Calculates Fletcher-32.")
            {
                Argument = new Argument<bool>(() => false), Required = false
            });

            Add(new Option(new[]
                {
                    "--md5", "-m"
                }, "Calculates MD5.")
                {
                    Argument = new Argument<bool>(() => true), Required = false
                });

            Add(new Option(new[]
                {
                    "--separated-tracks", "-t"
                }, "Checksums each track separately.")
                {
                    Argument = new Argument<bool>(() => true), Required = false
                });

            Add(new Option(new[]
                {
                    "--sha1", "-s"
                }, "Calculates SHA1.")
                {
                    Argument = new Argument<bool>(() => true), Required = false
                });

            Add(new Option("--sha256", "Calculates SHA256.")
            {
                Argument = new Argument<bool>(() => false), Required = false
            });

            Add(new Option("--sha384", "Calculates SHA384.")
            {
                Argument = new Argument<bool>(() => false), Required = false
            });

            Add(new Option("--sha512", "Calculates SHA512.")
            {
                Argument = new Argument<bool>(() => true), Required = false
            });

            Add(new Option(new[]
                {
                    "--spamsum", "-f"
                }, "Calculates SpamSum fuzzy hash.")
                {
                    Argument = new Argument<bool>(() => true), Required = false
                });

            Add(new Option(new[]
                {
                    "--whole-disc", "-w"
                }, "Checksums the whole disc.")
                {
                    Argument = new Argument<bool>(() => true), Required = false
                });

            AddArgument(new Argument<string>
            {
                Arity = ArgumentArity.ExactlyOne, Description = "Media image path", Name = "image-path"
            });

            Handler = CommandHandler.Create(GetType().GetMethod(nameof(Invoke)));
        }

        public static int Invoke(bool debug, bool verbose, bool adler32, bool crc16, bool crc32, bool crc64,
                                 bool fletcher16, bool fletcher32, bool md5, bool sha1, bool sha256, bool sha384,
                                 bool sha512, bool spamSum, string imagePath, bool separatedTracks, bool wholeDisc)
        {
            MainClass.PrintCopyright();

            if(debug)
                DicConsole.DebugWriteLineEvent += System.Console.Error.WriteLine;

            if(verbose)
                DicConsole.VerboseWriteLineEvent += System.Console.WriteLine;

            Statistics.AddCommand("checksum");

            DicConsole.DebugWriteLine("Checksum command", "--adler32={0}", adler32);
            DicConsole.DebugWriteLine("Checksum command", "--crc16={0}", crc16);
            DicConsole.DebugWriteLine("Checksum command", "--crc32={0}", crc32);
            DicConsole.DebugWriteLine("Checksum command", "--crc64={0}", crc64);
            DicConsole.DebugWriteLine("Checksum command", "--debug={0}", debug);
            DicConsole.DebugWriteLine("Checksum command", "--fletcher16={0}", fletcher16);
            DicConsole.DebugWriteLine("Checksum command", "--fletcher32={0}", fletcher32);
            DicConsole.DebugWriteLine("Checksum command", "--input={0}", imagePath);
            DicConsole.DebugWriteLine("Checksum command", "--md5={0}", md5);
            DicConsole.DebugWriteLine("Checksum command", "--separated-tracks={0}", separatedTracks);
            DicConsole.DebugWriteLine("Checksum command", "--sha1={0}", sha1);
            DicConsole.DebugWriteLine("Checksum command", "--sha256={0}", sha256);
            DicConsole.DebugWriteLine("Checksum command", "--sha384={0}", sha384);
            DicConsole.DebugWriteLine("Checksum command", "--sha512={0}", sha512);
            DicConsole.DebugWriteLine("Checksum command", "--spamsum={0}", spamSum);
            DicConsole.DebugWriteLine("Checksum command", "--verbose={0}", verbose);
            DicConsole.DebugWriteLine("Checksum command", "--whole-disc={0}", wholeDisc);

            var     filtersList = new FiltersList();
            IFilter inputFilter = filtersList.GetFilter(imagePath);

            if(inputFilter == null)
            {
                DicConsole.ErrorWriteLine("Cannot open specified file.");

                return(int)ErrorNumber.CannotOpenFile;
            }

            IMediaImage inputFormat = ImageFormat.Detect(inputFilter);

            if(inputFormat == null)
            {
                DicConsole.ErrorWriteLine("Unable to recognize image format, not checksumming");

                return(int)ErrorNumber.UnrecognizedFormat;
            }

            inputFormat.Open(inputFilter);
            Statistics.AddMediaFormat(inputFormat.Format);
            Statistics.AddMedia(inputFormat.Info.MediaType, false);
            Statistics.AddFilter(inputFilter.Name);
            var enabledChecksums = new EnableChecksum();

            if(adler32)
                enabledChecksums |= EnableChecksum.Adler32;

            if(crc16)
                enabledChecksums |= EnableChecksum.Crc16;

            if(crc32)
                enabledChecksums |= EnableChecksum.Crc32;

            if(crc64)
                enabledChecksums |= EnableChecksum.Crc64;

            if(md5)
                enabledChecksums |= EnableChecksum.Md5;

            if(sha1)
                enabledChecksums |= EnableChecksum.Sha1;

            if(sha256)
                enabledChecksums |= EnableChecksum.Sha256;

            if(sha384)
                enabledChecksums |= EnableChecksum.Sha384;

            if(sha512)
                enabledChecksums |= EnableChecksum.Sha512;

            if(spamSum)
                enabledChecksums |= EnableChecksum.SpamSum;

            if(fletcher16)
                enabledChecksums |= EnableChecksum.Fletcher16;

            if(fletcher32)
                enabledChecksums |= EnableChecksum.Fletcher32;

            Checksum mediaChecksum = null;

            switch(inputFormat)
            {
                case IOpticalMediaImage opticalInput when opticalInput.Tracks != null:
                    try
                    {
                        Checksum trackChecksum = null;

                        if(wholeDisc)
                            mediaChecksum = new Checksum(enabledChecksums);

                        ulong previousTrackEnd = 0;

                        List<Track> inputTracks = opticalInput.Tracks;

                        foreach(Track currentTrack in inputTracks)
                        {
                            if(currentTrack.TrackStartSector - previousTrackEnd != 0 && wholeDisc)
                                for(ulong i = previousTrackEnd + 1; i < currentTrack.TrackStartSector; i++)
                                {
                                    DicConsole.Write("\rHashing track-less sector {0}", i);

                                    byte[] hiddenSector = inputFormat.ReadSector(i);

                                    mediaChecksum?.Update(hiddenSector);
                                }

                            DicConsole.DebugWriteLine("Checksum command",
                                                      "Track {0} starts at sector {1} and ends at sector {2}",
                                                      currentTrack.TrackSequence, currentTrack.TrackStartSector,
                                                      currentTrack.TrackEndSector);

                            if(separatedTracks)
                                trackChecksum = new Checksum(enabledChecksums);

                            ulong sectors     = (currentTrack.TrackEndSector - currentTrack.TrackStartSector) + 1;
                            ulong doneSectors = 0;
                            DicConsole.WriteLine("Track {0} has {1} sectors", currentTrack.TrackSequence, sectors);

                            while(doneSectors < sectors)
                            {
                                byte[] sector;

                                if(sectors - doneSectors >= SECTORS_TO_READ)
                                {
                                    sector = opticalInput.ReadSectors(doneSectors, SECTORS_TO_READ,
                                                                      currentTrack.TrackSequence);

                                    DicConsole.Write("\rHashing sectors {0} to {2} of track {1}", doneSectors,
                                                     currentTrack.TrackSequence, doneSectors + SECTORS_TO_READ);

                                    doneSectors += SECTORS_TO_READ;
                                }
                                else
                                {
                                    sector = opticalInput.ReadSectors(doneSectors, (uint)(sectors - doneSectors),
                                                                      currentTrack.TrackSequence);

                                    DicConsole.Write("\rHashing sectors {0} to {2} of track {1}", doneSectors,
                                                     currentTrack.TrackSequence, doneSectors + (sectors - doneSectors));

                                    doneSectors += sectors - doneSectors;
                                }

                                if(wholeDisc)
                                    mediaChecksum?.Update(sector);

                                if(separatedTracks)
                                    trackChecksum?.Update(sector);
                            }

                            DicConsole.WriteLine();

                            if(separatedTracks)
                                if(trackChecksum != null)
                                    foreach(ChecksumType chk in trackChecksum.End())
                                        DicConsole.WriteLine("Track {0}'s {1}: {2}", currentTrack.TrackSequence,
                                                             chk.type, chk.Value);

                            previousTrackEnd = currentTrack.TrackEndSector;
                        }

                        if(opticalInput.Info.Sectors - previousTrackEnd != 0 && wholeDisc)
                            for(ulong i = previousTrackEnd + 1; i < opticalInput.Info.Sectors; i++)
                            {
                                DicConsole.Write("\rHashing track-less sector {0}", i);

                                byte[] hiddenSector = inputFormat.ReadSector(i);
                                mediaChecksum?.Update(hiddenSector);
                            }

                        if(wholeDisc)
                            if(mediaChecksum != null)
                                foreach(ChecksumType chk in mediaChecksum.End())
                                    DicConsole.WriteLine("Disk's {0}: {1}", chk.type, chk.Value);
                    }
                    catch(Exception ex)
                    {
                        if(debug)
                            DicConsole.DebugWriteLine("Could not get tracks because {0}", ex.Message);
                        else
                            DicConsole.WriteLine("Unable to get separate tracks, not checksumming them");
                    }

                    break;

                case ITapeImage tapeImage when tapeImage.IsTape && tapeImage.Files?.Count > 0:
                {
                    Checksum trackChecksum = null;

                    if(wholeDisc)
                        mediaChecksum = new Checksum(enabledChecksums);

                    ulong previousTrackEnd = 0;

                    foreach(TapeFile currentFile in tapeImage.Files)
                    {
                        if(currentFile.FirstBlock - previousTrackEnd != 0 && wholeDisc)
                            for(ulong i = previousTrackEnd + 1; i < currentFile.FirstBlock; i++)
                            {
                                DicConsole.Write("\rHashing file-less block {0}", i);

                                byte[] hiddenSector = inputFormat.ReadSector(i);

                                mediaChecksum?.Update(hiddenSector);
                            }

                        DicConsole.DebugWriteLine("Checksum command",
                                                  "Track {0} starts at sector {1} and ends at block {2}",
                                                  currentFile.File, currentFile.FirstBlock, currentFile.LastBlock);

                        if(separatedTracks)
                            trackChecksum = new Checksum(enabledChecksums);

                        ulong sectors     = (currentFile.LastBlock - currentFile.FirstBlock) + 1;
                        ulong doneSectors = 0;
                        DicConsole.WriteLine("File {0} has {1} sectors", currentFile.File, sectors);

                        while(doneSectors < sectors)
                        {
                            byte[] sector;

                            if(sectors - doneSectors >= SECTORS_TO_READ)
                            {
                                sector = tapeImage.ReadSectors(doneSectors + currentFile.FirstBlock, SECTORS_TO_READ);

                                DicConsole.Write("\rHashing blocks {0} to {2} of file {1}", doneSectors,
                                                 currentFile.File, doneSectors + SECTORS_TO_READ);

                                doneSectors += SECTORS_TO_READ;
                            }
                            else
                            {
                                sector = tapeImage.ReadSectors(doneSectors + currentFile.FirstBlock,
                                                               (uint)(sectors - doneSectors));

                                DicConsole.Write("\rHashing blocks {0} to {2} of file {1}", doneSectors,
                                                 currentFile.File, doneSectors + (sectors - doneSectors));

                                doneSectors += sectors - doneSectors;
                            }

                            if(wholeDisc)
                                mediaChecksum?.Update(sector);

                            if(separatedTracks)
                                trackChecksum?.Update(sector);
                        }

                        DicConsole.WriteLine();

                        if(separatedTracks)
                            if(trackChecksum != null)
                                foreach(ChecksumType chk in trackChecksum.End())
                                    DicConsole.WriteLine("File {0}'s {1}: {2}", currentFile.File, chk.type, chk.Value);

                        previousTrackEnd = currentFile.LastBlock;
                    }

                    if(tapeImage.Info.Sectors - previousTrackEnd != 0 && wholeDisc)
                        for(ulong i = previousTrackEnd + 1; i < tapeImage.Info.Sectors; i++)
                        {
                            DicConsole.Write("\rHashing file-less sector {0}", i);

                            byte[] hiddenSector = inputFormat.ReadSector(i);
                            mediaChecksum?.Update(hiddenSector);
                        }

                    if(wholeDisc)
                        if(mediaChecksum != null)
                            foreach(ChecksumType chk in mediaChecksum.End())
                                DicConsole.WriteLine("Tape's {0}: {1}", chk.type, chk.Value);

                    break;
                }

                default:
                {
                    mediaChecksum = new Checksum(enabledChecksums);

                    ulong sectors = inputFormat.Info.Sectors;
                    DicConsole.WriteLine("Sectors {0}", sectors);
                    ulong doneSectors = 0;

                    while(doneSectors < sectors)
                    {
                        byte[] sector;

                        if(sectors - doneSectors >= SECTORS_TO_READ)
                        {
                            sector = inputFormat.ReadSectors(doneSectors, SECTORS_TO_READ);

                            DicConsole.Write("\rHashing sectors {0} to {1}", doneSectors,
                                             doneSectors + SECTORS_TO_READ);

                            doneSectors += SECTORS_TO_READ;
                        }
                        else
                        {
                            sector = inputFormat.ReadSectors(doneSectors, (uint)(sectors - doneSectors));

                            DicConsole.Write("\rHashing sectors {0} to {1}", doneSectors,
                                             doneSectors + (sectors - doneSectors));

                            doneSectors += sectors - doneSectors;
                        }

                        mediaChecksum.Update(sector);
                    }

                    DicConsole.WriteLine();

                    foreach(ChecksumType chk in mediaChecksum.End())
                        DicConsole.WriteLine("Disk's {0}: {1}", chk.type, chk.Value);

                    break;
                }
            }

            return(int)ErrorNumber.NoError;
        }
    }
}