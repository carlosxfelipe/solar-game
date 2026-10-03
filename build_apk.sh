#!/bin/bash

echo "Building the game for Android (APK)..."
echo "This may take a few moments on the first run."

# Build the APK
# Using %3B instead of ; to avoid Bash/MSBuild parsing issues with semicolons
dotnet publish SolarGame.Android/SolarGame.Android.csproj \
    -c Release \
    -p:AndroidPackageFormat=apk \
    -p:AndroidUseSharedRuntime=false

if [ $? -eq 0 ]; then
    echo ""
    echo "=================================================="
    echo "Success!"
    
    # Find the generated APK
    APK_PATH=$(find SolarGame.Android/bin/Release -name "*-Signed.apk" | head -n 1)
    
    if [ -z "$APK_PATH" ]; then
        APK_PATH=$(find SolarGame.Android/bin/Release -name "*.apk" | head -n 1)
    fi
    
    # Rename and move to the root folder with a short name
    SHORT_NAME="SupermarketSimulator.apk"
    cp "$APK_PATH" "./$SHORT_NAME"
    
    echo "Your APK was generated and copied to:"
    echo "./$SHORT_NAME"
    echo "=================================================="
    echo "Copy this file to your Android phone and install it."
else
    echo ""
    echo "There was a build error. Check the logs above."
fi
