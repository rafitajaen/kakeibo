# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Copy central build configuration for layer caching
COPY Directory.Build.props Directory.Packages.props BannedSymbols.txt ./

# Copy project file and restore dependencies
COPY src/Kakeibo.Api/Kakeibo.Api.csproj src/Kakeibo.Api/
RUN dotnet restore src/Kakeibo.Api/Kakeibo.Api.csproj

# Copy source code and publish
COPY src/Kakeibo.Api/ src/Kakeibo.Api/
RUN dotnet publish src/Kakeibo.Api/Kakeibo.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "Kakeibo.Api.dll"]
