# docker build --build-arg BASE=alpine:3.21
ARG BASE=alpine:latest
FROM ${BASE}

RUN apk update

# common
RUN apk add bash build-base cmake curl git icu lsb-release-minimal nodejs npm sudo tar wget

# sentry-native
RUN apk add curl-dev docker-cli libunwind-dev libunwind-static linux-headers openssl-dev zlib-dev xz-dev xz-static

# sentry-dotnet
RUN apk add grpc-plugins openjdk11 powershell
ENV PROTOBUF_PROTOC=/usr/bin/protoc
ENV GRPC_PROTOC_PLUGIN=/usr/bin/grpc_csharp_plugin
RUN pwsh -Command Install-Module Pester -Scope AllUsers -Force

# mono (3.22+)
RUN if ! apk add mono; then \
    sed -i.bak 's|/v3\.[0-9]\+|/edge|g' /etc/apk/repositories && \
    cat /etc/apk/repositories && \
    apk update && \
    apk add --no-cache mono && \
    mv /etc/apk/repositories.bak /etc/apk/repositories && \
    apk update; \
  fi
RUN mono --version

# runner
RUN addgroup runner
RUN adduser -S -u 1001 -h /home/runner -G runner runner
RUN mkdir -p /home/runner /__e /__w /__w/_temp /__w/_actions /__w/_tool
RUN chown -R runner:runner /home/runner /__e /__w
RUN ln -s /__w /home/runner/work
RUN echo "runner ALL=(ALL) NOPASSWD:ALL" > /etc/sudoers.d/runner
RUN chmod 0440 /etc/sudoers.d/runner
RUN chmod -R 777 /opt
RUN chmod -R 777 /usr/share
USER runner
WORKDIR /__w
ENTRYPOINT ["/bin/bash"]
