# src/Dynamite.API/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore
COPY src/Dynamite.Shared/Dynamite.Shared.csproj               src/Dynamite.Shared/
COPY src/Dynamite.Core/Dynamite.Core.csproj                   src/Dynamite.Core/
COPY src/Dynamite.Application/Dynamite.Application.csproj     src/Dynamite.Application/
COPY src/Dynamite.Infrastructure/Dynamite.Infrastructure.csproj src/Dynamite.Infrastructure/
COPY src/Dynamite.API/Dynamite.API.csproj                     src/Dynamite.API/

RUN dotnet restore src/Dynamite.API/Dynamite.API.csproj

# Build
COPY src/ src/
RUN dotnet publish src/Dynamite.API/Dynamite.API.csproj \
    -c Release -o /app/publish --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 5000
ENTRYPOINT ["dotnet", "Dynamite.API.dll"]