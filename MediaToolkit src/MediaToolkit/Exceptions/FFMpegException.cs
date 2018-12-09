using System;

namespace MediaToolkit.Exceptions
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
