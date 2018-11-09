using MediaToolkit.Model;
using MediaToolkit.Options;
using MediaToolkit.Util;
using System;
using System.Globalization;
using System.Text;

namespace MediaToolkit
{
    internal class CommandBuilder
    {
        internal static string Serialize(EngineParameters engineParameters)
        {
            switch (engineParameters.Task)
            {
                case FFmpegTask.GetThumbnail:
                    return GetThumbnail(engineParameters.InputFile, engineParameters.OutputFile, engineParameters.ConversionOptions);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static string GetThumbnail(string inputFile, string outputFile, ConversionOptions conversionOptions)
        {
            var commandBuilder = new StringBuilder();

            commandBuilder.AppendFormat(CultureInfo.InvariantCulture, " -ss {0} ", conversionOptions.Seek.GetValueOrDefault(TimeSpan.FromSeconds(1)).TotalSeconds);

            commandBuilder.AppendFormat(" -i \"{0}\" ", inputFile);
            commandBuilder.AppendFormat(" -vframes {0} ", 1);

            return commandBuilder.AppendFormat(" \"{0}\" ", outputFile).ToString();
        }
    }
}
