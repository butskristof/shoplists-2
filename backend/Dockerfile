# syntax=docker/dockerfile:1
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /app

# Copy build configuration and project files for restore layer caching
COPY .editorconfig Directory.Build.props Directory.Packages.props global.json ./
COPY --parents **/*.csproj .
RUN dotnet restore src/3-hosts/Api/Api.csproj -a $TARGETARCH \
 && dotnet restore src/3-hosts/DatabaseMigrator/DatabaseMigrator.csproj -a $TARGETARCH

# Copy source and build
COPY src/ src/

# Publish targets — one per host
FROM build AS publish-api
RUN dotnet publish src/3-hosts/Api/Api.csproj -a $TARGETARCH -c Release --no-restore -o /out

FROM build AS publish-migrator
RUN dotnet publish src/3-hosts/DatabaseMigrator/DatabaseMigrator.csproj -a $TARGETARCH -c Release --no-restore -o /out

# Runtime targets
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --link --from=publish-api /out .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Shoplists.Api.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS migrator
WORKDIR /app
COPY --link --from=publish-migrator /out .
USER $APP_UID
ENTRYPOINT ["dotnet", "Shoplists.DatabaseMigrator.dll"]
