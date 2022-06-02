@REM https://github.com/dotnet/sdk/issues/14281
@REM https://github.com/dotnet/designs/blob/main/accepted/2020/single-file/design.md#user-experience
@REM https://github.com/dotnet/core/issues/5409

set CURDIR=%CD%%
set PRODUCTFILE=TestSSH
set PRODUCTFILEMAC=%PRODUCTFILE%-mac
set PRODUCTFILEWIN=%PRODUCTFILE%.exe
set SOLUTIONFILE=%PRODUCTFILE%.sln

echo "Starting %SOLUTIONFILE% build..."
dotnet clean --configuration Debug  --verbosity detailed --framework net5.0
dotnet clean --configuration Release --verbosity detailed --framework net5.0

dotnet build --framework net5.0  -r win10-x64 --configuration Release --verbosity minimal
dotnet publish --framework net5.0  -r win10-x64 --configuration Release --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true --verbosity minimal

dotnet build --framework net5.0  -r linux-x64 --configuration Release --verbosity minimal
dotnet publish --framework net5.0  -r linux-x64 --configuration Release --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true --verbosity minimal

dotnet build --framework net5.0  -r osx.10.13-x64 --configuration Release --verbosity minimal
dotnet publish --framework net5.0  -r osx.10.13-x64 --configuration Release --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true --verbosity minimal

echo Product location: %CURDIR%/%PRODUCTFILE%/bin/Release/netcoreapp3.1/win10-x64/publish/%PRODUCTFILE%


