# syntax=docker/dockerfile:1
# Replaced with --build-context localfeed=<dir> when NUGET_FEED=local (packages packed from the sibling repositories).
FROM scratch AS localfeed

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG NUGET_FEED=github
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props nuget.config global.json Leasing.sln ./
COPY src/ src/
COPY tools/ tools/
COPY tests/ tests/
# The GitHub Packages token comes from the build secret "nuget_token" and only lives in the environment of this step.
RUN --mount=type=secret,id=nuget_token,required=false \
    --mount=type=bind,from=localfeed,target=/localfeed \
    if [ "$NUGET_FEED" = "local" ]; then \
      dotnet nuget update source github --source /localfeed --configfile nuget.config; \
    else \
      export NuGetPackageSourceCredentials_github="Username=AlbertoMitroi;Password=$(cat /run/secrets/nuget_token)"; \
    fi && \
    dotnet restore Leasing.sln
RUN dotnet build Leasing.sln -c Release --no-restore

FROM build AS test
RUN dotnet test Leasing.sln -c Release --no-build --logger "console;verbosity=normal"

FROM build AS publish
RUN dotnet publish src/Leasing.Host/Leasing.Host.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=publish /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Leasing.Host.dll"]
