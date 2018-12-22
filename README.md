# <img align="center" src="./PackageIcon.png">  Javi.FFmpeg

This [.NET standard](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) package 
provides a wrapper for [FFmpeg](https://ffmpeg.org/).<br>
FFmpeg is able to convert, decode, encode, transcode, mux, demux, stream, split and slice video and audio files
supporting pretty much any format out there.<br>
With this package, using ffmpeg from your application is as simple as making a method call and using event listeners for result.

- [Features](#features)
- [Getting Started](#getting-started)
- [Usage](#usage)
- [License](#license)
- [Acknowledgments](#acknowledgments)

## Features
- Wraps the commandline tool ffmpeg.exe
- Provides events for progress, completion and for every line of ffmpeg console output.
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

## Usage

### Convert a video file

Converting the format of a video file on the command line using FFmeg:

    ffmpeg -i sample.mp4 sample.mkv

Implementation of that same command using this package:
  
Add to usings:

    using Javi.FFmpeg

Instantiate an object of class FFmpeg, providing the full path to the local copy of the ffmpeg executable, and call the Run method:

    using (var ffmpeg = new FFmpeg(@"<path_to_your_local_copy_of_ffmpeg>"))
    {
        string inputFile = "Sample.mp4";
        string outputFile = "Sample.mkv";
        string commandLine = string.Format($"-i \"{inputFile}\" \"{outputFile}\"");

        ffmpeg.Run(inputFile, outputFile, commandLine);
    }

Since FFmpeg implements the IDisposable interface the code is wrapped in a using statement.

### Extension methods

Class FFmpegExtensions.cs provides extension methods on class FFmpeg where the FFmpeg.Run method is called with 
a commandline for specific tasks such as extracting a subtitle from a video file, extracting a thumbnail image from 
a video file or to cut a specific part out of a video or audio file. These extension methods provide examples of use. 
An application consuming this package could have its own specific extension methods implemented in a similar way.

### Demo application

A C# WPF demo application is available which uses all features of the package. Code from this demo should not be used in production code,
the code is merely to demonstrate the usage of this FFmpeg package.
    
## License

This project is licensed under the [MIT License](https://github.com/jacovis/Javi.FFmpeg/blob/master/LICENSE.md).

## Acknowledgments

The code in this package is based on the work from [AydinAdn/MediaToolkit](https://github.com/AydinAdn/MediaToolkit)<br>
This code is heavily simplified and refactored beyond recognition, where nethods based on FFmpeg functionality have become calls to the ffmpeg 
commandline using command line options. Custom command lines for use with ffmpeg are readily available on lots of support sites such as 
[stackoverflow](), [superuser](https://superuser.com/) and a multitude of [blogs](https://www.ostechnix.com/20-ffmpeg-commands-beginners/) 
using a good search query. A custom command can be easily implemented using method FFmpeg.Run.<br>
<br>
Support for retrieving meta data from media files has been deleted. If meta data retrieval is required, I 
suggest to use [Javi.MediaInfo](https://github.com/jacovis/Javi.MediaInfo). 
This package is a wrapper for [MediaInfo](https://mediaarea.net/en/MediaInfo) which provides a wealth of information 
from any video or audio file.<br>
<br>
Sample video courtesy of https://sample-videos.com/
