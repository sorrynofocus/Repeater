#!/bin/bash
# .Net Core application buildscript.
# Author: Chris Winters
# Date: June 3 2021
# 
# To build, call:
# . ./gobuild.sh
# Yes- there's two dots with space between before the file.
#TODO:
# Split dotnet builds for linux, windows, mac. All OSes are supported. Adding more? Refer to RIDs (see RIDS for platform type )
# RIDS: https://docs.microsoft.com/en-us/dotnet/core/rid-catalog#windows-rids

export CURDIR=`pwd`
PRODUCTFILE="Repeater"
PRODUCTFILEMAC="$PRODUCTFILE-mac"
PRODUCTFILEWIN="$PRODUCTFILE.exe"
SOLUTIONFILE="$PRODUCTFILE.sln"

if [ -f "$SOLUTIONFILE" ];
then
    echo "Starting $SOLUTIONFILE build..."
    dotnet clean --configuration Debug  --verbosity detailed --framework netcoreapp3.1
    dotnet clean --configuration Release --verbosity detailed --framework netcoreapp3.1

    dotnet build --framework netcoreapp3.1  -r win10-x64 --configuration Release --verbosity detailed
    dotnet publish --framework netcoreapp3.1  -r win10-x64 --configuration Release --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true --verbosity detailed

    dotnet build --framework netcoreapp3.1  -r linux-x64 --configuration Release --verbosity detailed
    dotnet publish --framework netcoreapp3.1  -r linux-x64 --configuration Release --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true --verbosity detailed

    dotnet build --framework netcoreapp3.1  -r osx.10.13-x64 --configuration Release --verbosity detailed
    dotnet publish --framework netcoreapp3.1  -r osx.10.13-x64 --configuration Release --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true --verbosity detailed

    echo "Checking following files in build..."
    echo "File: $CURDIR/$PRODUCTFILE/bin/Release/netcoreapp3.1/linux-x64/publish/$PRODUCTFILE"
    echo "File: $CURDIR/$PRODUCTFILE/bin/Release/netcoreapp3.1/osx.10.13-x64/publish/$PRODUCTFILE"
    echo "File: $CURDIR/$PRODUCTFILE/bin/Release/netcoreapp3.1/win10-x64/publish/$PRODUCTFILEWIN"        
    
    if [[ -f "$CURDIR/$PRODUCTFILE/bin/Release/netcoreapp3.1/linux-x64/publish/$PRODUCTFILE" ]] && [[ -f "$CURDIR/$PRODUCTFILE/bin/Release/netcoreapp3.1/osx.10.13-x64/publish/$PRODUCTFILE" ]] && [[ -f "$CURDIR/candle/bin/Release/netcoreapp3.1/win10-x64/publish/$PRODUCTFILEWIN" ]];
    then
        echo "Product build successful!"
    
    echo "Product build complete." >&2

    else
        echo "Product build not successful." >&2
    fi

else
   echo "The $SOLUTIONFILE does not exist. " >&2
   echo "Product build -NOT- complete." >&2
   #exit 1
fi

