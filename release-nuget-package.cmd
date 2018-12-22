rem https://docs.microsoft.com/en-us/nuget/quickstart/create-and-publish-a-package-using-visual-studio

rd /s /q .\Javi.FFmpeg\bin\Release
dotnet build -c Release .\Javi.FFmpeg\Javi.FFmpeg.csproj
nuget push .\Javi.FFmpeg\bin\Release\*.nupkg -Source https://api.nuget.org/v3/index.json