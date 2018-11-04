using MediaToolkit.Model;
using System;

namespace MediaToolkit
{
    public class ConversionCompleteEventArgs : EventArgs
    {
        /// <summary>
        /// Raises notification once conversion is complete
        /// </summary>
        /// <param name="InputFile">The input file.</param>
        /// <param name="OutputFile">The output file.</param>
        /// <param name="totalDuration">The total duration of the original media</param>
        /// <param name="muxingOverhead">The muxing overhead.</param>
        public ConversionCompleteEventArgs(TimeSpan totalDuration, double muxingOverhead)
        {
            TotalDuration = totalDuration;
            MuxingOverhead = muxingOverhead;
        }

        public MediaFile InputFile { get; set; }
        public MediaFile OutputFile { get; set; }
        public double MuxingOverhead { get; internal set; }
        public TimeSpan TotalDuration { get; internal set; }
    }
}