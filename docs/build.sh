#!/bin/bash
set -e

if [ ! -f ./docfx/docfx.exe ]; then
    echo "Installing docfx"
    curl -sL https://github.com/dotnet/docfx/releases/download/v2.48.1/docfx.zip -o docfx.zip
    unzip -oq docfx.zip -d docfx
fi

mono ./docfx/docfx.exe docfx.json

# on Travis-CI, publish the docs
if [[ "$TRAVIS_OS_NAME" == "linux" && "$TRAVIS_PULL_REQUEST" = "false" && "$TRAVIS_BRANCH" == "master" ]]; then
  git clone https://github.com/davisp/ghp-import.git &&
  ./ghp-import/ghp_import.py -n -p -f -m "Documentation upload" -b gh-pages -r https://"$GH_TOKEN"@github.com/getsentry/sentry-dotnet.git _site &&
  echo "Uploaded documentation"
fi
