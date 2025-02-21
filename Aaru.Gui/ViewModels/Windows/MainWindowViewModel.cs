﻿// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : MainWindowViewModel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI view models.
//
// --[ Description ] ----------------------------------------------------------
//
//     View model and code for the main window.
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
// Copyright © 2011-2024 Natalia Portillo
// ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Interop;
using Aaru.CommonTypes.Structs.Devices.SCSI;
using Aaru.Console;
using Aaru.Core;
using Aaru.Core.Media.Info;
using Aaru.Database;
using Aaru.Devices;
using Aaru.Gui.Models;
using Aaru.Gui.ViewModels.Dialogs;
using Aaru.Gui.ViewModels.Panels;
using Aaru.Gui.Views.Dialogs;
using Aaru.Gui.Views.Panels;
using Aaru.Gui.Views.Windows;
using Aaru.Localization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using JetBrains.Annotations;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using DeviceInfo = Aaru.Core.Devices.Info.DeviceInfo;
using ImageInfo = Aaru.Gui.Views.Panels.ImageInfo;
using Partition = Aaru.Gui.Views.Panels.Partition;
using PlatformID = Aaru.CommonTypes.Interop.PlatformID;

namespace Aaru.Gui.ViewModels.Windows;

public sealed class MainWindowViewModel : ViewModelBase
{
    const    string           MODULE_NAME = "Main Window ViewModel";
    readonly DevicesRootModel _devicesRoot;
    readonly Bitmap           _ejectIcon;
    readonly Bitmap           _genericFolderIcon;
    readonly Bitmap           _genericHddIcon;
    readonly Bitmap           _genericOpticalIcon;
    readonly Bitmap           _genericTapeIcon;
    readonly ImagesRootModel  _imagesRoot;
    readonly Bitmap           _removableIcon;
    readonly Bitmap           _sdIcon;
    readonly Bitmap           _usbIcon;
    readonly MainWindow       _view;
    Views.Dialogs.Console     _console;
    object                    _contentPanel;
    bool                      _devicesSupported;
    object                    _treeViewSelectedItem;

    public MainWindowViewModel(MainWindow view)
    {
        AboutCommand                = ReactiveCommand.Create(ExecuteAboutCommand);
        EncodingsCommand            = ReactiveCommand.Create(ExecuteEncodingsCommand);
        PluginsCommand              = ReactiveCommand.Create(ExecutePluginsCommand);
        StatisticsCommand           = ReactiveCommand.Create(ExecuteStatisticsCommand);
        ExitCommand                 = ReactiveCommand.Create(ExecuteExitCommand);
        SettingsCommand             = ReactiveCommand.Create(ExecuteSettingsCommand);
        ConsoleCommand              = ReactiveCommand.Create(ExecuteConsoleCommand);
        OpenCommand                 = ReactiveCommand.Create(ExecuteOpenCommand);
        CalculateEntropyCommand     = ReactiveCommand.Create(ExecuteCalculateEntropyCommand);
        VerifyImageCommand          = ReactiveCommand.Create(ExecuteVerifyImageCommand);
        ChecksumImageCommand        = ReactiveCommand.Create(ExecuteChecksumImageCommand);
        ConvertImageCommand         = ReactiveCommand.Create(ExecuteConvertImageCommand);
        CreateSidecarCommand        = ReactiveCommand.Create(ExecuteCreateSidecarCommand);
        ViewImageSectorsCommand     = ReactiveCommand.Create(ExecuteViewImageSectorsCommand);
        DecodeImageMediaTagsCommand = ReactiveCommand.Create(ExecuteDecodeImageMediaTagsCommand);
        RefreshDevicesCommand       = ReactiveCommand.Create(ExecuteRefreshDevicesCommand);
        _view                       = view;
        TreeRoot                    = [];
        ContentPanel                = Greeting;

        _imagesRoot = new ImagesRootModel
        {
            Name = UI.Title_Images
        };

        TreeRoot.Add(_imagesRoot);

        switch(DetectOS.GetRealPlatformID())
        {
            case PlatformID.Win32NT:
            case PlatformID.Linux:
            case PlatformID.FreeBSD:
                _devicesRoot = new DevicesRootModel
                {
                    Name = UI.Title_Devices
                };

                TreeRoot.Add(_devicesRoot);
                DevicesSupported = true;

                break;
        }

        _genericHddIcon =
            new Bitmap(AssetLoader.Open(new Uri("avares://Aaru.Gui/Assets/Icons/oxygen/32x32/drive-harddisk.png")));

        _genericOpticalIcon =
            new Bitmap(AssetLoader.Open(new Uri("avares://Aaru.Gui/Assets/Icons/oxygen/32x32/drive-optical.png")));

        _genericTapeIcon =
            new Bitmap(AssetLoader.Open(new Uri("avares://Aaru.Gui/Assets/Icons/oxygen/32x32/media-tape.png")));

        _genericFolderIcon =
            new Bitmap(AssetLoader.Open(new Uri("avares://Aaru.Gui/Assets/Icons/oxygen/32x32/inode-directory.png")));

        _usbIcon =
            new
                Bitmap(AssetLoader.Open(new
                                            Uri("avares://Aaru.Gui/Assets/Icons/oxygen/32x32/drive-removable-media-usb.png")));

        _removableIcon =
            new
                Bitmap(AssetLoader.Open(new Uri("avares://Aaru.Gui/Assets/Icons/oxygen/32x32/drive-removable-media.png")));

        _sdIcon =
            new Bitmap(AssetLoader.Open(new Uri("avares://Aaru.Gui/Assets/Icons/oxygen/32x32/media-flash-sd-mmc.png")));

        _ejectIcon =
            new Bitmap(AssetLoader.Open(new Uri("avares://Aaru.Gui/Assets/Icons/oxygen/32x32/media-eject.png")));
    }

    public string FileLabel                 => UI.Menu_File;
    public string OpenLabel                 => UI.Menu_Open;
    public string SettingsLabel             => UI.Menu_Settings;
    public string ExitLabel                 => UI.Menu_Exit;
    public string DevicesLabel              => UI.Menu_Devices;
    public string RefreshDevicesLabel       => UI.Menu_Refresh;
    public string WindowLabel               => UI.Menu_Window;
    public string ConsoleLabel              => UI.Menu_Console;
    public string HelpLabel                 => UI.Menu_Help;
    public string EncodingsLabel            => UI.Menu_Encodings;
    public string PluginsLabel              => UI.Menu_Plugins;
    public string StatisticsLabel           => UI.Menu_Statistics;
    public string AboutLabel                => UI.Menu_About;
    public string RefreshDevicesFullLabel   => UI.Menu_Refresh_devices;
    public string CloseAllImagesLabel       => UI.Menu_Close_all_images;
    public string CalculateEntropyLabel     => UI.ButtonLabel_Calculate_entropy;
    public string VerifyImageLabel          => UI.ButtonLabel_Verify;
    public string ChecksumImageLabel        => UI.ButtonLabel_Checksum;
    public string CreateSidecarLabel        => UI.ButtonLabel_Create_Aaru_Metadata_sidecar;
    public string ViewImageSectorsLabel     => UI.ButtonLabel_View_sectors;
    public string DecodeImageMediaTagsLabel => UI.ButtonLabel_Decode_media_tags;

    public bool DevicesSupported
    {
        get => _devicesSupported;
        set => this.RaiseAndSetIfChanged(ref _devicesSupported, value);
    }

    public bool NativeMenuSupported
    {
        get
        {
            Window mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
              ?.MainWindow;

            return mainWindow is not null && NativeMenu.GetIsNativeMenuExported(mainWindow);
        }
    }

    [NotNull]
    public string Greeting => UI.Welcome_to_Aaru;

    public ObservableCollection<RootModel> TreeRoot                    { get; }
    public ReactiveCommand<Unit, Unit>     AboutCommand                { get; }
    public ReactiveCommand<Unit, Unit>     ConsoleCommand              { get; }
    public ReactiveCommand<Unit, Unit>     EncodingsCommand            { get; }
    public ReactiveCommand<Unit, Unit>     PluginsCommand              { get; }
    public ReactiveCommand<Unit, Unit>     StatisticsCommand           { get; }
    public ReactiveCommand<Unit, Unit>     ExitCommand                 { get; }
    public ReactiveCommand<Unit, Task>     SettingsCommand             { get; }
    public ReactiveCommand<Unit, Task>     OpenCommand                 { get; }
    public ReactiveCommand<Unit, Unit>     CalculateEntropyCommand     { get; }
    public ReactiveCommand<Unit, Unit>     VerifyImageCommand          { get; }
    public ReactiveCommand<Unit, Unit>     ChecksumImageCommand        { get; }
    public ReactiveCommand<Unit, Unit>     ConvertImageCommand         { get; }
    public ReactiveCommand<Unit, Unit>     CreateSidecarCommand        { get; }
    public ReactiveCommand<Unit, Unit>     ViewImageSectorsCommand     { get; }
    public ReactiveCommand<Unit, Unit>     DecodeImageMediaTagsCommand { get; }
    public ReactiveCommand<Unit, Unit>     RefreshDevicesCommand       { get; }

    public object ContentPanel
    {
        get => _contentPanel;
        set => this.RaiseAndSetIfChanged(ref _contentPanel, value);
    }

    public object TreeViewSelectedItem
    {
        get => _treeViewSelectedItem;
        set
        {
            if(value == _treeViewSelectedItem) return;

            this.RaiseAndSetIfChanged(ref _treeViewSelectedItem, value);

            ContentPanel = null;

            switch(value)
            {
                case ImageModel imageModel:
                    ContentPanel = new ImageInfo
                    {
                        DataContext = imageModel.ViewModel
                    };

                    break;
                case PartitionModel partitionModel:
                    ContentPanel = new Partition
                    {
                        DataContext = partitionModel.ViewModel
                    };

                    break;
                case FileSystemModel fileSystemModel:
                    ContentPanel = new FileSystem
                    {
                        DataContext = fileSystemModel.ViewModel
                    };

                    break;
                case SubdirectoryModel subdirectoryModel:
                    ContentPanel = new Subdirectory
                    {
                        DataContext = new SubdirectoryViewModel(subdirectoryModel, _view)
                    };

                    break;
                case DeviceModel deviceModel:
                {
                    if(deviceModel.ViewModel is null)
                    {
                        var dev = Device.Create(deviceModel.Path, out ErrorNumber devErrno);

                        switch(dev)
                        {
                            case null:
                                ContentPanel = string.Format(UI.Error_0_opening_device, devErrno);

                                return;
                            case Devices.Remote.Device remoteDev:
                                Statistics.AddRemote(remoteDev.RemoteApplication,
                                                     remoteDev.RemoteVersion,
                                                     remoteDev.RemoteOperatingSystem,
                                                     remoteDev.RemoteOperatingSystemVersion,
                                                     remoteDev.RemoteArchitecture);

                                break;
                        }

                        if(dev.Error)
                        {
                            ContentPanel = string.Format(UI.Error_0_opening_device, dev.LastError);

                            return;
                        }

                        var devInfo = new DeviceInfo(dev);

                        deviceModel.ViewModel = new DeviceInfoViewModel(devInfo, _view);

                        if(!dev.IsRemovable)
                        {
                            deviceModel.Media.Add(new MediaModel
                            {
                                NonRemovable = true,
                                Name         = UI.Non_removable_device_commands_not_yet_implemented
                            });
                        }
                        else
                        {
                            // TODO: Removable non-SCSI?
                            var scsiInfo = new ScsiInfo(dev);

                            if(!scsiInfo.MediaInserted)
                            {
                                deviceModel.Media.Add(new MediaModel
                                {
                                    NoMediaInserted = true,
                                    Icon            = _ejectIcon,
                                    Name            = UI.No_media_inserted
                                });
                            }
                            else
                            {
                                var mediaResource =
                                    new Uri($"avares://Aaru.Gui/Assets/Logos/Media/{scsiInfo.MediaType}.png");

                                deviceModel.Media.Add(new MediaModel
                                {
                                    DevicePath = deviceModel.Path,
                                    Icon = AssetLoader.Exists(mediaResource)
                                               ? new Bitmap(AssetLoader.Open(mediaResource))
                                               : null,
                                    Name      = $"{scsiInfo.MediaType}",
                                    ViewModel = new MediaInfoViewModel(scsiInfo, deviceModel.Path, _view)
                                });
                            }
                        }

                        dev.Close();
                    }

                    ContentPanel = new Views.Panels.DeviceInfo
                    {
                        DataContext = deviceModel.ViewModel
                    };

                    break;
                }
                case MediaModel { NonRemovable: true }:
                    ContentPanel = UI.Non_removable_device_commands_not_yet_implemented;

                    break;
                case MediaModel { NoMediaInserted: true }:
                    ContentPanel = UI.No_media_inserted;

                    break;
                case MediaModel mediaModel:
                {
                    if(mediaModel.ViewModel != null)
                    {
                        ContentPanel = new MediaInfo
                        {
                            DataContext = mediaModel.ViewModel
                        };
                    }

                    break;
                }
            }
        }
    }

    void ExecuteCalculateEntropyCommand()
    {
        if(TreeViewSelectedItem is not ImageModel imageModel) return;

        var imageEntropyWindow = new ImageEntropy();
        imageEntropyWindow.DataContext = new ImageEntropyViewModel(imageModel.Image, imageEntropyWindow);

        imageEntropyWindow.Closed += (_, _) => imageEntropyWindow = null;

        imageEntropyWindow.Show();
    }

    void ExecuteVerifyImageCommand()
    {
        if(TreeViewSelectedItem is not ImageModel imageModel) return;

        var imageVerifyWindow = new ImageVerify();
        imageVerifyWindow.DataContext = new ImageVerifyViewModel(imageModel.Image, imageVerifyWindow);

        imageVerifyWindow.Closed += (_, _) => imageVerifyWindow = null;

        imageVerifyWindow.Show();
    }

    void ExecuteChecksumImageCommand()
    {
        if(TreeViewSelectedItem is not ImageModel imageModel) return;

        var imageChecksumWindow = new ImageChecksum();
        imageChecksumWindow.DataContext = new ImageChecksumViewModel(imageModel.Image, imageChecksumWindow);

        imageChecksumWindow.Closed += (_, _) => imageChecksumWindow = null;

        imageChecksumWindow.Show();
    }

    void ExecuteConvertImageCommand()
    {
        if(TreeViewSelectedItem is not ImageModel imageModel) return;

        var imageConvertWindow = new ImageConvert();

        imageConvertWindow.DataContext =
            new ImageConvertViewModel(imageModel.Image, imageModel.Path, imageConvertWindow);

        imageConvertWindow.Closed += (_, _) => imageConvertWindow = null;

        imageConvertWindow.Show();
    }

    void ExecuteCreateSidecarCommand()
    {
        if(TreeViewSelectedItem is not ImageModel imageModel) return;

        var imageSidecarWindow = new ImageSidecar();

        // TODO: Pass thru chosen default encoding
        imageSidecarWindow.DataContext =
            new ImageSidecarViewModel(imageModel.Image,
                                      imageModel.Path,
                                      imageModel.Filter.Id,
                                      null,
                                      imageSidecarWindow);

        imageSidecarWindow.Show();
    }

    void ExecuteViewImageSectorsCommand()
    {
        if(TreeViewSelectedItem is not ImageModel imageModel) return;

        new ViewSector
        {
            DataContext = new ViewSectorViewModel(imageModel.Image)
        }.Show();
    }

    void ExecuteDecodeImageMediaTagsCommand()
    {
        if(TreeViewSelectedItem is not ImageModel imageModel) return;

        new DecodeMediaTags
        {
            DataContext = new DecodeMediaTagsViewModel(imageModel.Image)
        }.Show();
    }

    internal void ExecuteAboutCommand()
    {
        var dialog = new About();
        dialog.DataContext = new AboutViewModel(dialog);
        dialog.ShowDialog(_view);
    }

    void ExecuteEncodingsCommand()
    {
        var dialog = new Encodings();
        dialog.DataContext = new EncodingsViewModel(dialog);
        dialog.ShowDialog(_view);
    }

    void ExecutePluginsCommand()
    {
        var dialog = new PluginsDialog();
        dialog.DataContext = new PluginsViewModel(dialog);
        dialog.ShowDialog(_view);
    }

    void ExecuteStatisticsCommand()
    {
        using var ctx = AaruContext.Create(Settings.Settings.LocalDbPath);

        if(!ctx.Commands.Any()     &&
           !ctx.Filesystems.Any()  &&
           !ctx.Filters.Any()      &&
           !ctx.MediaFormats.Any() &&
           !ctx.Medias.Any()       &&
           !ctx.Partitions.Any()   &&
           !ctx.SeenDevices.Any())
        {
            MessageBoxManager.GetMessageBoxStandard(UI.Title_Warning, UI.There_are_no_statistics)
                             .ShowWindowDialogAsync(_view);

            return;
        }

        var dialog = new StatisticsDialog();
        dialog.DataContext = new StatisticsViewModel(dialog);
        dialog.ShowDialog(_view);
    }

    internal async Task ExecuteSettingsCommand()
    {
        var dialog = new SettingsDialog();
        dialog.DataContext = new SettingsViewModel(dialog, false);
        await dialog.ShowDialog(_view);
    }

    internal void ExecuteExitCommand() =>
        (Application.Current?.ApplicationLifetime as ClassicDesktopStyleApplicationLifetime)?.Shutdown();

    void ExecuteConsoleCommand()
    {
        if(_console is null)
        {
            _console             = new Views.Dialogs.Console();
            _console.DataContext = new ConsoleViewModel(_console);
        }

        _console.Show();
    }

    async Task ExecuteOpenCommand()
    {
        // TODO: Extensions
        IReadOnlyList<IStorageFile> result = await _view.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title         = UI.Dialog_Choose_image_to_open,
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                FilePickerFileTypes.All
            }
        });

        if(result.Count != 1) return;

        IFilter inputFilter = PluginRegister.Singleton.GetFilter(result[0].Path.AbsolutePath);

        if(inputFilter == null)
        {
            MessageBoxManager.GetMessageBoxStandard(UI.Title_Error,
                                                    UI.Cannot_open_specified_file,
                                                    ButtonEnum.Ok,
                                                    Icon.Error);

            return;
        }

        try
        {
            if(ImageFormat.Detect(inputFilter) is not IMediaImage imageFormat)
            {
                MessageBoxManager.GetMessageBoxStandard(UI.Title_Error,
                                                        UI.Image_format_not_identified,
                                                        ButtonEnum.Ok,
                                                        Icon.Error);

                return;
            }

            AaruConsole.WriteLine(UI.Image_format_identified_by_0_1, imageFormat.Name, imageFormat.Id);

            try
            {
                ErrorNumber opened = imageFormat.Open(inputFilter);

                if(opened != ErrorNumber.NoError)
                {
                    MessageBoxManager.GetMessageBoxStandard(UI.Title_Error,
                                                            string.Format(UI.Error_0_opening_image_format, opened),
                                                            ButtonEnum.Ok,
                                                            Icon.Error);

                    AaruConsole.ErrorWriteLine(UI.Unable_to_open_image_format);
                    AaruConsole.ErrorWriteLine(UI.No_error_given);

                    return;
                }

                var mediaResource = new Uri($"avares://Aaru.Gui/Assets/Logos/Media/{imageFormat.Info.MediaType}.png");

                var imageModel = new ImageModel
                {
                    Path = result[0].Path.AbsolutePath,
                    Icon = AssetLoader.Exists(mediaResource)
                               ? new Bitmap(AssetLoader.Open(mediaResource))
                               : imageFormat.Info.MetadataMediaType == MetadataMediaType.BlockMedia
                                   ? _genericHddIcon
                                   : imageFormat.Info.MetadataMediaType == MetadataMediaType.OpticalDisc
                                       ? _genericOpticalIcon
                                       : _genericFolderIcon,
                    FileName  = Path.GetFileName(result[0].Path.AbsolutePath),
                    Image     = imageFormat,
                    ViewModel = new ImageInfoViewModel(result[0].Path.AbsolutePath, inputFilter, imageFormat, _view),
                    Filter    = inputFilter
                };

                List<CommonTypes.Partition> partitions = Core.Partitions.GetAll(imageFormat);
                Core.Partitions.AddSchemesToStats(partitions);

                var            checkRaw = false;
                List<string>   idPlugins;
                PluginRegister plugins = PluginRegister.Singleton;

                if(partitions.Count == 0)
                {
                    AaruConsole.DebugWriteLine(MODULE_NAME, UI.No_partitions_found);

                    checkRaw = true;
                }
                else
                {
                    AaruConsole.WriteLine(UI._0_partitions_found, partitions.Count);

                    foreach(string scheme in partitions.Select(p => p.Scheme).Distinct().OrderBy(s => s))
                    {
                        // TODO: Add icons to partition schemes
                        var schemeModel = new PartitionSchemeModel
                        {
                            Name = scheme
                        };

                        foreach(CommonTypes.Partition partition in partitions.Where(p => p.Scheme == scheme)
                                                                             .OrderBy(p => p.Start))
                        {
                            var partitionModel = new PartitionModel
                            {
                                // TODO: Add icons to partition types
                                Name      = $"{partition.Name} ({partition.Type})",
                                Partition = partition,
                                ViewModel = new PartitionViewModel(partition)
                            };

                            AaruConsole.WriteLine(UI.Identifying_filesystems_on_partition);

                            Core.Filesystems.Identify(imageFormat, out idPlugins, partition);

                            if(idPlugins.Count == 0)
                                AaruConsole.WriteLine(UI.Filesystem_not_identified);
                            else
                            {
                                AaruConsole.WriteLine(string.Format(UI.Identified_by_0_plugins, idPlugins.Count));

                                foreach(string pluginName in idPlugins)
                                {
                                    if(!plugins.Filesystems.TryGetValue(pluginName, out IFilesystem fs)) continue;
                                    if(fs is null) continue;

                                    fs.GetInformation(imageFormat,
                                                      partition,
                                                      null,
                                                      out string information,
                                                      out CommonTypes.AaruMetadata.FileSystem fsMetadata);

                                    var rofs = fs as IReadOnlyFilesystem;

                                    if(rofs != null)
                                    {
                                        ErrorNumber error = rofs.Mount(imageFormat,
                                                                       partition,
                                                                       null,
                                                                       new Dictionary<string, string>(),
                                                                       null);

                                        if(error != ErrorNumber.NoError) rofs = null;
                                    }

                                    var filesystemModel = new FileSystemModel
                                    {
                                        VolumeName = rofs?.Metadata.VolumeName is null
                                                         ? fsMetadata.VolumeName is null
                                                               ? $"{fsMetadata.Type}"
                                                               : $"{fsMetadata.VolumeName} ({fsMetadata.Type})"
                                                         : $"{rofs.Metadata.VolumeName} ({rofs.Metadata.Type})",
                                        Filesystem = fs,
                                        ReadOnlyFilesystem = rofs,
                                        ViewModel = new FileSystemViewModel(rofs?.Metadata ?? fsMetadata, information)
                                    };

                                    // TODO: Trap expanding item
                                    if(rofs != null)
                                    {
                                        filesystemModel.Roots.Add(new SubdirectoryModel
                                        {
                                            Name   = "/",
                                            Path   = "",
                                            Plugin = rofs
                                        });

                                        Statistics.AddCommand("ls");
                                    }

                                    Statistics.AddFilesystem(rofs?.Metadata.Type ?? fsMetadata.Type);
                                    partitionModel.FileSystems.Add(filesystemModel);
                                }
                            }

                            schemeModel.Partitions.Add(partitionModel);
                        }

                        imageModel.PartitionSchemesOrFileSystems.Add(schemeModel);
                    }
                }

                if(checkRaw)
                {
                    var wholePart = new CommonTypes.Partition
                    {
                        Name   = Localization.Core.Whole_device,
                        Length = imageFormat.Info.Sectors,
                        Size   = imageFormat.Info.Sectors * imageFormat.Info.SectorSize
                    };

                    Core.Filesystems.Identify(imageFormat, out idPlugins, wholePart);

                    if(idPlugins.Count == 0)
                        AaruConsole.WriteLine(UI.Filesystem_not_identified);
                    else
                    {
                        AaruConsole.WriteLine(string.Format(UI.Identified_by_0_plugins, idPlugins.Count));

                        foreach(string pluginName in idPlugins)
                        {
                            if(!plugins.Filesystems.TryGetValue(pluginName, out IFilesystem fs)) continue;
                            if(fs is null) continue;

                            fs.GetInformation(imageFormat,
                                              wholePart,
                                              null,
                                              out string information,
                                              out CommonTypes.AaruMetadata.FileSystem fsMetadata);

                            var rofs = fs as IReadOnlyFilesystem;

                            if(rofs != null)
                            {
                                ErrorNumber error = rofs.Mount(imageFormat,
                                                               wholePart,
                                                               null,
                                                               new Dictionary<string, string>(),
                                                               null);

                                if(error != ErrorNumber.NoError) rofs = null;
                            }

                            var filesystemModel = new FileSystemModel
                            {
                                VolumeName = rofs?.Metadata.VolumeName is null
                                                 ? fsMetadata.VolumeName is null
                                                       ? $"{fsMetadata.Type}"
                                                       : $"{fsMetadata.VolumeName} ({fsMetadata.Type})"
                                                 : $"{rofs.Metadata.VolumeName} ({rofs.Metadata.Type})",
                                Filesystem         = fs,
                                ReadOnlyFilesystem = rofs,
                                ViewModel          = new FileSystemViewModel(rofs?.Metadata ?? fsMetadata, information)
                            };

                            // TODO: Trap expanding item
                            if(rofs != null)
                            {
                                filesystemModel.Roots.Add(new SubdirectoryModel
                                {
                                    Name   = "/",
                                    Path   = "",
                                    Plugin = rofs
                                });

                                Statistics.AddCommand("ls");
                            }

                            Statistics.AddFilesystem(rofs?.Metadata.Type ?? fsMetadata.Type);
                            imageModel.PartitionSchemesOrFileSystems.Add(filesystemModel);
                        }
                    }
                }

                Statistics.AddMediaFormat(imageFormat.Format);
                Statistics.AddMedia(imageFormat.Info.MediaType, false);
                Statistics.AddFilter(inputFilter.Name);

                _imagesRoot.Images.Add(imageModel);
            }
            catch(Exception ex)
            {
                MessageBoxManager.GetMessageBoxStandard(UI.Title_Error,
                                                        UI.Unable_to_open_image_format,
                                                        ButtonEnum.Ok,
                                                        Icon.Error);

                AaruConsole.ErrorWriteLine(UI.Unable_to_open_image_format);
                AaruConsole.ErrorWriteLine(Localization.Core.Error_0, ex.Message);
                AaruConsole.WriteException(ex);
            }
        }
        catch(Exception ex)
        {
            MessageBoxManager.GetMessageBoxStandard(UI.Title_Error,
                                                    UI.Exception_reading_file,
                                                    ButtonEnum.Ok,
                                                    Icon.Error);

            AaruConsole.ErrorWriteLine(string.Format(UI.Error_reading_file_0, ex.Message));
            AaruConsole.WriteException(ex);
        }

        Statistics.AddCommand("image-info");
    }

    internal void LoadComplete() => RefreshDevices();

    void ExecuteRefreshDevicesCommand() => RefreshDevices();

    void RefreshDevices()
    {
        if(!DevicesSupported) return;

        try
        {
            AaruConsole.WriteLine(UI.Refreshing_devices);
            _devicesRoot.Devices.Clear();

            foreach(Devices.DeviceInfo device in Device.ListDevices()
                                                       .Where(d => d.Supported)
                                                       .OrderBy(d => d.Vendor)
                                                       .ThenBy(d => d.Model))
            {
                AaruConsole.DebugWriteLine(MODULE_NAME,
                                           UI.Found_supported_device_model_0_by_manufacturer_1_on_bus_2_and_path_3,
                                           device.Model,
                                           device.Vendor,
                                           device.Bus,
                                           device.Path);

                var deviceModel = new DeviceModel
                {
                    Icon = _genericHddIcon,
                    Name = $"{device.Vendor} {device.Model} ({device.Bus})",
                    Path = device.Path
                };

                var dev = Device.Create(device.Path, out _);

                if(dev != null)
                {
                    if(dev is Devices.Remote.Device remoteDev)
                    {
                        Statistics.AddRemote(remoteDev.RemoteApplication,
                                             remoteDev.RemoteVersion,
                                             remoteDev.RemoteOperatingSystem,
                                             remoteDev.RemoteOperatingSystemVersion,
                                             remoteDev.RemoteArchitecture);
                    }

                    deviceModel.Icon = dev.Type switch
                                       {
                                           DeviceType.ATAPI or DeviceType.SCSI => dev.ScsiType switch
                                               {
                                                   PeripheralDeviceTypes.DirectAccess
                                                    or PeripheralDeviceTypes.SCSIZonedBlockDevice
                                                    or PeripheralDeviceTypes.SimplifiedDevice => dev.IsRemovable
                                                           ? dev.IsUsb ? _usbIcon : _removableIcon
                                                           : _genericHddIcon,
                                                   PeripheralDeviceTypes.SequentialAccess => _genericTapeIcon,
                                                   PeripheralDeviceTypes.OpticalDevice
                                                    or PeripheralDeviceTypes.WriteOnceDevice
                                                    or PeripheralDeviceTypes.OCRWDevice => _removableIcon,
                                                   PeripheralDeviceTypes.MultiMediaDevice => _genericOpticalIcon,
                                                   _                                      => deviceModel.Icon
                                               },
                                           DeviceType.SecureDigital or DeviceType.MMC => _sdIcon,
                                           DeviceType.NVMe                            => null,
                                           _                                          => deviceModel.Icon
                                       };

                    dev.Close();
                }

                _devicesRoot.Devices.Add(deviceModel);
            }
        }
        catch(InvalidOperationException ex)
        {
            AaruConsole.WriteException(ex);
        }
    }
}