﻿using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using Aaru.Database;
using Aaru.Database.Models;
using Aaru.Gui.Models;
using Aaru.Gui.Views.Dialogs;
using ReactiveUI;

namespace Aaru.Gui.ViewModels.Dialogs
{
    public class StatisticsDialogViewModel : ViewModelBase
    {
        readonly StatisticsDialog _view;

        string _analyzeText;
        bool   _analyzeVisible;
        string _checksumText;
        bool   _checksumVisible;
        bool   _commandsVisible;
        string _compareText;
        bool   _compareVisible;
        string _convertImageText;
        bool   _convertImageVisible;
        string _createSidecarText;
        bool   _createSidecarVisible;
        string _decodeText;
        bool   _decodeVisible;
        string _deviceInfoText;
        bool   _deviceInfoVisible;
        string _deviceReportText;
        bool   _deviceReportVisible;
        bool   _devicesVisible;
        string _dumpMediaText;
        bool   _dumpMediaVisible;
        string _entropyText;
        bool   _entropyVisible;
        bool   _filesystemsVisible;
        bool   _filtersVisible;
        bool   _formatsCommandVisible;
        string _formatsText;
        bool   _formatsVisible;
        string _imageInfoText;
        bool   _imageInfoVisible;
        string _mediaInfoText;
        bool   _mediaInfoVisible;
        string _mediaScanText;
        bool   _mediaScanVisible;
        bool   _mediasVisible;
        bool   _partitionsVisible;
        string _printHexText;
        bool   _printHexVisible;
        string _verifyText;
        bool   _verifyVisible;

        public StatisticsDialogViewModel(StatisticsDialog view)
        {
            _view        = view;
            Filters      = new ObservableCollection<NameCountModel>();
            Formats      = new ObservableCollection<NameCountModel>();
            Partitions   = new ObservableCollection<NameCountModel>();
            Filesystems  = new ObservableCollection<NameCountModel>();
            Devices      = new ObservableCollection<DeviceStatsModel>();
            Medias       = new ObservableCollection<MediaStatsModel>();
            CloseCommand = ReactiveCommand.Create(ExecuteCloseCommand);
            using var ctx = AaruContext.Create(Settings.Settings.LocalDbPath);

            if(ctx.Commands.Any())
            {
                if(ctx.Commands.Any(c => c.Name == "analyze"))
                {
                    ulong count = ctx.Commands.Where(c => c.Name == "analyze" && c.Synchronized).Select(c => c.Count).
                                      FirstOrDefault();

                    count += (ulong)ctx.Commands.LongCount(c => c.Name == "analyze" && !c.Synchronized);

                    AnalyzeVisible = true;
                    AnalyzeText    = $"You have called the Analyze command {count} times";
                }

                if(ctx.Commands.Any(c => c.Name == "checksum"))
                {
                    ulong count = ctx.Commands.Where(c => c.Name == "checksum" && c.Synchronized).Select(c => c.Count).
                                      FirstOrDefault();

                    count += (ulong)ctx.Commands.LongCount(c => c.Name == "checksum" && !c.Synchronized);

                    ChecksumVisible = true;
                    ChecksumText    = $"You have called the Checksum command {count} times";
                }

                if(ctx.Commands.Any(c => c.Name == "compare"))
                {
                    ulong count = ctx.Commands.Where(c => c.Name == "compare" && c.Synchronized).Select(c => c.Count).
                                      FirstOrDefault();

                    count += (ulong)ctx.Commands.LongCount(c => c.Name == "compare" && !c.Synchronized);

                    CompareVisible = true;
                    CompareText    = $"You have called the Compare command {count} times";
                }

                if(ctx.Commands.Any(c => c.Name == "convert-image"))
                {
                    ulong count = ctx.Commands.Where(c => c.Name == "convert-image" && c.Synchronized).
                                      Select(c => c.Count).FirstOrDefault();

                    count += (ulong)ctx.Commands.LongCount(c => c.Name == "convert-image" && !c.Synchronized);

                    ConvertImageVisible = true;
                    ConvertImageText    = $"You have called the Convert-Image command {count} times";
                }

                if(ctx.Commands.Any(c => c.Name == "create-sidecar"))
                {
                    ulong count = ctx.Commands.Where(c => c.Name == "create-sidecar" && c.Synchronized).
                                      Select(c => c.Count).FirstOrDefault();

                    count += (ulong)ctx.Commands.LongCount(c => c.Name == "create-sidecar" && !c.Synchronized);

                    CreateSidecarVisible = true;
                    CreateSidecarText    = $"You have called the Create-Sidecar command {count} times";
                }

                if(ctx.Commands.Any(c => c.Name == "decode"))
                {
                    ulong count = ctx.Commands.Where(c => c.Name == "decode" && c.Synchronized).Select(c => c.Count).
                                      FirstOrDefault();

                    count += (ulong)ctx.Commands.LongCount(c => c.Name == "decode" && !c.Synchronized);

                    DecodeVisible = true;
                    DecodeText    = $"You have called the Decode command {count} times";
                }

                if(ctx.Commands.Any(c => c.Name == "device-info"))
                {
                    ulong count = ctx.Commands.Where(c => c.Name == "device-info" && c.Synchronized).
                                      Select(c => c.Count).FirstOrDefault();

                    count += (ulong)ctx.Commands.LongCount(c => c.Name == "device-info" && !c.Synchronized);

                    DeviceInfoVisible = true;
                    DeviceInfoText    = $"You have called the Device-Info command {count} times";
                }

                if(ctx.Commands.Any(c => c.Name == "device-report"))
                {
                    ulong count = ctx.Commands.Where(c => c.Name == "device-report" && c.Synchronized).
                                      Select(c => c.Count).FirstOrDefault();

                    count += (ulong)ctx.Commands.LongCount(c => c.Name == "device-report" && !c.Synchronized);

                    DeviceReportVisible = true;
                    DeviceReportText    = $"You have called the Device-Report command {count} times";
                }

                if(ctx.Commands.Any(c => c.Name == "dump-media"))
                {
                    ulong count = ctx.Commands.Where(c => c.Name == "dump-media" && c.Synchronized).
                                      Select(c => c.Count).FirstOrDefault();

                    count += (ulong)ctx.Commands.LongCount(c => c.Name == "dump-media" && !c.Synchronized);

                    DumpMediaVisible = true;
                    DumpMediaText    = $"You have called the Dump-Media command {count} times";
                }

                if(ctx.Commands.Any(c => c.Name == "entropy"))
                {
                    ulong count = ctx.Commands.Where(c => c.Name == "entropy" && c.Synchronized).Select(c => c.Count).
                                      FirstOrDefault();

                    count += (ulong)ctx.Commands.LongCount(c => c.Name == "entropy" && !c.Synchronized);

                    EntropyVisible = true;
                    EntropyText    = $"You have called the Entropy command {count} times";
                }

                if(ctx.Commands.Any(c => c.Name == "formats"))
                {
                    ulong count = ctx.Commands.Where(c => c.Name == "formats" && c.Synchronized).Select(c => c.Count).
                                      FirstOrDefault();

                    count += (ulong)ctx.Commands.LongCount(c => c.Name == "formats" && !c.Synchronized);

                    FormatsCommandVisible = true;
                    FormatsCommandText    = $"You have called the Formats command {count} times";
                }

                if(ctx.Commands.Any(c => c.Name == "image-info"))
                {
                    ulong count = ctx.Commands.Where(c => c.Name == "image-info" && c.Synchronized).
                                      Select(c => c.Count).FirstOrDefault();

                    count += (ulong)ctx.Commands.LongCount(c => c.Name == "image-info" && !c.Synchronized);

                    ImageInfoVisible = true;
                    ImageInfoText    = $"You have called the Image-Info command {count} times";
                }

                if(ctx.Commands.Any(c => c.Name == "media-info"))
                {
                    ulong count = ctx.Commands.Where(c => c.Name == "media-info" && c.Synchronized).
                                      Select(c => c.Count).FirstOrDefault();

                    count += (ulong)ctx.Commands.LongCount(c => c.Name == "media-info" && !c.Synchronized);

                    MediaInfoVisible = true;
                    MediaInfoText    = $"You have called the Media-Info command {count} times";
                }

                if(ctx.Commands.Any(c => c.Name == "media-scan"))
                {
                    ulong count = ctx.Commands.Where(c => c.Name == "media-scan" && c.Synchronized).
                                      Select(c => c.Count).FirstOrDefault();

                    count += (ulong)ctx.Commands.LongCount(c => c.Name == "media-scan" && !c.Synchronized);

                    MediaScanVisible = true;
                    MediaScanText    = $"You have called the Media-Scan command {count} times";
                }

                if(ctx.Commands.Any(c => c.Name == "printhex"))
                {
                    ulong count = ctx.Commands.Where(c => c.Name == "printhex" && c.Synchronized).Select(c => c.Count).
                                      FirstOrDefault();

                    count += (ulong)ctx.Commands.LongCount(c => c.Name == "printhex" && !c.Synchronized);

                    PrintHexVisible = true;
                    PrintHexText    = $"You have called the Print-Hex command {count} times";
                }

                if(ctx.Commands.Any(c => c.Name == "verify"))
                {
                    ulong count = ctx.Commands.Where(c => c.Name == "verify" && c.Synchronized).Select(c => c.Count).
                                      FirstOrDefault();

                    count += (ulong)ctx.Commands.LongCount(c => c.Name == "verify" && !c.Synchronized);

                    VerifyVisible = true;
                    VerifyText    = $"You have called the Verify command {count} times";
                }

                CommandsVisible = AnalyzeVisible       || ChecksumVisible || CompareVisible ||
                                  ConvertImageVisible  ||
                                  CreateSidecarVisible || DecodeVisible || DeviceInfoVisible ||
                                  DeviceReportVisible  ||
                                  DumpMediaVisible     || EntropyVisible || FormatsCommandVisible ||
                                  ImageInfoVisible     ||
                                  MediaInfoVisible     || MediaScanVisible || PrintHexVisible || VerifyVisible;
            }

            if(ctx.Filters.Any())
            {
                FiltersVisible = true;

                foreach(string nvs in ctx.Filters.Select(n => n.Name).Distinct())
                {
                    ulong count = ctx.Filters.Where(c => c.Name == nvs && c.Synchronized).Select(c => c.Count).
                                      FirstOrDefault();

                    count += (ulong)ctx.Filters.LongCount(c => c.Name == nvs && !c.Synchronized);

                    Filters.Add(new NameCountModel
                    {
                        Name = nvs, Count = count
                    });
                }
            }

            if(ctx.MediaFormats.Any())
            {
                FormatsVisible = true;

                foreach(string nvs in ctx.MediaFormats.Select(n => n.Name).Distinct())
                {
                    ulong count = ctx.MediaFormats.Where(c => c.Name == nvs && c.Synchronized).Select(c => c.Count).
                                      FirstOrDefault();

                    count += (ulong)ctx.MediaFormats.LongCount(c => c.Name == nvs && !c.Synchronized);

                    Formats.Add(new NameCountModel
                    {
                        Name = nvs, Count = count
                    });
                }
            }

            if(ctx.Partitions.Any())
            {
                PartitionsVisible = true;

                foreach(string nvs in ctx.Partitions.Select(n => n.Name).Distinct())
                {
                    ulong count = ctx.Partitions.Where(c => c.Name == nvs && c.Synchronized).Select(c => c.Count).
                                      FirstOrDefault();

                    count += (ulong)ctx.Partitions.LongCount(c => c.Name == nvs && !c.Synchronized);

                    Partitions.Add(new NameCountModel
                    {
                        Name = nvs, Count = count
                    });
                }
            }

            if(ctx.Filesystems.Any())
            {
                FilesystemsVisible = true;

                foreach(string nvs in ctx.Filesystems.Select(n => n.Name).Distinct())
                {
                    ulong count = ctx.Filesystems.Where(c => c.Name == nvs && c.Synchronized).Select(c => c.Count).
                                      FirstOrDefault();

                    count += (ulong)ctx.Filesystems.LongCount(c => c.Name == nvs && !c.Synchronized);

                    Filesystems.Add(new NameCountModel
                    {
                        Name = nvs, Count = count
                    });
                }
            }

            if(ctx.SeenDevices.Any())
            {
                DevicesVisible = true;

                foreach(DeviceStat ds in ctx.SeenDevices.OrderBy(n => n.Manufacturer).ThenBy(n => n.Manufacturer).
                                             ThenBy(n => n.Revision).ThenBy(n => n.Bus))
                    Devices.Add(new DeviceStatsModel
                    {
                        Model = ds.Model, Manufacturer = ds.Manufacturer, Revision = ds.Revision, Bus = ds.Bus
                    });
            }

            if(!ctx.Medias.Any())
                return;

            MediasVisible = true;

            foreach(string media in ctx.Medias.OrderBy(ms => ms.Type).Select(ms => ms.Type).Distinct())
            {
                ulong count = ctx.Medias.Where(c => c.Type == media && c.Synchronized && c.Real).Select(c => c.Count).
                                  FirstOrDefault();

                count += (ulong)ctx.Medias.LongCount(c => c.Type == media && !c.Synchronized && c.Real);

                if(count > 0)
                    Medias.Add(new MediaStatsModel
                    {
                        Name = media, Count = count, Type = "real"
                    });

                count = ctx.Medias.Where(c => c.Type == media && c.Synchronized && !c.Real).Select(c => c.Count).
                            FirstOrDefault();

                count += (ulong)ctx.Medias.LongCount(c => c.Type == media && !c.Synchronized && !c.Real);

                if(count == 0)
                    continue;

                Medias.Add(new MediaStatsModel
                {
                    Name = media, Count = count, Type = "image"
                });
            }
        }

        public string AnalyzeText
        {
            get => _analyzeText;
            set => this.RaiseAndSetIfChanged(ref _analyzeText, value);
        }

        public bool AnalyzeVisible
        {
            get => _analyzeVisible;
            set => this.RaiseAndSetIfChanged(ref _analyzeVisible, value);
        }

        public string ChecksumText
        {
            get => _checksumText;
            set => this.RaiseAndSetIfChanged(ref _checksumText, value);
        }

        public bool ChecksumVisible
        {
            get => _checksumVisible;
            set => this.RaiseAndSetIfChanged(ref _checksumVisible, value);
        }

        public string CompareText
        {
            get => _compareText;
            set => this.RaiseAndSetIfChanged(ref _compareText, value);
        }

        public bool CompareVisible
        {
            get => _compareVisible;
            set => this.RaiseAndSetIfChanged(ref _compareVisible, value);
        }

        public string ConvertImageText
        {
            get => _convertImageText;
            set => this.RaiseAndSetIfChanged(ref _convertImageText, value);
        }

        public bool ConvertImageVisible
        {
            get => _convertImageVisible;
            set => this.RaiseAndSetIfChanged(ref _convertImageVisible, value);
        }

        public string CreateSidecarText
        {
            get => _createSidecarText;
            set => this.RaiseAndSetIfChanged(ref _createSidecarText, value);
        }

        public bool CreateSidecarVisible
        {
            get => _createSidecarVisible;
            set => this.RaiseAndSetIfChanged(ref _createSidecarVisible, value);
        }

        public string DecodeText
        {
            get => _decodeText;
            set => this.RaiseAndSetIfChanged(ref _decodeText, value);
        }

        public bool DecodeVisible
        {
            get => _decodeVisible;
            set => this.RaiseAndSetIfChanged(ref _decodeVisible, value);
        }

        public string DeviceInfoText
        {
            get => _deviceInfoText;
            set => this.RaiseAndSetIfChanged(ref _deviceInfoText, value);
        }

        public bool DeviceInfoVisible
        {
            get => _deviceInfoVisible;
            set => this.RaiseAndSetIfChanged(ref _deviceInfoVisible, value);
        }

        public string DeviceReportText
        {
            get => _deviceReportText;
            set => this.RaiseAndSetIfChanged(ref _deviceReportText, value);
        }

        public bool DeviceReportVisible
        {
            get => _deviceReportVisible;
            set => this.RaiseAndSetIfChanged(ref _deviceReportVisible, value);
        }

        public string DumpMediaText
        {
            get => _dumpMediaText;
            set => this.RaiseAndSetIfChanged(ref _dumpMediaText, value);
        }

        public bool DumpMediaVisible
        {
            get => _dumpMediaVisible;
            set => this.RaiseAndSetIfChanged(ref _dumpMediaVisible, value);
        }

        public string EntropyText
        {
            get => _entropyText;
            set => this.RaiseAndSetIfChanged(ref _entropyText, value);
        }

        public bool EntropyVisible
        {
            get => _entropyVisible;
            set => this.RaiseAndSetIfChanged(ref _entropyVisible, value);
        }

        public string FormatsCommandText
        {
            get => _formatsText;
            set => this.RaiseAndSetIfChanged(ref _formatsText, value);
        }

        public bool FormatsCommandVisible
        {
            get => _formatsCommandVisible;
            set => this.RaiseAndSetIfChanged(ref _formatsCommandVisible, value);
        }

        public string ImageInfoText
        {
            get => _imageInfoText;
            set => this.RaiseAndSetIfChanged(ref _imageInfoText, value);
        }

        public bool ImageInfoVisible
        {
            get => _imageInfoVisible;
            set => this.RaiseAndSetIfChanged(ref _imageInfoVisible, value);
        }

        public string MediaInfoText
        {
            get => _mediaInfoText;
            set => this.RaiseAndSetIfChanged(ref _mediaInfoText, value);
        }

        public bool MediaInfoVisible
        {
            get => _mediaInfoVisible;
            set => this.RaiseAndSetIfChanged(ref _mediaInfoVisible, value);
        }

        public string MediaScanText
        {
            get => _mediaScanText;
            set => this.RaiseAndSetIfChanged(ref _mediaScanText, value);
        }

        public bool MediaScanVisible
        {
            get => _mediaScanVisible;
            set => this.RaiseAndSetIfChanged(ref _mediaScanVisible, value);
        }

        public string PrintHexText
        {
            get => _printHexText;
            set => this.RaiseAndSetIfChanged(ref _printHexText, value);
        }

        public bool PrintHexVisible
        {
            get => _printHexVisible;
            set => this.RaiseAndSetIfChanged(ref _printHexVisible, value);
        }

        public string VerifyText
        {
            get => _verifyText;
            set => this.RaiseAndSetIfChanged(ref _verifyText, value);
        }

        public bool VerifyVisible
        {
            get => _verifyVisible;
            set => this.RaiseAndSetIfChanged(ref _verifyVisible, value);
        }

        public bool CommandsVisible
        {
            get => _commandsVisible;
            set => this.RaiseAndSetIfChanged(ref _commandsVisible, value);
        }

        public bool FiltersVisible
        {
            get => _filtersVisible;
            set => this.RaiseAndSetIfChanged(ref _filtersVisible, value);
        }

        public bool PartitionsVisible
        {
            get => _partitionsVisible;
            set => this.RaiseAndSetIfChanged(ref _partitionsVisible, value);
        }

        public bool FormatsVisible
        {
            get => _formatsVisible;
            set => this.RaiseAndSetIfChanged(ref _formatsVisible, value);
        }

        public bool FilesystemsVisible
        {
            get => _filesystemsVisible;
            set => this.RaiseAndSetIfChanged(ref _filesystemsVisible, value);
        }

        public bool DevicesVisible
        {
            get => _devicesVisible;
            set => this.RaiseAndSetIfChanged(ref _devicesVisible, value);
        }

        public bool MediasVisible
        {
            get => _mediasVisible;
            set => this.RaiseAndSetIfChanged(ref _mediasVisible, value);
        }

        public string                                 CommandsLabel     => "Commands";
        public string                                 FilterLabel       => "Filter";
        public string                                 PartitionLabel    => "Partition";
        public string                                 PartitionsLabel   => "Partitions";
        public string                                 FiltersLabel      => "Filters";
        public string                                 FormatsLabel      => "Formats";
        public string                                 FormatLabel       => "Format";
        public string                                 FilesystemsLabel  => "Filesystems";
        public string                                 FilesystemLabel   => "Filesystem";
        public string                                 TimesFoundLabel   => "Times found";
        public string                                 DevicesLabel      => "Devices";
        public string                                 DeviceLabel       => "Device";
        public string                                 ManufacturerLabel => "Manufacturer";
        public string                                 RevisionLabel     => "Revision";
        public string                                 BusLabel          => "Bus";
        public string                                 MediasLabel       => "Medias";
        public string                                 MediaLabel        => "Media";
        public string                                 TypeLabel         => "Type";
        public string                                 Title             => "Encodings";
        public string                                 CloseLabel        => "Close";
        public ReactiveCommand<Unit, Unit>            CloseCommand      { get; }
        public ObservableCollection<NameCountModel>   Filters           { get; }
        public ObservableCollection<NameCountModel>   Formats           { get; }
        public ObservableCollection<NameCountModel>   Partitions        { get; }
        public ObservableCollection<NameCountModel>   Filesystems       { get; }
        public ObservableCollection<DeviceStatsModel> Devices           { get; }
        public ObservableCollection<MediaStatsModel>  Medias            { get; }

        void ExecuteCloseCommand() => _view.Close();
    }
}