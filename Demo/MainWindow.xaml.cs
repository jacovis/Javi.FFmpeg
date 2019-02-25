using Javi.FFmpeg;
using Javi.FFmpeg.Extensions;
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

        // Note that cancellation is only implemented in this demo in method ButtonConvertEAC_Click
        private CancellationTokenSource CancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ButtonRun_Click(object sender, RoutedEventArgs e)
        {
            OutputText("***Start FFmpeg");

            using (var ffmpeg = new FFmpeg(FFmpegFileName))
            {
                string inputFile = "Sample.mp4";
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

                // Grab thumbnails
                // For this sample thumbnails from 0:07:28.600 to 0:07:54.200 are grabbed every 42ms
                // the loop here assumes a typical value for framerate: (24000/1001) == 23.976 fps === 42 ms
                int startPosition = (7 * 60 + 28) * 1000 + 600; // start at 7min54sec mark, in milliseconds
                int duration = (0 * 60 + 25) * 1000 + 600; // duration to grab is 25sec and 600 milliseconds, in milliseconds
                for (int i = 0; i < (duration / 42); i++)
                {
                    OutputText(string.Format($"{i} / {(int)(duration / 42)}"));

                    TimeSpan seekPosition = TimeSpan.FromMilliseconds(startPosition + i * 42);

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

                try
                {
                    await Task.Run(() => ffmpeg.ExtractSubtitle(this.InputFile, Path.ChangeExtension(InputFile, "srt"), 0));
                }
                catch (Exception ex)
                {
                    OutputText("!!!! Exception: " + ex.Message);
                }

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

                await Task.Run(() => ffmpeg.CutMedia(this.InputFile, outputFile, TimeSpan.FromMilliseconds((7 * 60 + 28) * 1000 + 600), TimeSpan.FromMilliseconds((7 * 60 + 54) * 1000 + 200)));

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

        private async void ButtonStream_Click(object sender, RoutedEventArgs e)
        {
            string ffmpegCommand = @"ffmpeg -re -i udp://224.2.2.8:1008 -map 0:p:40 -map 0:p:29 -map 0:p:42 -map 0:p:17 -map 0:p:19 -vcodec mpeg2video -s 720x576 -r 25 -flags cgop+ilme -sc_threshold 1000000000 -b:v 2M -minrate:v 2M -maxrate:v 2M -bufsize:v 1.4M -acodec mp2 -ac 2 -b:a 192k -program title=xyz:st=0:st=1 -program title=zz:st=2:st=3 -program title=z2:st=4:st=5 -program title=SriShankara:st=6:st=7 -program title=Shubhvarta:st=8:st=9 -f mpegts udp://230.0.0.5:1008?pkt_size=1316";
            using (var ffmpeg = new FFmpeg(FFmpegFileName))
            {
                ffmpeg.OnProgress += OnProgressEvent;
                ffmpeg.OnCompleted += OnCompletedEvent;
                ffmpeg.OnData += (s, args) => { OutputText(args.Data); };

                OutputText("***Start capturing stream");

                try
                {
                    await Task.Run(() => ffmpeg.Run(ffmpegCommand, string.Empty, ffmpegCommand));
                }
                catch (FFmpegException fe)
                {
                    OutputText(fe.Message + (fe.InnerException == null ? "" : ", " + fe.InnerException.Message));
                }
                catch (Exception ex)
                {
                    OutputText(ex.Message);
                }

                OutputText("***Ready capturing stream");
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
