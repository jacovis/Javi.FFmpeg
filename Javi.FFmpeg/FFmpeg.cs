using Javi.FFmpeg.Events;
using Javi.FFmpeg.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

namespace Javi.FFmpeg
{
    public class FFmpeg : IDisposable
    {
        #region Fields

        /// <summary>
        /// The standard arguments to pass to ffmpeg:
        /// -nostdin                Disable interaction on standard input.
        /// -y                      Overwrite output files without asking.
        /// -loglevel info          Set logging level
        /// </summary>
        private readonly string StandardArguments = "-nostdin -y -loglevel info ";

        /// <summary>
        /// The ffmpeg executable full path
        /// </summary>
        protected readonly string FFmpegPath;

        /// <summary>
        /// The ffmpeg process
        /// </summary>
        protected Process FFmpegProcess;

        #endregion

        #region Events

        /// <summary>
        /// Event fired for progress in ffmpeg process.
        /// </summary>
        public event EventHandler<FFmpegProgressEventArgs> OnProgress;

        /// <summary>
        /// Event fired when ffmpeg is done processing.
        /// </summary>
        public event EventHandler<FFmpegCompletedEventArgs> OnCompleted;

        /// <summary>
        /// Event for every line of output from ffmpeg when processing.
        /// </summary>
        public event EventHandler<FFmpegDataEventArgs> OnData;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="FFmpeg" /> class.
        /// </summary>
        /// <param name="ffmpegPath">Full path to ffmpeg executable.</param>
        public FFmpeg(string ffmpegPath)
        {
            this.FFmpegPath = ffmpegPath;

            if (!File.Exists(this.FFmpegPath))
            {
                throw new FFmpegNotFoundException(this.FFmpegPath);
            }
        }

        /// <summary>
        /// Retrieve a thumbnail image from a video file.
        /// </summary>
        /// <param name="inputFile">Video file.</param>
        /// <param name="outputFile">Image file.</param>
        /// <param name="seekPosition">The seek position.</param>
        /// <param name="cancellationToken">The cancellation token to cancel a running ffmpeg process.</param>
        public void GetThumbnail(string inputFile, string outputFile, TimeSpan seekPosition, CancellationToken cancellationToken = default(CancellationToken))
        {
            this.Run(inputFile, outputFile,
                ((FormattableString)$"-ss {seekPosition.TotalSeconds} -i \"{inputFile}\" -vframes 1  \"{outputFile}\"").ToString(CultureInfo.InvariantCulture),
                cancellationToken);
        }

        /// <summary>
        /// Extracts the subtitle.
        /// </summary>
        /// <param name="inputFile">The input file.</param>
        /// <param name="outputFile">The output file.</param>
        /// <param name="subtitleTrack">The subtitle text stream to extract. This number is zero based. Omit to extract the first subtitle stream.</param>
        /// <param name="cancellationToken">The cancellation token to cancel a running ffmpeg process.</param>
        public void ExtractSubtitle(string inputFile, string outputFile, int subtitleTrack = 0, CancellationToken cancellationToken = default(CancellationToken))
        {
            this.Run(inputFile, outputFile,
                string.Format($"-i \"{inputFile}\" -vn -an -map 0:s:{subtitleTrack} -c:s:0 srt \"{outputFile}\""),
                cancellationToken);
        }

        /// <summary>
        /// Cuts the media.
        /// </summary>
        /// <param name="inputFile">The input file.</param>
        /// <param name="outputFile">The output file.</param>
        /// <param name="start">The starttime in seconds.</param>
        /// <param name="end">The endtime in seconds.</param>
        /// <param name="cancellationToken">The cancellation token to cancel a running ffmpeg process.</param>
        public void CutMedia(string inputFile, string outputFile, TimeSpan start, TimeSpan end, CancellationToken cancellationToken = default(CancellationToken))
        {
            this.Run(inputFile, outputFile,
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
        /// <param name="cancellationToken">The cancellation token to cancel a running ffmpeg process.</param>
        public void ConvertAudioAC3(string inputFile, string outputFile, int audioTrack, int bitRate, int samplingRate, CancellationToken cancellationToken = default(CancellationToken))
        {
            this.Run(inputFile, outputFile,
                string.Format($" -hwaccel auto -i \"{inputFile}\" -map {audioTrack} -c:s copy -c:v copy -c:a ac3 -b:a {bitRate}k  -ar {samplingRate} \"{outputFile}\""),
                cancellationToken);
        }

        /// <summary>
        /// Call ffmpeg using a custom command.
        /// The ffmpegCommandLine must be a command line ffmpeg can process, including the input file, output file and parameters.
        /// </summary>
        /// <param name="inputFile">The input file.</param>
        /// <param name="ffmpegCommandLine">The ffmpeg commandline parameters.</param>
        /// <param name="cancellationToken">The cancellation token to cancel a running ffmpeg process.</param>
        /// <exception cref="ArgumentNullException">When ffmpegCommand is null or whitespace or empty.</exception>
        public void Run(string inputFile, string outputFile, string ffmpegCommandLine, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(ffmpegCommandLine))
            {
                throw new ArgumentNullException("ffmpegCommand");
            }

            if (!inputFile.StartsWith("http://") && !File.Exists(inputFile))
            {
                throw new FileNotFoundException("Input file not found", inputFile);
            }

            this.FFmpegRunner(inputFile, outputFile, ffmpegCommandLine, cancellationToken);
        }
        
        private void FFmpegRunner(string inputFile, string outputFile, string ffmpegCommandLine, CancellationToken cancellationToken)
        {
            List<string> receivedMessagesLog = new List<string>();
            TimeSpan totalMediaDuration = new TimeSpan();
            bool totalMediaDurationKnown = false;
            Exception caughtException = null;

            ProcessStartInfo processStartInfo = this.GenerateStartInfo(ffmpegCommandLine);

            this.OnData?.Invoke(this, new FFmpegDataEventArgs(ffmpegCommandLine));

            using (this.FFmpegProcess = Process.Start(processStartInfo))
            {

                this.FFmpegProcess.ErrorDataReceived += (sender, received) =>
                {
                    if (received.Data == null) { return; }

                    try
                    {
                        receivedMessagesLog.Insert(0, received.Data);

                        this.OnData?.Invoke(this, new FFmpegDataEventArgs(received.Data));

                        if (!totalMediaDurationKnown)
                        {
                            totalMediaDurationKnown = RegexEngine.MediaDuration(received.Data, out totalMediaDuration);
                        }

                        if (RegexEngine.IsProgressData(received.Data, out FFmpegProgressEventArgs progressEvent))
                        {
                            progressEvent.InputFile = inputFile;
                            progressEvent.OutputFile = outputFile;
                            progressEvent.TotalDuration = totalMediaDuration;
                            this.OnProgress?.Invoke(this, progressEvent);
                        }
                        else if (RegexEngine.IsConvertCompleteData(received.Data, out FFmpegCompletedEventArgs convertCompleteEvent))
                        {
                            convertCompleteEvent.InputFile = inputFile;
                            convertCompleteEvent.OutputFile = outputFile;
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
                            // Swallow exceptions that are thrown when killing the process, ie. the application is ending naturally before we get a chance to kill it.
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

                if (this.FFmpegProcess.ExitCode != 0 || caughtException != null)
                {
                    if (this.FFmpegProcess.ExitCode != 1 && receivedMessagesLog.Count >= 2)
                    {
                        throw new FFmpegException(this.FFmpegProcess.ExitCode + ": " + receivedMessagesLog[1] + receivedMessagesLog[0], caughtException);
                    }
                    else
                    {
                        throw new FFmpegException(string.Format($"ffmpeg exited with exitcode {this.FFmpegProcess.ExitCode}"), caughtException);
                    }
                }
            }
        }

        private ProcessStartInfo GenerateStartInfo(string arguments)
        {
            return new ProcessStartInfo
            {
                Arguments = StandardArguments + arguments,
                FileName = this.FFmpegPath,
                CreateNoWindow = true,
                RedirectStandardInput = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };
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
                if (this.FFmpegProcess != null)
                {
                    this.FFmpegProcess.Dispose();
                }
                this.FFmpegProcess = null;

                isdisposed = true;
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="FFmpeg"/> class.
        /// </summary>
        ~FFmpeg()
        {
            Dispose(false);
        }
        #endregion

    }
}