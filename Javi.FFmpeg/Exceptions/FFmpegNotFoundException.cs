﻿using System;

namespace Javi.FFmpeg
{
    public class FFmpegNotFoundException : Exception
    {
        public FFmpegNotFoundException()
        {
        }

        public FFmpegNotFoundException(string message)
            : base(message)
        {
        }

        public FFmpegNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
