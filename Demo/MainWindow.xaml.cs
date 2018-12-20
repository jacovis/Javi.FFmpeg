using Javi.FFmpeg;
using Javi.FFmpeg.Events;
using Javi.FFmpeg.Exceptions;
using Microsoft.Win32;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Set this to the location of ffmpeg.exe on your machine:
        private string FFmpegFileName = @"D:\Projecten\Tools\ffmpeg\Executable\bin\ffmpeg.exe";

        private string InputFile;

        private CancellationTokenSource CancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            OutputText("***Start FFmpeg");

            using (var ffmpeg = new FFmpeg(FFmpegFileName))
            {
                string inputFile = "Sample.avi";
                string outputFile = "Sample.mkv";
                string commandLine = string.Format($"-i \"{inputFile}\" \"{outputFile}\"");

                ffmpeg.Run(inputFile, outputFile, commandLine);
            }

            OutputText("***End FFmpeg");
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
            this.InputFile = SelectFile(this.InputFile);
            if (string.IsNullOrWhiteSpace(this.InputFile)) { return; }

            OutputText("***Start grab thumbnail");

            using (var ffmpeg = new FFmpeg(FFmpegFileName))
            {
                ffmpeg.OnCompleted += (sender, e) => { OutputText(string.Format($"complete event: {e.MuxingOverhead} {e.TotalDuration}")); };
                ffmpeg.OnData += (sender, e) => { OutputText(e.Data); };

                // Grab thumbnails
                // For this sample thumbnails from 0:00:59 to 0:01:09 are grabbed every 42ms
                // typical value for framerate (24000/1001) == 23.976 fps === 42 ms
                // ie grab images for 2 seconds = 2000 ms every 42 ms: do GetThumbnail in a loop for 2000 / 42 = 47 times
                for (int i = 0; i < (2 * 1000 / 42); i++)
                {
                    OutputText(i.ToString());

                    TimeSpan seekPosition = TimeSpan.FromMilliseconds((0 * 60 + 59) * 1000 + i * 42);

                    int hours = seekPosition.Hours;
                    int minutes = seekPosition.Minutes;
                    int seconds = seekPosition.Seconds;
                    int milliseconds = seekPosition.Milliseconds;
                    string timeString = hours.ToString("D2") + "." + minutes.ToString("D2") + "." + seconds.ToString("D2") + "." + milliseconds.ToString("D3");
                    string outputFile = Path.Combine(Path.GetDirectoryName(InputFile), Path.GetFileNameWithoutExtension(InputFile) + " " + timeString + ".jpg");

                    ffmpeg.GetThumbnail(InputFile, outputFile, seekPosition);
                }
            }

            OutputText("***End grab thumbnail");
        }

        private async void ButtonGrabThumbnail_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => GrabThumbnail());
        }

        private async void ButtonExtractSrt_Click(object sender, RoutedEventArgs e)
        {
            this.InputFile = SelectFile(this.InputFile);
            if (string.IsNullOrWhiteSpace(this.InputFile)) { return; }

            using (var ffmpeg = new FFmpeg(FFmpegFileName))
            {
                ffmpeg.OnProgress += OnProgressEvent;
                ffmpeg.OnCompleted += OnCompletedEvent;
                ffmpeg.OnData += (s, args) => { OutputText(args.Data); };

                OutputText("***Start extract srt");

                await Task.Run(() => ffmpeg.ExtractSubtitle(this.InputFile, Path.ChangeExtension(InputFile, "srt"), 0));

                OutputText("***Ready extract srt");

            }
        }

        private async void ButtonCutVideo_Click(object sender, RoutedEventArgs e)
        {
            this.InputFile = SelectFile(this.InputFile);
            if (string.IsNullOrWhiteSpace(this.InputFile)) { return; }

            string outputFile = Path.Combine(Path.GetDirectoryName(InputFile), Path.GetFileNameWithoutExtension(InputFile) + "_Cut" + Path.GetExtension(InputFile));

            using (var ffmpeg = new FFmpeg(FFmpegFileName))
            {
                ffmpeg.OnProgress += OnProgressEvent;
                ffmpeg.OnCompleted += OnCompletedEvent;
                ffmpeg.OnData += (s, args) => { OutputText(args.Data); };

                OutputText("***Start cut video");

                // For this sample, cut from the selected file from 00:32:59 to 00:34:00
                await Task.Run(() => ffmpeg.CutMedia(this.InputFile, outputFile, TimeSpan.FromSeconds(32 * 60 + 59), TimeSpan.FromSeconds(34 * 60 + 0)));

                OutputText("***Ready cut video");
            }
        }

        private async void ButtonConvertEAC_Click(object sender, RoutedEventArgs e)
        {
            this.InputFile = SelectFile(this.InputFile);
            if (string.IsNullOrWhiteSpace(this.InputFile)) { return; }

            var outputFile = Path.Combine(Path.GetDirectoryName(InputFile), Path.GetFileNameWithoutExtension(InputFile) + "_ConvertAC3" + Path.GetExtension(InputFile));

            using (var ffmpeg = new FFmpeg(FFmpegFileName))
            {
                ffmpeg.OnProgress += OnProgressEvent;
                ffmpeg.OnCompleted += OnCompletedEvent;
                ffmpeg.OnData += (s, args) => { OutputText(args.Data); };

                OutputText("***Start convert eac");

                try
                {
                    using (this.CancellationTokenSource = new CancellationTokenSource())
                    {
                        var token = this.CancellationTokenSource.Token;
                        await Task.Run(() => ffmpeg.ConvertAudioAC3(this.InputFile, outputFile, 0, 640000, 48000, token), token);
                    }
                }
                catch (OperationCanceledException)
                {
                    OutputText("***Operation cancelled *****");
                }
                catch (FFmpegException fe)
                {
                    OutputText(fe.Message + (fe.InnerException == null ? "" : ", " + fe.InnerException.Message));
                }

                OutputText("***Ready convert eac");
            }
        }

        private void OnProgressEvent(object sender, FFmpegProgressEventArgs e)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ConvertProgressEvent");
            sb.AppendLine($"    Bitrate: {e.Bitrate}");
            sb.AppendLine($"    Fps: {e.Fps}");
            sb.AppendLine($"    Frame: {e.Frame}");
            sb.AppendLine($"    ProcessedDuration: {e.ProcessedDuration}");
            sb.AppendLine($"    SizeKb: {e.SizeKb}");
            sb.AppendLine($"    Speed: {e.Speed}");
            sb.Append($"    TotalDuration: {e.TotalDuration}");

            OutputText(sb.ToString());
        }

        private void OnCompletedEvent(object sender, FFmpegCompletedEventArgs e)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ConversionCompleteEvent");
            sb.AppendLine($"    MuxingOverhead: {e.MuxingOverhead}");
            sb.Append($"    TotalDuration: {e.TotalDuration}");

            OutputText(sb.ToString());
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
