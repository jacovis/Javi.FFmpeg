# <img align="center" src="./PackageIcon.png">  Javi.FFmpeg

This [.NET standard](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) package 
provides a wrapper for [FFmpeg](https://ffmpeg.org/).<br>
FFmpeg is able to convert, decode, encode, transcode, mux, demux, stream, split and slice video and audio files
supporting pretty much any format out there.<br>
With this package using ffmpeg from your application is as simple as making a method call and use event listeners for result.

## Features
- Wraps the commandline tool ffmpeg.exe
- Provides events for progress, completion and for every line of output from the commandline.
- ffmpeg process can be cancelled from code.
    
## Getting Started

- Install package using nuget

Install Javi.FFmpeg from NuGet using the Package Manager Console with the following command

    PM> Install-Package Javi.FFmpeg

Alternatively search on [NuGet Javi.FFmpeg](https://www.nuget.org/packages/Javi.FFmpeg)

- Download a copy of FFmpeg

Since this package is only a wrapper for FFmpeg, a copy of the ffmpeg.exe application must be available. FFmpeg builds can
be downloaded using links from the [FFmpeg download site](https://ffmpeg.org/download.html).<br>
Windows builds can be downloaded from https://ffmpeg.zeranoe.com/builds/

## Samples of usage 

- [Convert a video file](#convert-a-video-file)

#### Convert a video file

Converting the format of a video file is done using the commandline  

    ffmpeg -i sample.mp4 sample.mkv

Implementing this using code from this package can be following these steps:
  
Add to the usings:

    using Javi.FFmpeg

Call the Run method in the FFmpeg class:  

    using (var ffmpeg = new FFmpeg(FFmpegFileName))
    {
        string inputFile = "Sample.mp4";
        string outputFile = "Sample.mkv";
        string commandLine = string.Format($"-i \"{inputFile}\" \"{outputFile}\"");

        ffmpeg.Run(inputFile, outputFile, commandLine);
    }

## License

This project is licensed under the [MIT License](https://github.com/jacovis/Javi.FFmpeg/blob/master/LICENSE.md).

## Acknowledgments

The code in this package is based on the work from [AydinAdn/MediaToolkit](https://github.com/AydinAdn/MediaToolkit)<br>
This code is a heavily simplified and refactored version where functions based on FFmpeg functionality have become calls to the ffmpeg 
commandline using command line options. Using google it is easy to retrieve custom command lines for use with ffmpeg, 
these google results can be easily implemented using method FFmpeg.Run.<br>
<br>
Support for retrieving meta data from media files has been deleted. If meta data retrieval is required, I 
suggest to use [Javi.MediaInfo](https://github.com/jacovis/Javi.MediaInfo). 
This package is a wrapper for [MediaInfo](https://mediaarea.net/en/MediaInfo) which provides a wealth of information 
from any video or audio file.<br>
<br>
Sample video courtesy of https://sample-videos.com/
