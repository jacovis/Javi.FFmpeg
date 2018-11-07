using MediaToolkit;
using MediaToolkit.Events;
using MediaToolkit.Model;
using MediaToolkit.Options;
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
        const string ffmpeg = @"D:\Projecten\Tools\ffmpeg\Executable\bin\ffmpeg.exe";

        //private MediaFile inputFile = new MediaFile { Filename = @"D:\Projecten\CSharp\MoSe\Sample\Suits.S08E10.720p.WEB.X264-METCON[rarbg]\Suits.S08E10.720p.WEB.X264-METCON.mkv" };

        // -ss 00:33:47 -t 00:04:58 
        //private MediaFile inputFile = new MediaFile { Filename = @"D:\Downloads\Muziek\_RADIOHEAD\20060617 Bonnaroo\mp4\Radiohead - Live at Bonnaroo Festival 2006 (Full Concert, Remastered, 60fps).mp4" };

        //private MediaFile inputFile = new MediaFile { Filename = @"C:\Users\Gebruiker\Downloads\NZB\better.call.saul.s04e08.1080p.web.x264-strife.mkv" };

        // 32:59 - 33:59
        //private MediaFile inputFile = new MediaFile { Filename = @"D:\Projecten\CSharp\MoSe\Sample\Kiss Me First S01E02\Kiss Me First S01E02 Make It Stop.mkv" };

        // multiple subs
        private MediaFile inputFile = new MediaFile { Filename = @"D:\Projecten\CSharp\MoSe\Sample\GLOW S02E10 Every Potato Has A Receipt_cut.mkv" };


        CancellationTokenSource CancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();
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
            // Grab thumbnail from a video
            OutputText("Start grab thumbnail");
            using (var engine = new Engine(ffmpeg))
            {
                engine.ConversionCompleteEvent += (sender, e) => { OutputText(string.Format($"complete event: {e.MuxingOverhead} {e.TotalDuration}")); };
                engine.FfmpegDataEvent += (sender, e) => { OutputText(e.Data); };

                // Save the frames 
                // typical value for framerate (24000/1001)=23.976 fps == 42 ms
                // ie grab images for 60 seconds = 60000 ms every 42 ms: loop getthumbnail 60000 / 42 times = 1429
                for (int i = 0; i < (60 * 1000 / 42); i++)
                {
                    OutputText(i.ToString());

                    var options = new ConversionOptions { Seek = TimeSpan.FromMilliseconds((32 * 60 + 59) * 1000 + i * 42) };
                    int hours = options.Seek.Value.Hours;
                    int minutes = options.Seek.Value.Minutes;
                    int seconds = options.Seek.Value.Seconds;
                    int milliseconds = options.Seek.Value.Milliseconds;
                    string timeString = (hours == 0 ? "" : hours.ToString("D2") + ".") + minutes.ToString("D2") + "." + seconds.ToString("D2") + "." + milliseconds.ToString("D3");
                    var outputFile = new MediaFile
                    {
                        Filename = Path.Combine(Path.GetDirectoryName(inputFile.Filename),
                        Path.GetFileNameWithoutExtension(inputFile.Filename) + " " + timeString + ".jpg")
                    };
                    engine.GetThumbnail(inputFile, outputFile, options);
                }
            }
            OutputText("End grab thumbnail");
        }

        private async void ButtonGrabThumbnail_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => GrabThumbnail());
        }

        private void ButtonRetrieveMetaData_Click(object sender, RoutedEventArgs e)
        {
            using (var engine = new Engine(ffmpeg))
            {
                engine.FfmpegDataEvent += (s, args) => { OutputText(args.Data); };
                engine.GetMetadata(inputFile);
            }

            OutputText(inputFile.Metadata.Duration.ToString());
        }

        private async void ButtonExtractSrt_Click(object sender, RoutedEventArgs e)
        {
            using (var engine = new Engine(ffmpeg))
            {
                engine.ConvertProgressEvent += HandleProgressEvent;
                engine.ConversionCompleteEvent += HandleCompleteEvent;
                engine.FfmpegDataEvent += (s, args) => { OutputText(args.Data); };

                OutputText("start extract srt");
                await Task.Run(() => engine.ExtractSubtitle(this.inputFile.Filename, Path.ChangeExtension(inputFile.Filename, "srt"), 0));
                OutputText("ready extract srt");
                OutputText(Environment.NewLine);
                OutputText(Environment.NewLine);

            }
        }

        private async void ButtonCutVideo_Click(object sender, RoutedEventArgs e)
        {
            var outputFile = new MediaFile(Path.Combine(Path.GetDirectoryName(inputFile.Filename), Path.GetFileNameWithoutExtension(inputFile.Filename) + "_Cut" + Path.GetExtension(inputFile.Filename)));

            using (var engine = new Engine(ffmpeg))
            {
                engine.ConvertProgressEvent += HandleProgressEvent;
                engine.ConversionCompleteEvent += HandleCompleteEvent;
                engine.FfmpegDataEvent += (s, args) => { OutputText(args.Data); };

                OutputText("***** start cut video");

                ////// !!let op: parameter duration is veranderd in end in method engine.CutMedia!!!!

                //// This example will create a 60 second video, starting from the 9:59
                //var options = new ConversionOptions();
                //// First parameter requests the starting frame to cut the media from.
                //// Second parameter requests how long to cut the video.
                //// LET OP!!! : dit doet een re-encode van video, audio en subtitle streams.... beter om custom command te doen
                //options.CutMedia(TimeSpan.FromSeconds(9 * 60 + 59), TimeSpan.FromSeconds(60));
                //await Task.Run(() => engine.Convert(inputFile, outputFile, options));

                //-ss 00:33:47 -t 00:04:58 -i "D:\Downloads\Muziek\_RADIOHEAD\20060617 Bonnaroo\mp4\Radiohead - Live at Bonnaroo Festival 2006 (Full Concert, Remastered, 60fps).mp4" -acodec copy -vcodec copy -scodec copy out.mp4
                //string ffmpegCommand = string.Format($"-ss {TimeSpan.FromSeconds(33 * 60 + 47)} -t {TimeSpan.FromSeconds(4 * 60 + 58)} -i \"{inputFile.Filename}\" -acodec copy -vcodec copy -scodec copy \"{outputFile.Filename}\"");
                //await Task.Run(() => engine.CustomCommand(inputFile, ffmpegCommand));

                // -map 0:v -c copy
                // voor copy alle video streams naar output; idem voor audio en subtitles
                // hiermee worden alle streams copied naar output terwijl er een stuk uit het origineel wordt geknipt.
                //string ffmpegCommand = string.Format($"-ss {TimeSpan.FromSeconds(10 * 60)} -t {TimeSpan.FromSeconds(4 * 60)} -i \"{inputFile.Filename}\" -map 0:v -c copy  -map 0:a -c copy -map 0:s -c copy \"{outputFile.Filename}\"");
                //await Task.Run(() => engine.CustomCommand(inputFile, ffmpegCommand));

                await Task.Run(() => engine.CutMedia(this.inputFile.Filename, outputFile.Filename, TimeSpan.FromSeconds(10 * 60 + 30), TimeSpan.FromSeconds(12 * 60 + 0)));

                OutputText("**** ready cut video");
            }
        }

        private async void ButtonConvertEAC_Click(object sender, RoutedEventArgs e)
        {
            var outputFile = new MediaFile(Path.Combine(Path.GetDirectoryName(inputFile.Filename), Path.GetFileNameWithoutExtension(inputFile.Filename) + "_ConvertAC3" + Path.GetExtension(inputFile.Filename)));

            using (var engine = new Engine(ffmpeg))
            {
                engine.ConvertProgressEvent += HandleProgressEvent;
                engine.ConversionCompleteEvent += HandleCompleteEvent;
                engine.FfmpegDataEvent += (s, args) => { OutputText(args.Data); };

                OutputText("***** start convert eac");

                try
                {
                    using (this.CancellationTokenSource = new CancellationTokenSource())
                    {
                        var token = this.CancellationTokenSource.Token;
                        await Task.Run(() => engine.ConvertAudioAC3(this.inputFile.Filename, outputFile.Filename, 0, 640000, 48000, token), token);
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

        private void HandleProgressEvent(object sender, ConvertProgressEventArgs e)
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

        private void HandleCompleteEvent(object sender, ConversionCompleteEventArgs e)
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
                Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(() =>
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
