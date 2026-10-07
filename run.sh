#!/bin/bash

# Default platform is desktop
PLATFORM=${1:-desktop}

case $PLATFORM in
    desktop)
        echo "Starting Desktop version..."
        dotnet run --project SolarGame.Desktop
        ;;
    android)
        echo "Building and starting Android version..."
        dotnet build SolarGame.Android -t:Run
        ;;
    ios)
        echo "Building and starting iOS version..."
        dotnet build SolarGame.iOS -t:Run
        ;;
    browser)
        echo "Starting Browser version..."
        dotnet run --project SolarGame.Browser
        ;;
    *)
        echo "Invalid platform: $PLATFORM"
        echo "Usage: ./run.sh [desktop|android|ios|browser]"
        exit 1
        ;;
esac
