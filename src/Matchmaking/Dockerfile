ARG BUILD_TARGET=release

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS base
COPY / /images/Matchmaking/
WORKDIR /images/Matchmaking
RUN dotnet restore -r linux-x64

FROM base AS release-shim
RUN dotnet publish -c Release /p:PublishSingleFile=true /p:PublishTrimmed=false -r linux-musl-x64 --force -o results

FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1-alpine AS build-release
LABEL build-target="release"
WORKDIR /usr/local/Matchmaking

COPY --from=release-shim /images/Matchmaking/results results
COPY --from=base /images/Matchmaking/appsettings.Development.json appsettings.Development.json

FROM build-${BUILD_TARGET}
ENTRYPOINT ["./results/Matchmaking", "--environment=Development"]