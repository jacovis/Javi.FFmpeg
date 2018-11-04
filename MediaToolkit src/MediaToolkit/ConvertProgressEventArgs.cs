using MediaToolkit.Model;
using System;

namespace MediaToolkit
{
    public class ConvertProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Raises notifications on the conversion process
        /// </summary>
        /// <param name="processed">Duration of the media which has been processed</param>
        /// <param name="totalDuration">The total duration of the original media</param>
        /// <param name="frame">The specific frame the conversion process is on</param>
        /// <param name="fps">The frames converted per second</param>
        /// <param name="sizeKb">The current size in Kb of the converted media</param>
        /// <param name="bitrate">The bit rate of the converted media</param>
        /// <param name="speed">The speed.</param>
        public ConvertProgressEventArgs(TimeSpan processed, TimeSpan totalDuration, long? frame, double? fps, int? sizeKb, double? bitrate, double? speed)
        {
            TotalDuration = totalDuration;
            ProcessedDuration = processed;
            Frame = frame;
            Fps = fps;
            SizeKb = sizeKb;
            Bitrate = bitrate;
            Speed = speed;
        }

        public MediaFile InputFile { get; set; }
        public MediaFile OutputFile { get; set; }
        public long? Frame { get; private set; }
        public double? Fps { get; private set; }
        public int? SizeKb { get; private set; }
        public TimeSpan ProcessedDuration { get; private set; }
        public double? Bitrate { get; private set; }
        public double? Speed { get; private set; }
        public TimeSpan TotalDuration { get; internal set; }
    }
}