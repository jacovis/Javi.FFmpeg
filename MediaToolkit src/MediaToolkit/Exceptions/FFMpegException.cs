using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
