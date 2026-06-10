# src/Dynamite.Bot/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files và restore trước (layer caching)
COPY src/Dynamite.Shared/Dynamite.Shared.csproj               src/Dynamite.Shared/
COPY src/Dynamite.Core/Dynamite.Core.csproj                   src/Dynamite.Core/
COPY src/Dynamite.Application/Dynamite.Application.csproj     src/Dynamite.Application/
COPY src/Dynamite.Infrastructure/Dynamite.Infrastructure.csproj src/Dynamite.Infrastructure/
COPY src/Dynamite.Modules/Moderation/Dynamite.Modules.Moderation.csproj    src/Dynamite.Modules/Moderation/
COPY src/Dynamite.Modules/Welcome/Dynamite.Modules.Welcome.csproj          src/Dynamite.Modules/Welcome/
COPY src/Dynamite.Modules/RoleManagement/Dynamite.Modules.RoleManagement.csproj src/Dynamite.Modules/RoleManagement/
COPY src/Dynamite.Modules/Setup/Dynamite.Modules.Setup.csproj              src/Dynamite.Modules/Setup/
COPY src/Dynamite.Modules/Logging/Dynamite.Modules.Logging.csproj          src/Dynamite.Modules/Logging/
COPY src/Dynamite.Modules/Security/Dynamite.Modules.Security.csproj        src/Dynamite.Modules/Security/
COPY src/Dynamite.Modules.Giveaway/Dynamite.Modules.Giveaway.csproj        src/Dynamite.Modules.Giveaway/
COPY src/Dynamite.Modules.Ticket/Dynamite.Modules.Ticket.csproj            src/Dynamite.Modules.Ticket/
COPY src/Dynamite.Modules.Economy/Dynamite.Modules.Economy.csproj          src/Dynamite.Modules.Economy/
COPY src/Dynamite.Bot/Dynamite.Bot.csproj                     src/Dynamite.Bot/

RUN dotnet restore src/Dynamite.Bot/Dynamite.Bot.csproj

# Copy toàn bộ source và build
COPY src/ src/
RUN dotnet publish src/Dynamite.Bot/Dynamite.Bot.csproj \
    -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

RUN mkdir -p logs

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Dynamite.Bot.dll"]