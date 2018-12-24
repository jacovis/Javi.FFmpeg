using System;

namespace Javi.FFmpeg
{
    public class FFmpegCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Raises notification once conversion is complete
        /// </summary>
        /// <param name="InputFile">The input file.</param>
        /// <param name="OutputFile">The output file.</param>
        /// <param name="totalDuration">The total duration of the original media</param>
        /// <param name="muxingOverhead">The muxing overhead.</param>
        public FFmpegCompletedEventArgs(TimeSpan totalDuration, double muxingOverhead)
        {
            TotalDuration = totalDuration;
            MuxingOverhead = muxingOverhead;
        }

        public string InputFile { get; set; }
        public string OutputFile { get; set; }
        public double MuxingOverhead { get; internal set; }
        public TimeSpan TotalDuration { get; internal set; }
    }
}