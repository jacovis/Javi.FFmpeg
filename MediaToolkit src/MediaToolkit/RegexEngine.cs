using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using MediaToolkit.Events;

namespace MediaToolkit
{
    /// <summary>
    ///     Contains all Regex tasks
    /// </summary>
    internal static class RegexEngine
    {
        /// <summary>
        ///     Dictionary containing every Regex test.
        /// </summary>
        internal static Dictionary<Find, Regex> Index = new Dictionary<Find, Regex>
        {
            {Find.Duration, new Regex(@"Duration: ([^,]*), ")},
            {Find.ConvertProgressFrame, new Regex(@"frame=\s*([0-9]*)")},
            {Find.ConvertProgressFps, new Regex(@"fps=\s*([0-9]*\.?[0-9]*?)")},
            {Find.ConvertProgressSize, new Regex(@"size=\s*([0-9]*)kB")},
            {Find.ConvertProgressFinished, new Regex(@"(muxing overhead: )([0-9]*\.?[0-9]*)%*")},
            {Find.ConvertProgressTime, new Regex(@"time=\s*([^ ]*)")},
            {Find.ConvertProgressBitrate, new Regex(@"bitrate=\s*([0-9]*\.?[0-9]*?)kbits/s")},
            {Find.ConvertProgressSpeed, new Regex(@"speed=\s*([0-9]*\.?[0-9]*[e]*[+]*[0-9]*?)x")},
        };

        internal enum Find
        {
            Duration,
            ConvertProgressSpeed,
            ConvertProgressBitrate,
            ConvertProgressFps,
            ConvertProgressFrame,
            ConvertProgressSize,
            ConvertProgressFinished,
            ConvertProgressTime,
        }

        /// <summary>
        /// Check if data contains the media duration. If so return this duration.
        /// </summary>
        /// <param name="data">Event data from ffmpeg.</param>
        /// <param name="mediaDuration">Duration of the media.</param>
        /// <returns></returns>
        internal static bool MediaDuration(string data, out TimeSpan mediaDuration)
        {
            bool result = false;
            mediaDuration = TimeSpan.Zero;

            Match matchDuration = RegexEngine.Index[RegexEngine.Find.Duration].Match(data);
            if (matchDuration.Success)
            {
                result = TimeSpan.TryParse(matchDuration.Groups[1].Value, out mediaDuration);
            }

            return result;
        }

        /// <summary>
        /// Establishes whether the data contains progress information.
        /// </summary>
        /// <param name="data">Event data from ffmpeg.</param>
        /// <param name="progressEventArgs">If successful, outputs a <see cref="FFmpegProgressEventArgs"/> which is generated from the data.</param>
        internal static bool IsProgressData(string data, out FFmpegProgressEventArgs progressEventArgs)
        {
            progressEventArgs = null;

            Match matchFrame = Index[Find.ConvertProgressFrame].Match(data);
            Match matchFps = Index[Find.ConvertProgressFps].Match(data);
            Match matchSize = Index[Find.ConvertProgressSize].Match(data);
            Match matchTime = Index[Find.ConvertProgressTime].Match(data);
            Match matchBitrate = Index[Find.ConvertProgressBitrate].Match(data);
            Match matchSpeed = Index[Find.ConvertProgressSpeed].Match(data);

            if (!matchSize.Success || !matchTime.Success || !matchBitrate.Success)
                return false;

            TimeSpan.TryParse(matchTime.Groups[1].Value, out TimeSpan processedDuration);

            long? frame = GetLongValue(matchFrame);
            double? fps = GetDoubleValue(matchFps);
            int? sizeKb = GetIntValue(matchSize);
            double? bitrate = GetDoubleValue(matchBitrate);
            double? speed = GetDoubleValue(matchSpeed);

            progressEventArgs = new FFmpegProgressEventArgs(processedDuration, TimeSpan.Zero, frame, fps, sizeKb, bitrate, speed);

            return true;
        }

        /// <summary>
        /// Establishes whether the data indicates the conversion is complete.
        /// </summary>
        /// <param name="data">Event data from ffmpeg.</param>
        /// <param name="conversionCompleteEvent">If successful, outputs a <see cref="FFmpegCompletedEventArgs"/> which is generated from the data.</param>
        internal static bool IsConvertCompleteData(string data, out FFmpegCompletedEventArgs conversionCompleteEvent)
        {
            conversionCompleteEvent = null;

            Match matchFinished = Index[Find.ConvertProgressFinished].Match(data);

            if (!matchFinished.Success) return false;

            Double.TryParse(matchFinished.Groups[2].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double muxingOverhead);

            conversionCompleteEvent = new FFmpegCompletedEventArgs(TimeSpan.Zero, muxingOverhead);

            return true;
        }

        private static long? GetLongValue(Match match)
        {
            try
            {
                return Convert.ToInt64(match.Groups[1].Value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        private static double? GetDoubleValue(Match match)
        {
            try
            {
                return Convert.ToDouble(match.Groups[1].Value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        private static int? GetIntValue(Match match)
        {
            try
            {
                return Convert.ToInt32(match.Groups[1].Value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }
    }
}
