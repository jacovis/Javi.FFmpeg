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

## Usage 

#### step 
text


## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

## Acknowledgments

The code in this package is based on the work from [AydinAdn/MediaToolkit](https://github.com/AydinAdn/MediaToolkit)<br>
This code is a simplified version where functions based on FFmpeg functionality have become calls to the ffmpeg 
commandline using command line options. Using google it is easy to retrieve custom command lines for use with ffmpeg, 
these google results can be easily implemented using method FFmpeg.Run.<br>
<br>
Support for retrieving meta data from media files has been deleted. If meta data retrieval is required, I 
suggest to use [Javi.MediaInfo](https://github.com/jacovis/Javi.MediaInfo). 
This package is a wrapper for [MediaInfo](https://mediaarea.net/en/MediaInfo) which provides a wealth of information 
from any video or audio file.





Licensing
---------  
- MediaToolkit is licensed under the [MIT license](https://github.com/AydinAdn/MediaToolkit/blob/master/LICENSE.md)
- MediaToolkit uses [FFmpeg](http://ffmpeg.org), a multimedia framework which is licensed under the [LGPLv2.1 license](http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html), its source can be downloaded from [here](https://github.com/AydinAdn/MediaToolkit/tree/master/FFmpeg%20src)

