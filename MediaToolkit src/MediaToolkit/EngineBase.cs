namespace MediaToolkit
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.Serialization;

    public class EngineBase : IDisposable
    {
        private bool isDisposed;

        /// <summary>   Full pathname of the FFmpeg file. </summary>
        protected readonly string FFmpegFilePath;

        /// <summary>   The ffmpeg process. </summary>
        protected Process FFmpegProcess;

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///     <para> Initializes FFmpeg.exe; Ensuring that there is a copy</para>
        ///     <para> in the clients temp folder &amp; isn't in use by another process.</para>
        /// </summary>
        protected EngineBase(string ffMpegPath)
        {
            this.isDisposed = false;

            this.FFmpegFilePath = ffMpegPath;

            if (!File.Exists(this.FFmpegFilePath))
            {
                throw new FfmpegNotFoundException(this.FFmpegFilePath);
            }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting
        ///     unmanaged resources.
        /// </summary>
        /// <remarks>   Aydin Aydin, 30/03/2015. </remarks>
        public virtual void Dispose()
        {
            this.Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing || this.isDisposed)
            {
                return;
            }

            if(FFmpegProcess != null)
            {
                this.FFmpegProcess.Dispose();
            }            
            this.FFmpegProcess = null;

            this.isDisposed = true;
        }
    }

    [Serializable]
    public class FfmpegNotFoundException : Exception
    {
        // Constructors
        public FfmpegNotFoundException(string message)
            : base(message)
        { }

        // Ensure Exception is Serializable
        protected FfmpegNotFoundException(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        { }
    }
}
