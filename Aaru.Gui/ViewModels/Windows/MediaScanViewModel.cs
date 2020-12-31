// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : MediaScanViewModel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI view models.
//
// --[ Description ] ----------------------------------------------------------
//
//     View model and code for the media scan window.
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
// Copyright © 2011-2021 Natalia Portillo
// ****************************************************************************/

using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading;
using Aaru.Core;
using Aaru.Core.Devices.Scanning;
using Aaru.Devices;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using MessageBox.Avalonia;
using MessageBox.Avalonia.Enums;
using OxyPlot;
using ReactiveUI;

namespace Aaru.Gui.ViewModels.Windows
{
    public sealed class MediaScanViewModel : ViewModelBase
    {
        readonly Window _view;
        string          _a;
        string          _avgSpeed;
        Color           _axesColor;
        string          _b;
        ulong           _blocks;
        ulong           _blocksToRead;
        string          _c;
        bool            _closeVisible;
        string          _d;
        string          _devicePath;
        string          _e;
        string          _f;
        Color           _lineColor;
        ScanResults     _localResults;
        string          _maxSpeed;
        double          _maxX;
        double          _maxY;
        string          _minSpeed;
        double          _minX;
        double          _minY;
        bool            _progress1Visible;
        string          _progress2Indeterminate;
        string          _progress2MaxValue;
        string          _progress2Text;
        string          _progress2Value;
        string          _progress2Visible;
        bool            _progressIndeterminate;
        double          _progressMaxValue;
        string          _progressText;
        double          _progressValue;
        bool            _progressVisible;
        bool            _resultsVisible;
        MediaScan       _scanner;
        bool            _startVisible;
        double          _stepsX;
        double          _stepsY;
        string          _stopEnabled;
        bool            _stopVisible;
        string          _totalTime;
        string          _unreadableSectors;

        public MediaScanViewModel(string devicePath, Window view)
        {
            _devicePath  = devicePath;
            _view        = view;
            StopVisible  = false;
            StartCommand = ReactiveCommand.Create(ExecuteStartCommand);
            CloseCommand = ReactiveCommand.Create(ExecuteCloseCommand);
            StopCommand  = ReactiveCommand.Create(ExecuteStopCommand);
            StartVisible = true;
            CloseVisible = true;
            BlockMapList = new ObservableCollection<(ulong block, double duration)>();
            ChartPoints  = new ObservableCollection<DataPoint>();
            StepsX       = double.NaN;
            StepsY       = double.NaN;
            AxesColor    = Colors.Black;
            LineColor    = Colors.Yellow;
        }

        public Color AxesColor
        {
            get => _axesColor;
            set => this.RaiseAndSetIfChanged(ref _axesColor, value);
        }

        public Color LineColor
        {
            get => _lineColor;
            set => this.RaiseAndSetIfChanged(ref _lineColor, value);
        }

        public ObservableCollection<(ulong block, double duration)> BlockMapList { get; }
        public ObservableCollection<DataPoint>                      ChartPoints  { get; }

        public ulong Blocks
        {
            get => _blocks;
            set => this.RaiseAndSetIfChanged(ref _blocks, value);
        }

        public string A
        {
            get => _a;
            set => this.RaiseAndSetIfChanged(ref _a, value);
        }

        public string B
        {
            get => _b;
            set => this.RaiseAndSetIfChanged(ref _b, value);
        }

        public string C
        {
            get => _c;
            set => this.RaiseAndSetIfChanged(ref _c, value);
        }

        public string D
        {
            get => _d;
            set => this.RaiseAndSetIfChanged(ref _d, value);
        }

        public string E
        {
            get => _e;
            set => this.RaiseAndSetIfChanged(ref _e, value);
        }

        public string F
        {
            get => _f;
            set => this.RaiseAndSetIfChanged(ref _f, value);
        }

        public string UnreadableSectors
        {
            get => _unreadableSectors;
            set => this.RaiseAndSetIfChanged(ref _unreadableSectors, value);
        }

        public string TotalTime
        {
            get => _totalTime;
            set => this.RaiseAndSetIfChanged(ref _totalTime, value);
        }

        public string AvgSpeed
        {
            get => _avgSpeed;
            set => this.RaiseAndSetIfChanged(ref _avgSpeed, value);
        }

        public string MaxSpeed
        {
            get => _maxSpeed;
            set => this.RaiseAndSetIfChanged(ref _maxSpeed, value);
        }

        public string MinSpeed
        {
            get => _minSpeed;
            set => this.RaiseAndSetIfChanged(ref _minSpeed, value);
        }

        public bool ProgressVisible
        {
            get => _progressVisible;
            set => this.RaiseAndSetIfChanged(ref _progressVisible, value);
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

        public double ProgressValue
        {
            get => _progressValue;
            set => this.RaiseAndSetIfChanged(ref _progressValue, value);
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

        public string StopEnabled
        {
            get => _stopEnabled;
            set => this.RaiseAndSetIfChanged(ref _stopEnabled, value);
        }

        public bool ResultsVisible
        {
            get => _resultsVisible;
            set => this.RaiseAndSetIfChanged(ref _resultsVisible, value);
        }

        public double MaxY
        {
            get => _maxY;
            set => this.RaiseAndSetIfChanged(ref _maxY, value);
        }

        public double MaxX
        {
            get => _maxX;
            set => this.RaiseAndSetIfChanged(ref _maxX, value);
        }

        public double MinY
        {
            get => _minY;
            set => this.RaiseAndSetIfChanged(ref _minY, value);
        }

        public double MinX
        {
            get => _minX;
            set => this.RaiseAndSetIfChanged(ref _minX, value);
        }

        public double StepsY
        {
            get => _stepsY;
            set => this.RaiseAndSetIfChanged(ref _stepsY, value);
        }

        public double StepsX
        {
            get => _stepsX;
            set => this.RaiseAndSetIfChanged(ref _stepsX, value);
        }

        public string Title { get; }

        public ReactiveCommand<Unit, Unit> StartCommand { get; }
        public ReactiveCommand<Unit, Unit> CloseCommand { get; }
        public ReactiveCommand<Unit, Unit> StopCommand  { get; }

        void ExecuteCloseCommand() => _view.Close();

        internal void ExecuteStopCommand() => _scanner?.Abort();

        void ExecuteStartCommand()
        {
            StopVisible     = true;
            StartVisible    = false;
            CloseVisible    = false;
            ProgressVisible = true;
            ResultsVisible  = true;
            ChartPoints.Clear();
            new Thread(DoWork).Start();
        }

        // TODO: Allow to save MHDD and ImgBurn log files
        async void DoWork()
        {
            if(_devicePath.Length == 2   &&
               _devicePath[1]     == ':' &&
               _devicePath[0]     != '/' &&
               char.IsLetter(_devicePath[0]))
                _devicePath = "\\\\.\\" + char.ToUpper(_devicePath[0]) + ':';

            var dev = new Device(_devicePath);

            if(dev.IsRemote)
                Statistics.AddRemote(dev.RemoteApplication, dev.RemoteVersion, dev.RemoteOperatingSystem,
                                     dev.RemoteOperatingSystemVersion, dev.RemoteArchitecture);

            if(dev.Error)
            {
                await MessageBoxManager.
                      GetMessageBoxStandardWindow("Error", $"Error {dev.LastError} opening device.", ButtonEnum.Ok,
                                                  Icon.Error).ShowDialog(_view);

                StopVisible     = false;
                StartVisible    = true;
                CloseVisible    = true;
                ProgressVisible = false;

                return;
            }

            Statistics.AddDevice(dev);

            _localResults                 =  new ScanResults();
            _scanner                      =  new MediaScan(null, null, _devicePath, dev, false);
            _scanner.ScanTime             += OnScanTime;
            _scanner.ScanUnreadable       += OnScanUnreadable;
            _scanner.UpdateStatus         += UpdateStatus;
            _scanner.StoppingErrorMessage += StoppingErrorMessage;
            _scanner.PulseProgress        += PulseProgress;
            _scanner.InitProgress         += InitProgress;
            _scanner.UpdateProgress       += UpdateProgress;
            _scanner.EndProgress          += EndProgress;
            _scanner.InitBlockMap         += InitBlockMap;
            _scanner.ScanSpeed            += ScanSpeed;

            ScanResults results = _scanner.Scan();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                TotalTime =
                    $"Took a total of {results.TotalTime} seconds ({results.ProcessingTime} processing commands).";

                AvgSpeed          = $"Average speed: {results.AvgSpeed:F3} MiB/sec.";
                MaxSpeed          = $"Fastest speed burst: {results.MaxSpeed:F3} MiB/sec.";
                MinSpeed          = $"Slowest speed burst: {results.MinSpeed:F3} MiB/sec.";
                A                 = $"{results.A} sectors took less than 3 ms.";
                B                 = $"{results.B} sectors took less than 10 ms but more than 3 ms.";
                C                 = $"{results.C} sectors took less than 50 ms but more than 10 ms.";
                D                 = $"{results.D} sectors took less than 150 ms but more than 50 ms.";
                E                 = $"{results.E} sectors took less than 500 ms but more than 150 ms.";
                F                 = $"{results.F} sectors took more than 500 ms.";
                UnreadableSectors = $"{results.UnreadableSectors.Count} sectors could not be read.";
            });

            // TODO: Show list of unreadable sectors
            /*
            if(results.UnreadableSectors.Count > 0)
                foreach(ulong bad in results.UnreadableSectors)
                    string.Format("Sector {0} could not be read", bad);
*/

            // TODO: Show results
            /*
            
            if(results.SeekTotal != 0 || results.SeekMin != double.MaxValue || results.SeekMax != double.MinValue)
                
                string.Format("Testing {0} seeks, longest seek took {1:F3} ms, fastest one took {2:F3} ms. ({3:F3} ms average)",
                                     results.SeekTimes, results.SeekMax, results.SeekMin, results.SeekTotal / 1000);
                                     */

            Statistics.AddCommand("media-scan");

            dev.Close();
            WorkFinished();
        }

        async void ScanSpeed(ulong sector, double currentSpeed) => await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if(ChartPoints.Count == 0)
                ChartPoints.Add(new DataPoint(0, currentSpeed));

            ChartPoints.Add(new DataPoint(sector, currentSpeed));

            if(currentSpeed > MaxY)
                MaxY = currentSpeed + (currentSpeed / 10d);
        });

        async void InitBlockMap(ulong blocks, ulong blockSize, ulong blocksToRead, ushort currentProfile) =>
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Blocks        = blocks / blocksToRead;
                _blocksToRead = blocksToRead;

                MinX = 0;
                MinY = 0;

                switch(currentProfile)
                {
                    case 0x0005: // CD and DDCD
                    case 0x0008:
                    case 0x0009:
                    case 0x000A:
                    case 0x0020:
                    case 0x0021:
                    case 0x0022:
                        if(blocks <= 360000)
                            MaxX = 360000;
                        else if(blocks <= 405000)
                            MaxX = 405000;
                        else if(blocks <= 445500)
                            MaxX = 445500;
                        else
                            MaxX = blocks;

                        StepsX = MaxX   / 10;
                        StepsY = 150    * 4;
                        MaxY   = StepsY * 12.5;

                        break;
                    case 0x0010: // DVD SL
                    case 0x0011:
                    case 0x0012:
                    case 0x0013:
                    case 0x0014:
                    case 0x0018:
                    case 0x001A:
                    case 0x001B:
                        MaxX   = 2298496;
                        StepsX = MaxX / 10;
                        StepsY = 1352.5;
                        MaxY   = StepsY * 18;

                        break;
                    case 0x0015: // DVD DL
                    case 0x0016:
                    case 0x0017:
                    case 0x002A:
                    case 0x002B:
                        MaxX   = 4173824;
                        StepsX = MaxX / 10;
                        StepsY = 1352.5;
                        MaxY   = StepsY * 18;

                        break;
                    case 0x0041:
                    case 0x0042:
                    case 0x0043:
                    case 0x0040: // BD
                        if(blocks <= 12219392)
                            MaxX = 12219392;
                        else if(blocks <= 24438784)
                            MaxX = 24438784;
                        else if(blocks <= 48878592)
                            MaxX = 48878592;
                        else if(blocks <= 62500864)
                            MaxX = 62500864;
                        else
                            MaxX = blocks;

                        StepsX = MaxX / 10;
                        StepsY = 4394.5;
                        MaxY   = StepsY * 18;

                        break;
                    case 0x0050: // HD DVD
                    case 0x0051:
                    case 0x0052:
                    case 0x0053:
                    case 0x0058:
                    case 0x005A:
                        if(blocks <= 7361599)
                            MaxX = 7361599;
                        else if(blocks <= 16305407)
                            MaxX = 16305407;
                        else
                            MaxX = blocks;

                        StepsX = MaxX / 10;
                        StepsY = 4394.5;
                        MaxY   = StepsY * 8;

                        break;
                    default:
                        MaxX   = blocks;
                        StepsX = MaxX / 10;
                        StepsY = 625;
                        MaxY   = StepsY;

                        break;
                }
            });

        async void WorkFinished() => await Dispatcher.UIThread.InvokeAsync(() =>
        {
            StopVisible     = false;
            StartVisible    = true;
            CloseVisible    = true;
            ProgressVisible = false;
        });

        async void EndProgress() => await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Progress1Visible = false;
        });

        async void UpdateProgress(string text, long current, long maximum) =>
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProgressText          = text;
                ProgressIndeterminate = false;

                ProgressMaxValue = maximum;
                ProgressValue    = current;
            });

        async void InitProgress() => await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Progress1Visible = true;
        });

        async void PulseProgress(string text) => await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ProgressText          = text;
            ProgressIndeterminate = true;
        });

        async void StoppingErrorMessage(string text) => await Dispatcher.UIThread.InvokeAsync(action: async () =>
        {
            ProgressText = text;

            await MessageBoxManager.GetMessageBoxStandardWindow("Error", $"{text}", ButtonEnum.Ok, Icon.Error).
                                    ShowDialog(_view);

            WorkFinished();
        });

        async void UpdateStatus(string text) => await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ProgressText = text;
        });

        async void OnScanUnreadable(ulong sector) => await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _localResults.Errored += _blocksToRead;
            UnreadableSectors     =  $"{_localResults.Errored} sectors could not be read.";
            BlockMapList.Add((sector / _blocksToRead, double.NaN));
        });

        async void OnScanTime(ulong sector, double duration) => await Dispatcher.UIThread.InvokeAsync(() =>
        {
            BlockMapList.Add((sector / _blocksToRead, duration));

            if(duration < 3)
                _localResults.A += _blocksToRead;
            else if(duration >= 3 &&
                    duration < 10)
                _localResults.B += _blocksToRead;
            else if(duration >= 10 &&
                    duration < 50)
                _localResults.C += _blocksToRead;
            else if(duration >= 50 &&
                    duration < 150)
                _localResults.D += _blocksToRead;
            else if(duration >= 150 &&
                    duration < 500)
                _localResults.E += _blocksToRead;
            else if(duration >= 500)
                _localResults.F += _blocksToRead;

            A = $"{_localResults.A} sectors took less than 3 ms.";
            B = $"{_localResults.B} sectors took less than 10 ms but more than 3 ms.";
            C = $"{_localResults.C} sectors took less than 50 ms but more than 10 ms.";
            D = $"{_localResults.D} sectors took less than 150 ms but more than 50 ms.";
            E = $"{_localResults.E} sectors took less than 500 ms but more than 150 ms.";
            F = $"{_localResults.F} sectors took more than 500 ms.";
        });
    }
}