// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : MediaDumpViewModel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI view models.
//
// --[ Description ] ----------------------------------------------------------
//
//     View model and code for the media dump window.
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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Aaru.CommonTypes;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Metadata;
using Aaru.Core;
using Aaru.Core.Devices.Dumping;
using Aaru.Core.Logging;
using Aaru.Core.Media.Info;
using Aaru.Devices;
using Aaru.Gui.Models;
using Aaru.Localization;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DynamicData;
using JetBrains.Annotations;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using DeviceInfo = Aaru.Core.Devices.Info.DeviceInfo;
using Dump = Aaru.Core.Devices.Dumping.Dump;
using File = System.IO.File;
using MediaType = Aaru.CommonTypes.MediaType;

namespace Aaru.Gui.ViewModels.Windows;

public sealed class MediaDumpViewModel : ViewModelBase
{
    readonly string  _devicePath;
    readonly Window  _view;
    bool             _closeVisible;
    string           _destination;
    bool             _destinationEnabled;
    Device           _dev;
    Dump             _dumper;
    string           _encodingEnabled;
    bool             _encodingVisible;
    bool             _existingMetadata;
    bool             _force;
    string           _formatReadOnly;
    string           _log;
    bool             _optionsVisible;
    string           _outputPrefix;
    bool             _persistent;
    bool             _progress1Visible;
    bool             _progress2Indeterminate;
    double           _progress2MaxValue;
    string           _progress2Text;
    double           _progress2Value;
    bool             _progress2Visible;
    bool             _progressIndeterminate;
    double           _progressMaxValue;
    string           _progressText;
    double           _progressValue;
    bool             _progressVisible;
    Resume           _resume;
    double           _retries;
    EncodingModel    _selectedEncoding;
    ImagePluginModel _selectedPlugin;
    Metadata         _sidecar;
    double           _skipped;
    bool             _startVisible;
    bool             _stopEnabled;
    bool             _stopOnError;
    bool             _stopVisible;
    bool             _track1Pregap;
    bool             _track1PregapVisible;
    bool             _trim;
    bool             _useResume;
    bool             _useSidecar;

    public MediaDumpViewModel(string               devicePath, DeviceInfo deviceInfo, Window view,
                              [CanBeNull] ScsiInfo scsiInfo = null)
    {
        _view              = view;
        DestinationEnabled = true;
        StartVisible       = true;
        CloseVisible       = true;
        OptionsVisible     = true;
        StartCommand       = ReactiveCommand.Create(ExecuteStartCommand);
        CloseCommand       = ReactiveCommand.Create(ExecuteCloseCommand);
        StopCommand        = ReactiveCommand.Create(ExecuteStopCommand);
        DestinationCommand = ReactiveCommand.Create(ExecuteDestinationCommand);
        PluginsList        = [];
        Encodings          = [];

        // Defaults
        StopOnError      = false;
        Force            = false;
        Persistent       = true;
        Resume           = true;
        Track1Pregap     = false;
        Sidecar          = true;
        Trim             = true;
        ExistingMetadata = false;
        Retries          = 5;
        Skipped          = 512;

        MediaType mediaType;

        if(scsiInfo != null)
            mediaType = scsiInfo.MediaType;
        else
        {
            switch(deviceInfo.Type)
            {
                case DeviceType.SecureDigital:
                    mediaType = MediaType.SecureDigital;

                    break;
                case DeviceType.MMC:
                    mediaType = MediaType.MMC;

                    break;
                default:
                    if(deviceInfo.IsPcmcia)
                        mediaType = MediaType.PCCardTypeII;
                    else if(deviceInfo.IsCompactFlash)
                        mediaType = MediaType.CompactFlash;
                    else
                        mediaType = MediaType.GENERIC_HDD;

                    break;
            }
        }

        PluginRegister plugins = PluginRegister.Singleton;

        foreach(IWritableImage plugin in plugins.WritableImages.Values)
        {
            if(plugin is null) continue;

            if(plugin.SupportedMediaTypes.Contains(mediaType))
            {
                PluginsList.Add(new ImagePluginModel
                {
                    Plugin = plugin
                });
            }
        }

        Encodings.AddRange(Encoding.GetEncodings()
                                   .Select(info => new EncodingModel
                                    {
                                        Name        = info.Name,
                                        DisplayName = info.GetEncoding().EncodingName
                                    }));

        Encodings.AddRange(Claunia.Encoding.Encoding.GetEncodings()
                                  .Select(info => new EncodingModel
                                   {
                                       Name        = info.Name,
                                       DisplayName = info.DisplayName
                                   }));

        Track1PregapVisible = mediaType switch
                              {
                                  MediaType.CD
                                   or MediaType.CDDA
                                   or MediaType.CDG
                                   or MediaType.CDEG
                                   or MediaType.CDI
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
                                   or MediaType.DDCD
                                   or MediaType.DDCDR
                                   or MediaType.DDCDRW
                                   or MediaType.DTSCD
                                   or MediaType.CDMIDI
                                   or MediaType.CDV
                                   or MediaType.CDIREADY
                                   or MediaType.FMTOWNS
                                   or MediaType.PS1CD
                                   or MediaType.PS2CD
                                   or MediaType.MEGACD
                                   or MediaType.SATURNCD
                                   or MediaType.GDROM
                                   or MediaType.GDR
                                   or MediaType.MilCD
                                   or MediaType.SuperCDROM2
                                   or MediaType.JaguarCD
                                   or MediaType.ThreeDO
                                   or MediaType.PCFX
                                   or MediaType.NeoGeoCD
                                   or MediaType.CDTV
                                   or MediaType.CD32
                                   or MediaType.Playdia
                                   or MediaType.Pippin
                                   or MediaType.VideoNow
                                   or MediaType.VideoNowColor
                                   or MediaType.VideoNowXp
                                   or MediaType.CVD => true,
                                  _ => false
                              };

        _devicePath = devicePath;
    }

    public string OutputFormatLabel     => UI.Output_format;
    public string ChooseLabel           => UI.ButtonLabel_Choose;
    public string StopOnErrorLabel      => UI.Stop_media_dump_on_first_error;
    public string ForceLabel            => UI.Continue_dumping_whatever_happens;
    public string RetriesLabel          => UI.Retry_passes;
    public string PersistentLabel       => UI.Try_to_recover_partial_or_incorrect_data;
    public string ResumeLabel           => UI.Create_or_use_resume_mapfile;
    public string Track1PregapLabel     => UI.Try_to_read_track_1_pregap;
    public string SkippedLabel          => UI.Skipped_sectors_on_error;
    public string SidecarLabel          => UI.Create_Aaru_Metadata_sidecar;
    public string TrimLabel             => UI.Trim_errors_from_skipped_sectors;
    public string ExistingMetadataLabel => UI.Take_metadata_from_existing_CICM_XML_sidecar;
    public string EncodingLabel         => UI.Encoding_to_use_on_metadata_sidecar_creation;
    public string DestinationLabel      => UI.Writing_image_to;
    public string LogLabel              => UI.Title_Log;
    public string StartLabel            => UI.ButtonLabel_Start;
    public string CloseLabel            => UI.ButtonLabel_Close;
    public string StopLabel             => UI.ButtonLabel_Stop;

    public ReactiveCommand<Unit, Unit> StartCommand       { get; }
    public ReactiveCommand<Unit, Unit> CloseCommand       { get; }
    public ReactiveCommand<Unit, Unit> StopCommand        { get; }
    public ReactiveCommand<Unit, Task> DestinationCommand { get; }

    public ObservableCollection<ImagePluginModel> PluginsList { get; }
    public ObservableCollection<EncodingModel>    Encodings   { get; }

    public string Title { get; }

    public bool OptionsVisible
    {
        get => _optionsVisible;
        set => this.RaiseAndSetIfChanged(ref _optionsVisible, value);
    }

    public ImagePluginModel SelectedPlugin
    {
        get => _selectedPlugin;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedPlugin, value);

            Destination = "";

            if(value is null)
            {
                DestinationEnabled = false;

                return;
            }

            DestinationEnabled = true;

            if(!value.Plugin.SupportedOptions.Any())
            {
                // Hide options
            }

            /* TODO: Plugin options
            grpOptions.Visible = true;

            var stkOptions = new StackLayout
            {
                Orientation = Orientation.Vertical
            };

            foreach((string name, Type type, string description, object @default) option in plugin.SupportedOptions)
                switch(option.type.ToString())
                {
                    case "System.Boolean":
                        var optBoolean = new CheckBox();
                        optBoolean.ID      = "opt" + option.name;
                        optBoolean.Text    = option.description;
                        optBoolean.Checked = (bool)option.@default;
                        stkOptions.Items.Add(optBoolean);

                        break;
                    case "System.SByte":
                    case "System.Int16":
                    case "System.Int32":
                    case "System.Int64":
                        var stkNumber = new StackLayout();
                        stkNumber.Orientation = Orientation.Horizontal;
                        var optNumber = new NumericStepper();
                        optNumber.ID    = "opt" + option.name;
                        optNumber.Value = Convert.ToDouble(option.@default);
                        stkNumber.Items.Add(optNumber);
                        var lblNumber = new Label();
                        lblNumber.Text = option.description;
                        stkNumber.Items.Add(lblNumber);
                        stkOptions.Items.Add(stkNumber);

                        break;
                    case "System.Byte":
                    case "System.UInt16":
                    case "System.UInt32":
                    case "System.UInt64":
                        var stkUnsigned = new StackLayout();
                        stkUnsigned.Orientation = Orientation.Horizontal;
                        var optUnsigned = new NumericStepper();
                        optUnsigned.ID       = "opt" + option.name;
                        optUnsigned.MinValue = 0;
                        optUnsigned.Value    = Convert.ToDouble(option.@default);
                        stkUnsigned.Items.Add(optUnsigned);
                        var lblUnsigned = new Label();
                        lblUnsigned.Text = option.description;
                        stkUnsigned.Items.Add(lblUnsigned);
                        stkOptions.Items.Add(stkUnsigned);

                        break;
                    case "System.Single":
                    case "System.Double":
                        var stkFloat = new StackLayout();
                        stkFloat.Orientation = Orientation.Horizontal;
                        var optFloat = new NumericStepper();
                        optFloat.ID            = "opt" + option.name;
                        optFloat.DecimalPlaces = 2;
                        optFloat.Value         = Convert.ToDouble(option.@default);
                        stkFloat.Items.Add(optFloat);
                        var lblFloat = new Label();
                        lblFloat.Text = option.description;
                        stkFloat.Items.Add(lblFloat);
                        stkOptions.Items.Add(stkFloat);

                        break;
                    case "System.Guid":
                        // TODO
                        break;
                    case "System.String":
                        var stkString = new StackLayout();
                        stkString.Orientation = Orientation.Horizontal;
                        var lblString = new Label();
                        lblString.Text = option.description;
                        stkString.Items.Add(lblString);
                        var optString = new TextBox();
                        optString.ID   = "opt" + option.name;
                        optString.Text = (string)option.@default;
                        stkString.Items.Add(optString);
                        stkOptions.Items.Add(stkString);

                        break;
                }

            grpOptions.Content = stkOptions;
*/
        }
    }

    public string FormatReadOnly
    {
        get => _formatReadOnly;
        set => this.RaiseAndSetIfChanged(ref _formatReadOnly, value);
    }

    public string Destination
    {
        get => _destination;
        set => this.RaiseAndSetIfChanged(ref _destination, value);
    }

    public bool DestinationEnabled
    {
        get => _destinationEnabled;
        set => this.RaiseAndSetIfChanged(ref _destinationEnabled, value);
    }

    public bool StopOnError
    {
        get => _stopOnError;
        set => this.RaiseAndSetIfChanged(ref _stopOnError, value);
    }

    public bool Force
    {
        get => _force;
        set => this.RaiseAndSetIfChanged(ref _force, value);
    }

    public double Retries
    {
        get => _retries;
        set => this.RaiseAndSetIfChanged(ref _retries, value);
    }

    public bool Persistent
    {
        get => _persistent;
        set => this.RaiseAndSetIfChanged(ref _persistent, value);
    }

    public bool Resume
    {
        get => _useResume;
        set
        {
            this.RaiseAndSetIfChanged(ref _useResume, value);

            if(!value) return;

            if(_outputPrefix != null) CheckResumeFile().GetAwaiter().GetResult();
        }
    }

    public bool Track1Pregap
    {
        get => _track1Pregap;
        set => this.RaiseAndSetIfChanged(ref _track1Pregap, value);
    }

    public bool Track1PregapVisible
    {
        get => _track1PregapVisible;
        set => this.RaiseAndSetIfChanged(ref _track1PregapVisible, value);
    }

    public double Skipped
    {
        get => _skipped;
        set => this.RaiseAndSetIfChanged(ref _skipped, value);
    }

    public bool Sidecar
    {
        get => _useSidecar;
        set
        {
            this.RaiseAndSetIfChanged(ref _useSidecar, value);
            EncodingVisible = value;
        }
    }

    public bool EncodingVisible
    {
        get => _encodingVisible;
        set => this.RaiseAndSetIfChanged(ref _encodingVisible, value);
    }

    public bool Trim
    {
        get => _trim;
        set => this.RaiseAndSetIfChanged(ref _trim, value);
    }

    public bool ExistingMetadata
    {
        get => _existingMetadata;
        set
        {
            this.RaiseAndSetIfChanged(ref _existingMetadata, value);

            if(value == false)
            {
                _sidecar = null;

                return;
            }

            IReadOnlyList<IStorageFile> result = _view.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                                                       {
                                                           Title         = UI.Dialog_Choose_existing_metadata_sidecar,
                                                           AllowMultiple = false,
                                                           FileTypeFilter = new List<FilePickerFileType>
                                                           {
                                                               FilePickerFileTypes.AaruMetadata
                                                           }
                                                       })
                                                      .Result;

            if(result.Count != 1)
            {
                ExistingMetadata = false;

                return;
            }

            try
            {
                var fs = new FileStream(result[0].Path.AbsolutePath, FileMode.Open);

                _sidecar =
                    (JsonSerializer.Deserialize(fs, typeof(MetadataJson), MetadataJsonContext.Default) as MetadataJson)
                  ?.AaruMetadata;

                fs.Close();
            }
            catch
            {
                // ReSharper disable AssignmentIsFullyDiscarded
                _ = MessageBoxManager.

                    // ReSharper restore AssignmentIsFullyDiscarded
                    GetMessageBoxStandard(UI.Title_Error, UI.Incorrect_metadata_sidecar_file, ButtonEnum.Ok, Icon.Error)
                   .ShowWindowDialogAsync(_view)
                   .Result;

                ExistingMetadata = false;
            }
        }
    }

    public EncodingModel SelectedEncoding
    {
        get => _selectedEncoding;
        set => this.RaiseAndSetIfChanged(ref _selectedEncoding, value);
    }

    public string EncodingEnabled
    {
        get => _encodingEnabled;
        set => this.RaiseAndSetIfChanged(ref _encodingEnabled, value);
    }

    public bool ProgressVisible
    {
        get => _progressVisible;
        set => this.RaiseAndSetIfChanged(ref _progressVisible, value);
    }

    public string Log
    {
        get => _log;
        set => this.RaiseAndSetIfChanged(ref _log, value);
    }

    public bool Progress1Visible
    {
        get => _progress1Visible;
        set => this.RaiseAndSetIfChanged(ref _progress1Visible, value);
    }

    public string ProgressText
    {
        get => _progressText;
        set => this.RaiseAndSetIfChanged(ref _progressText, value);
    }

    public double ProgressValue
    {
        get => _progressValue;
        set => this.RaiseAndSetIfChanged(ref _progressValue, value);
    }

    public double ProgressMaxValue
    {
        get => _progressMaxValue;
        set => this.RaiseAndSetIfChanged(ref _progressMaxValue, value);
    }

    public bool ProgressIndeterminate
    {
        get => _progressIndeterminate;
        set => this.RaiseAndSetIfChanged(ref _progressIndeterminate, value);
    }

    public bool Progress2Visible
    {
        get => _progress2Visible;
        set => this.RaiseAndSetIfChanged(ref _progress2Visible, value);
    }

    public string Progress2Text
    {
        get => _progress2Text;
        set => this.RaiseAndSetIfChanged(ref _progress2Text, value);
    }

    public double Progress2Value
    {
        get => _progress2Value;
        set => this.RaiseAndSetIfChanged(ref _progress2Value, value);
    }

    public double Progress2MaxValue
    {
        get => _progress2MaxValue;
        set => this.RaiseAndSetIfChanged(ref _progress2MaxValue, value);
    }

    public bool Progress2Indeterminate
    {
        get => _progress2Indeterminate;
        set => this.RaiseAndSetIfChanged(ref _progress2Indeterminate, value);
    }

    public bool StartVisible
    {
        get => _startVisible;
        set => this.RaiseAndSetIfChanged(ref _startVisible, value);
    }

    public bool CloseVisible
    {
        get => _closeVisible;
        set => this.RaiseAndSetIfChanged(ref _closeVisible, value);
    }

    public bool StopVisible
    {
        get => _stopVisible;
        set => this.RaiseAndSetIfChanged(ref _stopVisible, value);
    }

    public bool StopEnabled
    {
        get => _stopEnabled;
        set => this.RaiseAndSetIfChanged(ref _stopEnabled, value);
    }

    async Task ExecuteDestinationCommand()
    {
        if(SelectedPlugin is null) return;

        IStorageFile result = await _view.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = UI.Dialog_Choose_destination_file,
            FileTypeChoices = new List<FilePickerFileType>
            {
                new(SelectedPlugin.Plugin.Name)
                {
                    Patterns = SelectedPlugin.Plugin.KnownExtensions.ToList()
                }
            }
        });

        if(result is null)
        {
            Destination   = "";
            _outputPrefix = null;

            return;
        }

        Destination = result.Path.AbsolutePath;

        _outputPrefix = Path.Combine(Path.GetDirectoryName(Destination) ?? "",
                                     Path.GetFileNameWithoutExtension(Destination));

        if(string.IsNullOrEmpty(Path.GetExtension(Destination)))
            Destination += SelectedPlugin.Plugin.KnownExtensions.First();

        Resume = true;
    }

    async Task CheckResumeFile()
    {
        _resume = null;

        try
        {
            if(File.Exists(_outputPrefix + ".resume.json"))
            {
                var fs = new FileStream(_outputPrefix + ".resume.json", FileMode.Open);

                _resume =
                    (await JsonSerializer.DeserializeAsync(fs, typeof(ResumeJson), ResumeJsonContext.Default) as
                         ResumeJson)?.Resume;

                fs.Close();
            }

            // DEPRECATED: To be removed in Aaru 7
            else if(File.Exists(_outputPrefix + ".resume.xml"))
            {
                // Should be covered by virtue of being the same exact class as the JSON above
#pragma warning disable IL2026
                var xs = new XmlSerializer(typeof(Resume));
#pragma warning restore IL2026

                var sr = new StreamReader(_outputPrefix + ".resume.xml");

                // Should be covered by virtue of being the same exact class as the JSON above
#pragma warning disable IL2026
                _resume = (Resume)xs.Deserialize(sr);
#pragma warning restore IL2026

                sr.Close();
            }
        }
        catch
        {
            await MessageBoxManager
                 .GetMessageBoxStandard(UI.Title_Error,
                                        UI.Incorrect_resume_file_cannot_use_it,
                                        ButtonEnum.Ok,
                                        Icon.Error)
                 .ShowWindowDialogAsync(_view);

            Resume = false;

            return;
        }

        if(_resume == null || _resume.NextBlock <= _resume.LastBlock || _resume.BadBlocks.Count != 0 && !_resume.Tape)
            return;

        await MessageBoxManager
             .GetMessageBoxStandard(UI.Title_Warning,
                                    UI.Media_already_dumped_correctly_please_choose_another_destination,
                                    ButtonEnum.Ok,
                                    Icon.Warning)
             .ShowWindowDialogAsync(_view);

        Resume = false;
    }

    void ExecuteCloseCommand() => _view.Close();

    internal void ExecuteStopCommand()
    {
        StopEnabled = false;
        _dumper?.Abort();
    }

    void ExecuteStartCommand()
    {
        Log                = "";
        CloseVisible       = false;
        StartVisible       = false;
        StopVisible        = true;
        StopEnabled        = true;
        ProgressVisible    = true;
        DestinationEnabled = false;
        OptionsVisible     = false;

        UpdateStatus(UI.Opening_device);

        _dev = Device.Create(_devicePath, out ErrorNumber devErrno);

        switch(_dev)
        {
            case null:
                StoppingErrorMessage(string.Format(UI.Error_0_opening_device, devErrno));

                return;
            case Devices.Remote.Device remoteDev:
                Statistics.AddRemote(remoteDev.RemoteApplication,
                                     remoteDev.RemoteVersion,
                                     remoteDev.RemoteOperatingSystem,
                                     remoteDev.RemoteOperatingSystemVersion,
                                     remoteDev.RemoteArchitecture);

                break;
        }

        if(_dev.Error)
        {
            StoppingErrorMessage(string.Format(UI.Error_0_opening_device, _dev.LastError));

            return;
        }

        Statistics.AddDevice(_dev);
        Statistics.AddCommand("dump-media");

        if(SelectedPlugin is null)
        {
            StoppingErrorMessage(UI.Cannot_open_output_plugin);

            return;
        }

        Encoding encoding = null;

        if(SelectedEncoding is not null)
        {
            try
            {
                encoding = Claunia.Encoding.Encoding.GetEncoding(SelectedEncoding.Name);
            }
            catch(ArgumentException)
            {
                StoppingErrorMessage(UI.Specified_encoding_is_not_supported);

                return;
            }
        }

        Dictionary<string, string> parsedOptions = new();

        /* TODO: Options
        if(grpOptions.Content is StackLayout stkFormatOptions)
            foreach(Control option in stkFormatOptions.Children)
            {
                string value;

                switch(option)
                {
                    case CheckBox optBoolean:
                        value = optBoolean.Checked?.ToString();

                        break;
                    case NumericStepper optNumber:
                        value = optNumber.Value.ToString(CultureInfo.CurrentCulture);

                        break;
                    case TextBox optString:
                        value = optString.Text;

                        break;
                    default: continue;
                }

                string key = option.ID.Substring(3);

                parsedOptions.Add(key, value);
            }
        */

        var dumpLog = new DumpLog(_outputPrefix + ".log", _dev, false);

        dumpLog.WriteLine(UI.Output_image_format_0, SelectedPlugin.Name);

        var errorLog = new ErrorLog(_outputPrefix + ".error.log");

        _dumper = new Dump(Resume,
                           _dev,
                           _devicePath,
                           SelectedPlugin.Plugin,
                           (ushort)Retries,
                           Force,
                           false,
                           Persistent,
                           StopOnError,
                           _resume,
                           dumpLog,
                           encoding,
                           _outputPrefix,
                           Destination,
                           parsedOptions,
                           _sidecar,
                           (uint)Skipped,
                           ExistingMetadata == false,
                           Trim             == false,
                           Track1Pregap,
                           true,
                           false,
                           DumpSubchannel.Any,
                           0,
                           false,
                           false,
                           false,
                           false,
                           false,
                           true,
                           errorLog,
                           false,
                           64,
                           true,
                           true,
                           false,
                           10,
                           true,
                           1080);

        new Thread(DoWork).Start();
    }

    [SuppressMessage("ReSharper", "AsyncVoidMethod")]
    async void DoWork()
    {
        _dumper.UpdateStatus         += UpdateStatus;
        _dumper.ErrorMessage         += ErrorMessage;
        _dumper.StoppingErrorMessage += StoppingErrorMessage;
        _dumper.PulseProgress        += PulseProgress;
        _dumper.InitProgress         += InitProgress;
        _dumper.UpdateProgress       += UpdateProgress;
        _dumper.EndProgress          += EndProgress;
        _dumper.InitProgress2        += InitProgress2;
        _dumper.UpdateProgress2      += UpdateProgress2;
        _dumper.EndProgress2         += EndProgress2;

        _dumper.Start();

        _dev.Close();

        await WorkFinished();
    }

    async Task WorkFinished() => await Dispatcher.UIThread.InvokeAsync(() =>
    {
        CloseVisible     = true;
        StopVisible      = false;
        Progress1Visible = false;
        Progress2Visible = false;
    });

    [SuppressMessage("ReSharper", "AsyncVoidMethod")]
    async void EndProgress2() => await Dispatcher.UIThread.InvokeAsync(() => { Progress2Visible = false; });

    [SuppressMessage("ReSharper", "AsyncVoidMethod")]
    async void UpdateProgress2(string text, long current, long maximum) => await Dispatcher.UIThread.InvokeAsync(() =>
    {
        Progress2Text          = text;
        Progress2Indeterminate = false;

        Progress2MaxValue = maximum;
        Progress2Value    = current;
    });

    [SuppressMessage("ReSharper", "AsyncVoidMethod")]
    async void InitProgress2() => await Dispatcher.UIThread.InvokeAsync(() => { Progress2Visible = true; });

    [SuppressMessage("ReSharper", "AsyncVoidMethod")]
    async void EndProgress() => await Dispatcher.UIThread.InvokeAsync(() => { Progress1Visible = false; });

    [SuppressMessage("ReSharper", "AsyncVoidMethod")]
    async void UpdateProgress(string text, long current, long maximum) => await Dispatcher.UIThread.InvokeAsync(() =>
    {
        ProgressText          = text;
        ProgressIndeterminate = false;

        ProgressMaxValue = maximum;
        ProgressValue    = current;
    });

    [SuppressMessage("ReSharper", "AsyncVoidMethod")]
    async void InitProgress() => await Dispatcher.UIThread.InvokeAsync(() => { Progress1Visible = true; });

    [SuppressMessage("ReSharper", "AsyncVoidMethod")]
    async void PulseProgress(string text) => await Dispatcher.UIThread.InvokeAsync(() =>
    {
        ProgressText          = text;
        ProgressIndeterminate = true;
    });

    [SuppressMessage("ReSharper", "AsyncVoidMethod")]
    async void StoppingErrorMessage(string text) => await Dispatcher.UIThread.InvokeAsync(async () =>
    {
        ErrorMessage(text);

        await MessageBoxManager.GetMessageBoxStandard(UI.Title_Error, $"{text}", ButtonEnum.Ok, Icon.Error)
                               .ShowWindowDialogAsync(_view);

        await WorkFinished();
    });

    [SuppressMessage("ReSharper", "AsyncVoidMethod")]
    async void ErrorMessage(string text) => await Dispatcher.UIThread.InvokeAsync(() =>
    {
        Log += text + Environment.NewLine;
    });

    [SuppressMessage("ReSharper", "AsyncVoidMethod")]
    async void UpdateStatus(string text) => await Dispatcher.UIThread.InvokeAsync(() =>
    {
        Log += text + Environment.NewLine;
    });
}