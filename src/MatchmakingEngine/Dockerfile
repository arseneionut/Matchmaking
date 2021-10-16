ARG BUILD_TARGET=release

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS base
COPY / /images/MatchmakingEngine/
WORKDIR /images/MatchmakingEngine
RUN dotnet restore -r linux-x64

FROM base AS release-shim
RUN dotnet publish -c Release /p:PublishSingleFile=true /p:PublishTrimmed=false -r linux-musl-x64 --force -o results

FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1-alpine AS build-release
LABEL build-target="release"
WORKDIR /usr/local/MatchmakingEngine

COPY --from=release-shim /images/MatchmakingEngine/results results
COPY --from=base /images/MatchmakingEngine/appsettings.Development.json appsettings.Development.json

FROM build-${BUILD_TARGET}
ENTRYPOINT ["./results/MatchmakingEngine", "--environment=Development"]