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
    using MediaToolkit.Exceptions;
    using MediaToolkit.Util;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Threading;

    public class Engine : IDisposable
    {
        #region Fields

        /// <summary>
        /// The standard arguments to pass to ffmpeg:
        /// -nostdin                Disable interaction on standard input.
        /// -y                      Overwrite output files without asking.
        /// -loglevel info          Set logging level
        /// </summary>
        private readonly string StandardArguments = "-nostdin -y -loglevel info ";

        /// <summary>   Full pathname of the FFmpeg file. </summary>
        protected readonly string FFMpegFilePath;

        /// <summary>   The ffmpeg process. </summary>
        protected Process FFMpegProcess;

        #endregion

        #region Events

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

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="Engine" /> class.
        /// </summary>
        /// <param name="ffmpegPath">Path to ffmpeg executable.</param>
        public Engine(string ffmpegPath)
        {
            this.FFMpegFilePath = ffmpegPath;

            if (!File.Exists(this.FFMpegFilePath))
            {
                throw new MediaToolkit.Exceptions.FfmpegNotFoundException(this.FFMpegFilePath);
            }
        }

        /// <summary>
        /// Retrieve a thumbnail image from a video file.
        /// </summary>
        /// <param name="inputFile">Video file.</param>
        /// <param name="outputFile">Image file.</param>
        /// <param name="seekPosition">The seek position.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public void GetThumbnail(string inputFile, string outputFile, TimeSpan seekPosition, CancellationToken cancellationToken = default(CancellationToken))
        {
            this.CustomCommand(inputFile,
                ((FormattableString)$"-ss {seekPosition.TotalSeconds} -i \"{inputFile}\" -vframes 1  \"{outputFile}\"").ToString(CultureInfo.InvariantCulture),
                cancellationToken);
        }

        /// <summary>
        /// Extracts the subtitle.
        /// </summary>
        /// <param name="inputFile">The input file.</param>
        /// <param name="outputFile">The output file.</param>
        /// <param name="subtitleTrack">The subtitle text stream to extract. This number is zero based. Omit to extract the first subtitle stream.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public void ExtractSubtitle(string inputFile, string outputFile, int subtitleTrack = 0, CancellationToken cancellationToken = default(CancellationToken))
        {
            this.CustomCommand(inputFile,
                string.Format($"-i \"{inputFile}\" -vn -an -map 0:s:{subtitleTrack} -c:s:0 srt \"{outputFile}\""),
                cancellationToken);
        }

        /// <summary>
        /// Cuts the media.
        /// </summary>
        /// <param name="inputFile">The input file.</param>
        /// <param name="outputFile">The output file.</param>
        /// <param name="start">The starttime.</param>
        /// <param name="end">The endtime.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public void CutMedia(string inputFile, string outputFile, TimeSpan start, TimeSpan end, CancellationToken cancellationToken = default(CancellationToken))
        {
            this.CustomCommand(inputFile,
                ((FormattableString)$"-ss {start} -to {end} -i \"{inputFile}\" -map 0:v? -c copy  -map 0:a? -c copy -map 0:s? -c copy \"{outputFile}\"").ToString(CultureInfo.InvariantCulture),
                cancellationToken);
        }

        /// <summary>
        /// Converts the audio to ac-3
        /// </summary>
        /// <param name="inputFile">The input file.</param>
        /// <param name="outputFile">The output file.</param>
        /// <param name="audioTrack">The audio track.</param>
        /// <param name="bitRate">The bit rate.</param>
        /// <param name="samplingRate">The sampling rate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public void ConvertAudioAC3(string inputFile, string outputFile, int audioTrack, int bitRate, int samplingRate, CancellationToken cancellationToken = default(CancellationToken))
        {
            this.CustomCommand(inputFile,
                string.Format($" -hwaccel auto -i \"{inputFile}\" -map {audioTrack} -c:s copy -c:v copy -c:a ac3 -b:a {bitRate}k  -ar {samplingRate} \"{outputFile}\""),
                cancellationToken);
        }

        /// <summary>
        /// Call ffmpeg using a custom command.
        /// inputFile parameter is used to force progress and complete events to fire.
        /// The ffmpegCommand must be a command line ffmpeg can process, including the input file, output file and parameters.
        /// </summary>
        /// <param name="inputFile">The input file.</param>
        /// <param name="ffmpegCommand">The ffmpeg command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">ffmpegCommand</exception>
        public void CustomCommand(string inputFile, string ffmpegCommand, CancellationToken cancellationToken = default(CancellationToken))
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

            if (!engineParameters.InputFile.StartsWith("http://") && !File.Exists(engineParameters.InputFile))
            {
                throw new FileNotFoundException("Input file not found", engineParameters.InputFile);
            }

            this.RunFFMpeg(engineParameters, cancellationToken);
        }

        #region Private method helpers

        private ProcessStartInfo GenerateStartInfo(EngineParameters engineParameters)
        {
            string arguments = CommandBuilder.Serialize(engineParameters);

            return this.GenerateStartInfo(arguments);
        }

        private ProcessStartInfo GenerateStartInfo(string arguments)
        {
            return new ProcessStartInfo
            {
                Arguments = StandardArguments + arguments,
                FileName = this.FFMpegFilePath,
                CreateNoWindow = true,
                RedirectStandardInput = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };
        }

        #endregion

        /// <summary>
        /// Starts FFmpeg process.
        /// </summary>
        /// <param name="engineParameters">The engine parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void RunFFMpeg(EngineParameters engineParameters, CancellationToken cancellationToken)
        {
            List<string> receivedMessagesLog = new List<string>();
            TimeSpan totalMediaDuration = new TimeSpan();

            ProcessStartInfo processStartInfo = engineParameters.HasCustomArguments
                                              ? this.GenerateStartInfo(engineParameters.CustomArguments)
                                              : this.GenerateStartInfo(engineParameters);

            this.OnData?.Invoke(this, new FfmpegDataEventArgs(processStartInfo.Arguments));

            using (this.FFMpegProcess = Process.Start(processStartInfo))
            {
                Exception caughtException = null;

                this.FFMpegProcess.ErrorDataReceived += (sender, received) =>
                {
                    if (received.Data == null) return;

                    try
                    {
                        receivedMessagesLog.Insert(0, received.Data);

                        this.OnData?.Invoke(this, new FfmpegDataEventArgs(received.Data));

                        if (engineParameters.InputFile != null)
                        {
                            //RegexEngine.TestVideo(received.Data, engineParameters);
                            //RegexEngine.TestAudio(received.Data, engineParameters);

                            Match matchDuration = RegexEngine.Index[RegexEngine.Find.Duration].Match(received.Data);
                            if (matchDuration.Success)
                            {
                                TimeSpan.TryParse(matchDuration.Groups[1].Value, out totalMediaDuration);
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
                            this.FFMpegProcess.Kill();
                        }
                        catch (InvalidOperationException)
                        {
                            // swallow exceptions that are thrown when killing the process, 
                            // one possible candidate is the application ending naturally before we get a chance to kill it
                        }
                    }
                };

                this.FFMpegProcess.BeginErrorReadLine();
                if (cancellationToken != null)
                {
                    while (!this.FFMpegProcess.WaitForExit(100))
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            try
                            {
                                this.FFMpegProcess.Kill();
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
                    this.FFMpegProcess.WaitForExit();
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                if (this.FFMpegProcess.ExitCode != 0 || caughtException != null)
                {
                    if (this.FFMpegProcess.ExitCode != 1)
                    {
                        throw new FFMpegException(this.FFMpegProcess.ExitCode + ": " + receivedMessagesLog[1] + receivedMessagesLog[0], caughtException);
                    }
                    else
                    {
                        throw new FFMpegException("ffmpeg exited with errorcode 1", caughtException);
                    }
                }
            }
        }

        #region IDisposable
        private bool isdisposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.isdisposed)
            {
                if (this.FFMpegProcess != null)
                {
                    this.FFMpegProcess.Dispose();
                }
                this.FFMpegProcess = null;

                isdisposed = true;
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="Engine"/> class.
        /// </summary>
        ~Engine()
        {
            Dispose(false);
        }
        #endregion

    }
}