#!/bin/bash

echo "Building the game for Web (WebAssembly)..."
echo "This may take a few moments on the first run."

# Remove arquivos de builds anteriores para garantir um build limpo
echo "Cleaning previous builds..."
dotnet clean SolarGame.Browser -c Release

# Build the Web version
dotnet publish SolarGame.Browser -c Release

if [ $? -eq 0 ]; then
    echo ""
    echo "=================================================="
    echo "Success!"
    
    WEB_PATH="SolarGame.Browser/bin/Release/net8.0/publish/wwwroot"
    
    echo "Your Web build was generated successfully."
    echo "The files are located at:"
    echo "./$WEB_PATH"
    echo "=================================================="
    echo "You can deploy the contents of this folder to Netlify, Vercel, or GitHub Pages."
else
    echo ""
    echo "There was a build error. Check the logs above."
fi
