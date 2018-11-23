using System;

namespace MediaToolkit.Exceptions
{
    public class FfmpegNotFoundException : Exception
    {
        public FfmpegNotFoundException()
        {
        }

        public FfmpegNotFoundException(string message)
            : base(message)
        {
        }

        public FfmpegNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
