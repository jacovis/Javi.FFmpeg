using System;

namespace MediaToolkit.Exceptions
{
    public class FFMpegException : Exception
    {
        public FFMpegException()
        {
        }

        public FFMpegException(string message)
            : base(message)
        {
        }

        public FFMpegException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
