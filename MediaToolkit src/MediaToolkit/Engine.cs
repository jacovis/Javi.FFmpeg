/// <summary>
/// MediaToolkit
/// see https://github.com/AydinAdn/MediaToolkit
/// for features and samples.
/// JV Sep 2018 changes:
/// - removed the resource embedded ffmpeg.exe executable; the caller must now always specify the path to the executable
/// - deleted linux targeted code
/// - event ConversionCompleteEvent gets called now, the original source did not handle the complete event correctly
/// - added ExtractSubtitle method
/// - added CutMedia method; the standard method of cutting media resulted in reencode of the inputfile
/// </summary>
namespace MediaToolkit
{
    using MediaToolkit.Events;
    using MediaToolkit.Model;
    using MediaToolkit.Options;
    using MediaToolkit.Util;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Threading;

    public class Engine : EngineBase
    {
        /// <summary>
        /// The standard arguments to pass to ffmpeg:
        /// -nostdin                Disable interaction on standard input.
        /// -y                      Overwrite output files without asking.
        /// -loglevel info          Set logging level
        /// </summary>
        private readonly string standardArguments = "-nostdin -y -loglevel info ";

        /// <summary>
        /// Event fired for progress in ffmpeg process.
        /// </summary>
        public event EventHandler<ProgressEventArgs> OnProgress;

        /// <summary>
        /// Event fired when ffmpeg is done processing.
        /// </summary>
        public event EventHandler<CompletedEventArgs> OnCompleted;

        /// <summary>
        /// Event for every line of output from ffmpeg when processing.
        /// </summary>
        public event EventHandler<FfmpegDataEventArgs> OnData;

        /// <summary>
        /// Initializes a new instance of the <see cref="Engine" /> class.
        /// </summary>
        /// <param name="ffMpegPath"></param>
        public Engine(string ffMpegPath) : base(ffMpegPath) { }

        /// <summary>
        ///  Converts media with default options.
        /// </summary>
        /// <param name="inputFile">    Input file. </param>
        /// <param name="outputFile">   Output file. </param>
        public void Convert(MediaFile inputFile, MediaFile outputFile, CancellationToken cancellationToken = default(CancellationToken))
        {
            EngineParameters engineParams = new EngineParameters
            {
                InputFile = inputFile,
                OutputFile = outputFile,
                Task = FFmpegTask.Convert
            };

            this.FFmpegEngine(engineParams, cancellationToken);
        }

        /// <summary>
        /// Converts media with conversion options.
        /// </summary>
        /// <param name="inputFile">Input file.</param>
        /// <param name="outputFile">Output file.</param>
        /// <param name="options">Conversion options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public void Convert(MediaFile inputFile, MediaFile outputFile, ConversionOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            EngineParameters engineParams = new EngineParameters
            {
                InputFile = inputFile,
                OutputFile = outputFile,
                ConversionOptions = options,
                Task = FFmpegTask.Convert
            };

            this.FFmpegEngine(engineParams, cancellationToken);
        }

        /// <summary>
        /// Call ffmpeg using a custom command.
        /// inputFile parameter is used to force progress and complete events to fire.
        /// The ffmpegCommand must be a command line ffmpeg can process, including the input file, output file and parameters.
        /// </summary>
        /// <param name="inputFile">The input file.</param>
        /// <param name="ffmpegCommand">The ffmpeg command.</param>
        /// <exception cref="ArgumentNullException">ffmpegCommand</exception>
        public void CustomCommand(MediaFile inputFile, string ffmpegCommand, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(ffmpegCommand))
            {
                throw new ArgumentNullException("ffmpegCommand");
            }

            EngineParameters engineParameters = new EngineParameters
            {
                InputFile = inputFile,
                CustomArguments = ffmpegCommand
            };

            if (!engineParameters.InputFile.Filename.StartsWith("http://") && !File.Exists(engineParameters.InputFile.Filename))
            {
                throw new FileNotFoundException("Input file not found", engineParameters.InputFile.Filename);
            }

            this.StartFFmpegProcess(engineParameters, cancellationToken);
        }

        /// <summary>   
        /// Retrieve a thumbnail image from a video file. 
        /// </summary>
        /// <param name="inputFile">    Video file. </param>
        /// <param name="outputFile">   Image file. </param>
        /// <param name="options">      Conversion options. </param>
        public void GetThumbnail(MediaFile inputFile, MediaFile outputFile, ConversionOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            EngineParameters engineParams = new EngineParameters
            {
                InputFile = inputFile,
                OutputFile = outputFile,
                ConversionOptions = options,
                Task = FFmpegTask.GetThumbnail
            };

            this.FFmpegEngine(engineParams, cancellationToken);
        }

        /// <summary>
        /// Extracts the subtitle.
        /// </summary>
        /// <param name="inputFile">The input file.</param>
        /// <param name="outputFile">The output file.</param>
        /// <param name="subtitleTrack">The subtitle text stream to extract. This number is zero based. Omit to extract the first subtitle stream.</param>
        public void ExtractSubtitle(string inputFile, string outputFile, int subtitleTrack = 0, CancellationToken cancellationToken = default(CancellationToken))
        {

            MediaFile input = new MediaFile(inputFile);
            string ffmpegCommand = string.Format($"-i \"{input.Filename}\" -vn -an -map 0:s:{subtitleTrack} -c:s:0 srt \"{outputFile}\"");
            this.CustomCommand(input, ffmpegCommand, cancellationToken);
        }

        /// <summary>
        /// Cuts the media.
        /// </summary>
        /// <param name="inputFile">The input file.</param>
        /// <param name="outputFile">The output file.</param>
        /// <param name="start">The starttime.</param>
        /// <param name="end">The endtime.</param>
        public void CutMedia(string inputFile, string outputFile, TimeSpan start, TimeSpan end, CancellationToken cancellationToken = default(CancellationToken))
        {
            MediaFile input = new MediaFile(inputFile);
            string ffmpegCommand = string.Format($"-ss {start} -to {end} -i \"{input.Filename}\" -map 0:v? -c copy  -map 0:a? -c copy -map 0:s? -c copy \"{outputFile}\"");
            this.CustomCommand(input, ffmpegCommand, cancellationToken);
        }

        /// <summary>
        /// Converts the audio to ac-3
        /// </summary>
        /// <param name="inputFile">The input file.</param>
        /// <param name="outputFile">The output file.</param>
        /// <param name="audioTrack">The audio track.</param>
        public void ConvertAudioAC3(string inputFile, string outputFile, int audioTrack, int bitRate, int samplingRate, CancellationToken cancellationToken = default(CancellationToken))
        {
            MediaFile input = new MediaFile(inputFile);
            string ffmpegCommand = standardArguments + string.Format($" -hwaccel auto -i \"{inputFile}\" -map {audioTrack} -c:s copy -c:v copy -c:a ac3 -b:a {bitRate}k  -ar {samplingRate} \"{outputFile}\"");
            this.CustomCommand(input, ffmpegCommand, cancellationToken);
        }

        #region Private method - Helpers

        private void FFmpegEngine(EngineParameters engineParameters, CancellationToken cancellationToken)
        {
            if (!engineParameters.InputFile.Filename.StartsWith("http://") && !File.Exists(engineParameters.InputFile.Filename))
            {
                throw new FileNotFoundException("Input file not found", engineParameters.InputFile.Filename);
            }

            this.StartFFmpegProcess(engineParameters, cancellationToken);
        }

        private ProcessStartInfo GenerateStartInfo(EngineParameters engineParameters)
        {
            string arguments = CommandBuilder.Serialize(engineParameters);

            return this.GenerateStartInfo(arguments);
        }

        private ProcessStartInfo GenerateStartInfo(string arguments)
        {
            return new ProcessStartInfo
            {
                Arguments = standardArguments + arguments,
                FileName = this.FFmpegFilePath,
                CreateNoWindow = true,
                RedirectStandardInput = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };
        }

        #endregion

        /// -------------------------------------------------------------------------------------------------
        /// <summary>   Starts FFmpeg process. </summary>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the requested operation is
        ///     invalid.
        /// </exception>
        /// <exception cref="Exception">
        ///     Thrown when an exception error condition
        ///     occurs.
        /// </exception>
        /// <param name="engineParameters"> The engine parameters. </param>
        private void StartFFmpegProcess(EngineParameters engineParameters, CancellationToken cancellationToken)
        {
            List<string> receivedMessagesLog = new List<string>();
            TimeSpan totalMediaDuration = new TimeSpan();

            ProcessStartInfo processStartInfo = engineParameters.HasCustomArguments
                                              ? this.GenerateStartInfo(engineParameters.CustomArguments)
                                              : this.GenerateStartInfo(engineParameters);

            using (this.FFmpegProcess = Process.Start(processStartInfo))
            {
                Exception caughtException = null;

                this.FFmpegProcess.ErrorDataReceived += (sender, received) =>
                {
                    if (received.Data == null) return;

                    try
                    {
                        receivedMessagesLog.Insert(0, received.Data);

                        this.OnData?.Invoke(this, new FfmpegDataEventArgs(received.Data));

                        if (engineParameters.InputFile != null)
                        {
                            RegexEngine.TestVideo(received.Data, engineParameters);
                            RegexEngine.TestAudio(received.Data, engineParameters);

                            Match matchDuration = RegexEngine.Index[RegexEngine.Find.Duration].Match(received.Data);
                            if (matchDuration.Success)
                            {
                                if (engineParameters.InputFile.Metadata == null)
                                {
                                    engineParameters.InputFile.Metadata = new Metadata();
                                }

                                TimeSpan.TryParse(matchDuration.Groups[1].Value, out totalMediaDuration);
                                engineParameters.InputFile.Metadata.Duration = totalMediaDuration;
                            }
                        }

                        if (RegexEngine.IsProgressData(received.Data, out ProgressEventArgs progressEvent))
                        {
                            progressEvent.InputFile = engineParameters.InputFile;
                            progressEvent.OutputFile = engineParameters.OutputFile;
                            progressEvent.TotalDuration = totalMediaDuration;
                            this.OnProgress?.Invoke(this, progressEvent);
                        }
                        else if (RegexEngine.IsConvertCompleteData(received.Data, out CompletedEventArgs convertCompleteEvent))
                        {
                            convertCompleteEvent.InputFile = engineParameters.InputFile;
                            convertCompleteEvent.OutputFile = engineParameters.OutputFile;
                            convertCompleteEvent.TotalDuration = totalMediaDuration;
                            this.OnCompleted?.Invoke(this, convertCompleteEvent);
                        }
                    }
                    catch (Exception ex)
                    {
                        // catch the exception and kill the process since we're in a faulted state
                        caughtException = ex;

                        try
                        {
                            this.FFmpegProcess.Kill();
                        }
                        catch (InvalidOperationException)
                        {
                            // swallow exceptions that are thrown when killing the process, 
                            // one possible candidate is the application ending naturally before we get a chance to kill it
                        }
                    }
                };

                this.FFmpegProcess.BeginErrorReadLine();
                if (cancellationToken != null)
                {
                    while (!this.FFmpegProcess.WaitForExit(100))
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            try
                            {
                                this.FFmpegProcess.Kill();
                            }
                            catch (Win32Exception)
                            {
                                // The associated process could not be terminated or the process is terminating.
                            }
                        }
                    }
                }
                else
                {
                    this.FFmpegProcess.WaitForExit();
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                if ((this.FFmpegProcess.ExitCode != 0 && this.FFmpegProcess.ExitCode != 1) || caughtException != null)
                {
                    throw new FFMpegException(this.FFmpegProcess.ExitCode + ": " + receivedMessagesLog[1] + receivedMessagesLog[0], caughtException);
                }
            }
        }
    }
}