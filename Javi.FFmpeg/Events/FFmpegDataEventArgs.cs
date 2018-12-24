using System;

namespace Javi.FFmpeg
{
    /// <summary>
    /// Raw data from the running ffmpeg process as output by ffmpeg.
    /// </summary>
    public class FFmpegDataEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FFmpegDataEventArgs"/> class.
        /// </summary>
        /// <param name="data">The data.</param>
        public FFmpegDataEventArgs(string data)
        {
            this.Data = data;
        }

        public string Data { get; set; }
    }
}
