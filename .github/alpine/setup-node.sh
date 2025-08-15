#!/bin/sh

[ -n "$1" ] || { echo "Usage: $0 <path>"; exit 1; }

# A workaround for "JavaScript Actions in Alpine containers are only supported on x64 Linux runners."
# https://github.com/actions/runner/blob/8a9b96806d12343f7d123c669e29c629138023dd/src/Runner.Worker/Handlers/StepHost.cs#L283-L290
if [ "$(uname -m)" != "x86_64" ]; then
    mkdir -p $1
    ln -s /usr/bin/node $1
    ln -s /usr/bin/npm $1
    sed -i 's/ID=alpine/ID=unknown/' /usr/lib/os-release
fi
