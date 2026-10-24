# syntax=docker/dockerfile:1

# Contexte de build attendu : le dossier parent contenant
#   ./Sonarr/          (ce dépôt)
#   ./docker-sonarr/   (https://github.com/linuxserver/docker-sonarr, pour root/)
# En local : docker build -f Sonarr/.github/docker/Dockerfile .

############################
# Stage 1 : compilation
############################
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates gnupg \
    && curl -fsSL https://deb.nodesource.com/setup_24.x | bash - \
    && apt-get install -y nodejs \
    && npm install -g yarn \
    && rm -rf /var/lib/apt/lists/*

# Le SDK 10 compile du net6.0 : on coupe l'avertissement EOL
# NuGetAudit=false : les correctifs d'ImageSharp (>= 4.1.2) ne ciblent que net8.0+,
# et TreatWarningsAsErrors transforme NU1902/NU1903 en erreurs bloquantes
ENV CheckEolTargetFramework=false \
    NuGetAudit=false \
    DOTNET_CLI_TELEMETRY_OPTOUT=1

# Lus par build.sh (UpdateVersionNumber) pour remplir AssemblyVersion / AssemblyConfiguration
ARG SONARR_VERSION=""
ARG BRANCH=""
ENV SONARR_VERSION=${SONARR_VERSION} \
    BRANCH=${BRANCH}

WORKDIR /src
COPY ./Sonarr/ .
# build.sh fait un find sur _temp/obj et échoue s'il n'existe pas (checkout propre)
RUN rm -f global.json && mkdir -p _temp/obj

RUN --mount=type=cache,target=/root/.nuget/packages \
    --mount=type=cache,target=/usr/local/share/.cache/yarn \
    ./build.sh --backend --frontend -f net6.0 -r linux-musl-x64

############################
# Stage 2 : image finale
############################
FROM ghcr.io/linuxserver/baseimage-alpine:3.24

ARG BUILD_DATE
ARG VERSION
LABEL build_version="Linuxserver.io version:- ${VERSION} Build-date:- ${BUILD_DATE}"
LABEL maintainer="thespad"

ENV XDG_CONFIG_HOME="/config/xdg" \
    SONARR_BRANCH="main" \
    COMPlus_EnableDiagnostics=0 \
    TMPDIR=/run/sonarr-temp

RUN apk add --no-cache icu-libs sqlite-libs xmlstarlet

COPY --from=build --chmod=755 /src/_output/net6.0/linux-musl-x64/ /app/sonarr/bin/
COPY --from=build /src/_output/UI/ /app/sonarr/bin/UI/

RUN printf "UpdateMethod=docker\nBranch=${SONARR_BRANCH}\nPackageVersion=${VERSION:-LocalBuild}\nPackageAuthor=[lasyan3](https://github.com/lasyan3/Sonarr)\n" > /app/sonarr/package_info \
    && printf "Linuxserver.io version: ${VERSION}\nBuild-date: ${BUILD_DATE}" > /build_version

COPY docker-sonarr/root/ /

EXPOSE 8989
VOLUME /config
