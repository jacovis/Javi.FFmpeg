rem from:
rem https://docs.microsoft.com/en-us/nuget/quickstart/create-and-publish-a-package-using-visual-studio

rem ** Remove old binaries
rd /s /q .\Javi.FFmpeg\bin\Release

rem ** Build package
dotnet restore
dotnet build -c Release .\Javi.FFmpeg\Javi.FFmpeg.csproj

rem ** Push package to nuget
rem    uses API key previously added to local git config
nuget push .\Javi.FFmpeg\bin\Release\*.nupkg -Source https://api.nuget.org/v3/index.json