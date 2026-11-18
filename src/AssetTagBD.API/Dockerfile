# Build from the repository root:
#   docker build -f src/AssetTagBD.API/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first so NuGet restore is cached independently of source changes.
COPY ["AssetTagBD.sln", "./"]
COPY ["src/AssetTagBD.Domain/AssetTagBD.Domain.csproj", "src/AssetTagBD.Domain/"]
COPY ["src/AssetTagBD.Application/AssetTagBD.Application.csproj", "src/AssetTagBD.Application/"]
COPY ["src/AssetTagBD.Infrastructure/AssetTagBD.Infrastructure.csproj", "src/AssetTagBD.Infrastructure/"]
COPY ["src/AssetTagBD.API/AssetTagBD.API.csproj", "src/AssetTagBD.API/"]
RUN dotnet restore "src/AssetTagBD.API/AssetTagBD.API.csproj"

COPY src/ ./src/
RUN dotnet publish "src/AssetTagBD.API/AssetTagBD.API.csproj" \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# curl is used by container healthchecks to probe the real HTTP endpoint.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Run as the non-root user provided by the ASP.NET image.
USER app
ENTRYPOINT ["dotnet", "AssetTagBD.API.dll"]
