# src/Dynamite.Migrator/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Dynamite.Shared/Dynamite.Shared.csproj               src/Dynamite.Shared/
COPY src/Dynamite.Core/Dynamite.Core.csproj                   src/Dynamite.Core/
COPY src/Dynamite.Application/Dynamite.Application.csproj     src/Dynamite.Application/
COPY src/Dynamite.Infrastructure/Dynamite.Infrastructure.csproj src/Dynamite.Infrastructure/
COPY src/Dynamite.Migrator/Dynamite.Migrator.csproj           src/Dynamite.Migrator/

RUN dotnet restore src/Dynamite.Migrator/Dynamite.Migrator.csproj

COPY src/ src/
RUN dotnet publish src/Dynamite.Migrator/Dynamite.Migrator.csproj \
    -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Dynamite.Migrator.dll"]
