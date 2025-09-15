# syntax=docker/dockerfile:1.7
ARG SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:9.0
ARG RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:9.0
ARG BUILD_CONFIGURATION=Release

FROM ${SDK_IMAGE} AS restore
WORKDIR /src
COPY PhotoBank.Api/PhotoBank.Api.csproj PhotoBank.Api/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore PhotoBank.Api/PhotoBank.Api.csproj

FROM ${SDK_IMAGE} AS publish
WORKDIR /src
COPY . .
COPY --from=restore /root/.nuget /root/.nuget
COPY --from=restore /src/PhotoBank.Api/obj /src/PhotoBank.Api/obj
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish PhotoBank.Api/PhotoBank.Api.csproj -c ${BUILD_CONFIGURATION} -o /app/out \
    -p:UseAppHost=false

FROM ${RUNTIME_IMAGE} AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:5066 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_EnableDiagnostics=0
COPY --from=publish /app/out .
EXPOSE 5066
ENTRYPOINT ["dotnet", "PhotoBank.Api.dll"]
