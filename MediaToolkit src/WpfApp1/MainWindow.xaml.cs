using MediaToolkit;
using MediaToolkit.Events;
using MediaToolkit.Model;
using MediaToolkit.Options;
using Microsoft.Win32;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string FfmpegFileName;

        private MediaFile InputFile;

        CancellationTokenSource CancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();

            InputFile = new MediaFile();

            this.FfmpegFileName = SelectFile("*.exe", "Select location of ffmpeg.exe");
            if (string.IsNullOrWhiteSpace(this.FfmpegFileName))
            {
                this.Close();
            }
        }

        private string SelectFile(string defaultExt = "", string title = "")
        {
            string result = string.Empty;

            OpenFileDialog ofd = new OpenFileDialog
            {
                DefaultExt = defaultExt,
                Title = title,
            };

            if (ofd.ShowDialog() == true)
            {
                result = ofd.FileName;
            }

            return result;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.CancellationTokenSource != null)
                {
                    this.CancellationTokenSource.Cancel();
                }
            }
            catch (ObjectDisposedException)
            {
                // cancel was not possible, tokensource was already disposed
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            this.TextBlockMediaInfo.Text = string.Empty;
        }

        private void GrabThumbnail()
        {
            this.InputFile.Filename = SelectFile(this.InputFile.Filename);
            if (string.IsNullOrWhiteSpace(this.InputFile.Filename)) { return; }

            // Grab thumbnail from a video
            OutputText("Start grab thumbnail");
            using (var engine = new Engine(FfmpegFileName))
            {
                engine.OnCompleted += (sender, e) => { OutputText(string.Format($"complete event: {e.MuxingOverhead} {e.TotalDuration}")); };
                //engine.OnData += (sender, e) => { OutputText(e.Data); };

                // Save thumbnails
                // For this sample thumbnails from 0:00:59 to 0:01:09 are grabbed every 42ms
                // typical value for framerate (24000/1001)=23.976 fps === 42 ms
                // ie grab images for 2 seconds = 2000 ms every 42 ms: loop getthumbnail 2000 / 42 times = 47
                for (int i = 0; i < (2 * 1000 / 42); i++)
                {
                    OutputText(i.ToString());

                    var options = new ConversionOptions { Seek = TimeSpan.FromMilliseconds((0 * 60 + 59) * 1000 + i * 42) };
                    int hours = options.Seek.Value.Hours;
                    int minutes = options.Seek.Value.Minutes;
                    int seconds = options.Seek.Value.Seconds;
                    int milliseconds = options.Seek.Value.Milliseconds;
                    string timeString = hours.ToString("D2") + "." + minutes.ToString("D2") + "." + seconds.ToString("D2") + "." + milliseconds.ToString("D3");
                    var outputFile = new MediaFile
                    {
                        Filename = Path.Combine(Path.GetDirectoryName(InputFile.Filename),
                            Path.GetFileNameWithoutExtension(InputFile.Filename) + " " + timeString + ".jpg")
                    };
                    engine.GetThumbnail(InputFile, outputFile, options);
                }
            }
            OutputText("End grab thumbnail");
        }

        private async void ButtonGrabThumbnail_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => GrabThumbnail());
        }

        private async void ButtonExtractSrt_Click(object sender, RoutedEventArgs e)
        {
            this.InputFile.Filename = SelectFile(this.InputFile.Filename);
            if (string.IsNullOrWhiteSpace(this.InputFile.Filename)) { return; }

            using (var engine = new Engine(FfmpegFileName))
            {
                engine.OnProgress += HandleProgressEvent;
                engine.OnCompleted += HandleCompleteEvent;
                engine.OnData += (s, args) => { OutputText(args.Data); };

                OutputText("start extract srt");
                await Task.Run(() => engine.ExtractSubtitle(this.InputFile.Filename, Path.ChangeExtension(InputFile.Filename, "srt"), 0));
                OutputText("ready extract srt");
                OutputText(Environment.NewLine);
                OutputText(Environment.NewLine);

            }
        }

        private async void ButtonCutVideo_Click(object sender, RoutedEventArgs e)
        {
            this.InputFile.Filename = SelectFile(this.InputFile.Filename);
            if (string.IsNullOrWhiteSpace(this.InputFile.Filename)) { return; }

            var outputFile = new MediaFile(Path.Combine(Path.GetDirectoryName(InputFile.Filename), Path.GetFileNameWithoutExtension(InputFile.Filename) + "_Cut" + Path.GetExtension(InputFile.Filename)));

            using (var engine = new Engine(FfmpegFileName))
            {
                engine.OnProgress += HandleProgressEvent;
                engine.OnCompleted += HandleCompleteEvent;
                engine.OnData += (s, args) => { OutputText(args.Data); };

                OutputText("***** start cut video");

                await Task.Run(() => engine.CutMedia(this.InputFile.Filename, outputFile.Filename, TimeSpan.FromSeconds(32 * 60 + 59), TimeSpan.FromSeconds(34 * 60 + 0)));

                OutputText("**** ready cut video");
            }
        }

        private async void ButtonConvertEAC_Click(object sender, RoutedEventArgs e)
        {
            this.InputFile.Filename = SelectFile(this.InputFile.Filename);
            if (string.IsNullOrWhiteSpace(this.InputFile.Filename)) { return; }

            var outputFile = new MediaFile(Path.Combine(Path.GetDirectoryName(InputFile.Filename), Path.GetFileNameWithoutExtension(InputFile.Filename) + "_ConvertAC3" + Path.GetExtension(InputFile.Filename)));

            using (var engine = new Engine(FfmpegFileName))
            {
                engine.OnProgress += HandleProgressEvent;
                engine.OnCompleted += HandleCompleteEvent;
                engine.OnData += (s, args) => { OutputText(args.Data); };

                OutputText("***** start convert eac");

                try
                {
                    using (this.CancellationTokenSource = new CancellationTokenSource())
                    {
                        var token = this.CancellationTokenSource.Token;
                        await Task.Run(() => engine.ConvertAudioAC3(this.InputFile.Filename, outputFile.Filename, 0, 640000, 48000, token), token);
                    }
                }
                catch (OperationCanceledException)
                {
                    OutputText("***** operation cancelled *****");
                }
                catch (FFMpegException fe)
                {
                    OutputText(fe.Message + (fe.InnerException == null ? "" : ", " + fe.InnerException.Message));
                }

                OutputText("***** ready convert eac");
            }
        }

        private void HandleProgressEvent(object sender, ProgressEventArgs e)
        {
            OutputText("ConvertProgressEvent");

            OutputText($"    Bitrate: {e.Bitrate}");
            OutputText($"    Fps: {e.Fps}");
            OutputText($"    Frame: {e.Frame}");
            OutputText($"    ProcessedDuration: {e.ProcessedDuration}");
            OutputText($"    SizeKb: {e.SizeKb}");
            OutputText($"    Speed: {e.Speed}");
            OutputText($"    TotalDuration: {e.TotalDuration}");
        }

        private void HandleCompleteEvent(object sender, CompletedEventArgs e)
        {
            OutputText("ConversionCompleteEvent");

            OutputText($"    MuxingOverhead: {e.MuxingOverhead}");
            OutputText($"    TotalDuration: {e.TotalDuration}");
        }

        private void OutputText(string text)
        {
            if (Application.Current == null) { return; }
            if (Application.Current.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(() =>
                    OutputText(text)));
            }
            else
            {
                this.TextBlockMediaInfo.Text += Environment.NewLine + text;
                this.TextBlockMediaInfo.ScrollToEnd();
            }
        }


    }
}
