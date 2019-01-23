using System;
using System.Globalization;
using System.Threading;

namespace Javi.FFmpeg.Extensions
{
    public static class FFmpegExtensions
    {
        /// <summary>
        /// Extracts the subtitle.
        /// </summary>
        /// <param name="inputFile">The input file.</param>
        /// <param name="outputFile">The output file.</param>
        /// <param name="subtitleTrack">The subtitle text stream to extract. This number is zero based. Omit to extract the first subtitle stream.</param>
        /// <param name="cancellationToken">The cancellation token to cancel a running ffmpeg process.</param>
        public static void ExtractSubtitle(this FFmpeg ffmpeg, string inputFile, string outputFile, int subtitleTrack = 0, CancellationToken cancellationToken = default(CancellationToken))
        {
            ffmpeg.Run(inputFile, outputFile, string.Format($"-i \"{inputFile}\" -vn -an -map 0:s:{subtitleTrack} -c:s:0 srt \"{outputFile}\""), cancellationToken);
        }

        /// <summary>
        /// Retrieve a thumbnail image from a video file.
        /// </summary>
        /// <param name="inputFile">Video file.</param>
        /// <param name="outputFile">Image file.</param>
        /// <param name="seekPosition">The seek position.</param>
        /// <param name="cancellationToken">The cancellation token to cancel a running ffmpeg process.</param>
        public static void GetThumbnail(this FFmpeg ffmpeg, string inputFile, string outputFile, TimeSpan seekPosition, CancellationToken cancellationToken = default(CancellationToken))
        {
            ffmpeg.Run(inputFile, outputFile,
                ((FormattableString)$"-ss {seekPosition.TotalSeconds} -i \"{inputFile}\" -vframes 1  \"{outputFile}\"").ToString(CultureInfo.InvariantCulture),
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
        public static void CutMedia(this FFmpeg ffmpeg, string inputFile, string outputFile, TimeSpan start, TimeSpan end, CancellationToken cancellationToken = default(CancellationToken))
        {
            ffmpeg.Run(inputFile, outputFile,
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
        public static void ConvertAudioAC3(this FFmpeg ffmpeg, string inputFile, string outputFile, int audioTrack, int bitRate, int samplingRate, CancellationToken cancellationToken = default(CancellationToken))
        {
            ffmpeg.Run(inputFile, outputFile,
                string.Format($" -hwaccel auto -i \"{inputFile}\" -map {audioTrack} -c:s copy -c:v copy -c:a ac3 -b:a {bitRate} -ar {samplingRate} \"{outputFile}\""),
                cancellationToken);
        }

    }
}
