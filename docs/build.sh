#!/bin/bash
set -e

if [ ! -f ./docfx/docfx.exe ]; then
    echo "Installing docfx"
    curl -sL https://github.com/dotnet/docfx/releases/download/v2.56.4/docfx.zip -o docfx.zip
    unzip -oq docfx.zip -d docfx
fi

mono ./docfx/docfx.exe docfx.json
