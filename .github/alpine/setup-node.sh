#!/bin/sh

# A workaround for "JavaScript Actions in Alpine containers are only supported on x64 Linux runners."
# https://github.com/actions/runner/blob/8a9b96806d12343f7d123c669e29c629138023dd/src/Runner.Worker/Handlers/StepHost.cs#L283-L290
if [ "$(uname -m)" != "x86_64" ]; then
    for node in /__e/node*; do
        mkdir -p $node/bin
        ln -s /usr/bin/node $node/bin/node
        ln -s /usr/bin/npm $node/bin/npm
    done
    sed -i 's/ID=alpine/ID=unknown/' /usr/lib/os-release
fi
