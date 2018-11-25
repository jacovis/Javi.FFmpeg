using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using MediaToolkit.Events;

namespace MediaToolkit.Util
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
            {Find.BitRate, new Regex(@"([0-9]*)\s*kb/s")},
            {Find.Duration, new Regex(@"Duration: ([^,]*), ")},
            {Find.ConvertProgressFrame, new Regex(@"frame=\s*([0-9]*)")},
            {Find.ConvertProgressFps, new Regex(@"fps=\s*([0-9]*\.?[0-9]*?)")},
            {Find.ConvertProgressSize, new Regex(@"size=\s*([0-9]*)kB")},
            {Find.ConvertProgressFinished, new Regex(@"(muxing overhead: )([0-9]*\.?[0-9]*)%*")},
            {Find.ConvertProgressTime, new Regex(@"time=\s*([^ ]*)")},
            {Find.ConvertProgressBitrate, new Regex(@"bitrate=\s*([0-9]*\.?[0-9]*?)kbits/s")},
            {Find.ConvertProgressSpeed, new Regex(@"speed=\s*([0-9]*\.?[0-9]*[e]*[+]*[0-9]*?)x")},
            {Find.MetaAudio, new Regex(@"(Stream\s*#[0-9]*:[0-9]*\(?[^\)]*?\)?: Audio:.*)")},
            {Find.AudioFormatHzChannel, new Regex(@"Audio:\s*([^,]*),\s([^,]*),\s([^,]*)")},
            {Find.MetaVideo, new Regex(@"(Stream\s*#[0-9]*:[0-9]*\(?[^\)]*?\)?: Video:.*)")},
            {Find.VideoFormatColorSize, new Regex(@"Video:\s*([^,]*),\s*([^,]*,?[^,]*?),?\s*(?=[0-9]*x[0-9]*)([0-9]*x[0-9]*)")},
            {Find.VideoFps, new Regex(@"([0-9\.]*)\s*tbr")}
        };

        internal enum Find
        {
            AudioFormatHzChannel,
            ConvertProgressSpeed,
            ConvertProgressBitrate,
            ConvertProgressFps,
            ConvertProgressFrame,
            ConvertProgressSize,
            ConvertProgressFinished,
            ConvertProgressTime,
            Duration,
            MetaAudio,
            MetaVideo,
            BitRate,
            VideoFormatColorSize,
            VideoFps
        }

        /// <summary>
        /// Establishes whether the data contains progress information.
        /// </summary>
        /// <param name="data">Event data from the FFmpeg console.</param>
        /// <param name="progressEventArgs">If successful, outputs a <see cref="ProgressEventArgs"/> which is generated from the data.
        /// </param>
        internal static bool IsProgressData(string data, out ProgressEventArgs progressEventArgs)
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

            progressEventArgs = new ProgressEventArgs(processedDuration, TimeSpan.Zero, frame, fps, sizeKb, bitrate, speed);

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


        /// <summary>
        ///     <para> ---- </para>
        ///     <para>Establishes whether the data indicates the conversion is complete</para>
        /// </summary>
        /// <param name="data">Event data from the FFmpeg console.</param>
        /// <param name="conversionCompleteEvent">
        ///     <para>If successful, outputs a <see cref="CompletedEventArgs"/> which is </para>
        ///     <para>generated from the data. </para>
        /// </param>
        internal static bool IsConvertCompleteData(string data, out CompletedEventArgs conversionCompleteEvent)
        {
            conversionCompleteEvent = null;

            Match matchFinished = Index[Find.ConvertProgressFinished].Match(data);

            if (!matchFinished.Success) return false;

            Double.TryParse(matchFinished.Groups[2].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double muxingOverhead);

            conversionCompleteEvent = new CompletedEventArgs(TimeSpan.Zero, muxingOverhead);

            return true;
        }
    }
}
