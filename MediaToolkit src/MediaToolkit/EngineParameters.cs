namespace MediaToolkit
{
    using MediaToolkit.Options;
    using MediaToolkit.Util;

    /// -------------------------------------------------------------------------------------------------
    /// <summary>   Configures the engine to perform the correct task. </summary>
    internal class EngineParameters
    {
        internal bool HasCustomArguments
        {
            get
            {
                return !string.IsNullOrWhiteSpace(this.CustomArguments);
            }
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>   Gets or sets options for controlling the conversion. </summary>
        /// <value> Options that control the conversion. </value>
        internal ConversionOptions ConversionOptions { get; set; }

        internal string CustomArguments { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>   Gets or sets the input file. </summary>
        /// <value> The input file. </value>
        internal string InputFile { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>   Gets or sets the output file. </summary>
        /// <value> The output file. </value>
        internal string OutputFile { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>   Gets or sets the task. </summary>
        /// <value> The task. </value>
        internal FFmpegTask Task { get; set; }
    }
}