using System;

namespace MediaToolkit
{
    /// <summary>
    /// Data from the running ffmpeg process.
    /// </summary>
    public class FfmpegDataEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FfmpegDataEventArgs"/> class.
        /// </summary>
        /// <param name="data">The data.</param>
        public FfmpegDataEventArgs(string data)
        {
            this.Data = data;
        }

        public string Data { get; set; }
    }
}
