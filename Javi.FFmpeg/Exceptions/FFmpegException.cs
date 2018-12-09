using System;

namespace Javi.FFmpeg.Exceptions
{
    public class FFmpegException : Exception
    {
        public FFmpegException()
        {
        }

        public FFmpegException(string message)
            : base(message)
        {
        }

        public FFmpegException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
