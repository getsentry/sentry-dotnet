#!/bin/bash
set -euo pipefail

# Install old deprecated libssl 1.x for .NET 5.0 on Linux to avoid:
# Error: 'No usable version of libssl was found'

if apk --version >/dev/null 2>&1; then
    # Alpine Linux: openssl1.1-compat from the community repo
    apk add --repository=https://dl-cdn.alpinelinux.org/alpine/v3.18/community openssl1.1-compat
elif dpkg --version >/dev/null 2>&1; then
    # Ubuntu: libssl1 from focal-security
    # https://github.com/actions/runner-images/blob/d43555be6577f2ac4e4f78bf683c520687891e1b/images/ubuntu/scripts/build/install-sqlpackage.sh#L11-L21
    if [ "$(dpkg --print-architecture)" = "arm64" ]; then
        echo "deb http://ports.ubuntu.com/ubuntu-ports focal-security main" | tee /etc/apt/sources.list.d/focal-security.list
    else
        echo "deb http://security.ubuntu.com/ubuntu focal-security main" | tee /etc/apt/sources.list.d/focal-security.list
    fi
    apt-get update
    apt-get install -y --no-install-recommends libssl1.1
    rm /etc/apt/sources.list.d/focal-security.list
    apt-get update
fi
