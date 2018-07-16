#!/bin/bash
set -e

dotnet test -c Release /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Exclude="[Sentry.Test*]*"

## Docs
# Build
pushd docs
curl -L https://github.com/dotnet/docfx/releases/download/v2.37/docfx.zip -o docfx.zip
unzip docfx.zip -d docfx
mono ./docfx/docfx.exe docfx.json
# Publish
if [[ "$TRAVIS_OS_NAME" == "linux" && "$TRAVIS_PULL_REQUEST" = "false" && "$TRAVIS_BRANCH" == "master" ]]; then
  git clone https://github.com/davisp/ghp-import.git &&
  ./ghp-import/ghp_import.py -n -p -f -m "Documentation upload" -b gh-pages -r https://"$GH_TOKEN"@github.com/getsentry/sentry-dotnet.git _site &&
  echo "Uploaded documentation"
fi
popd
## Done with docs
