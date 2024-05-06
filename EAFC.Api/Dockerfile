FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["EAFC.Api/EAFC.Api.csproj", "EAFC.Api/"]
COPY ["EAFC.Services/EAFC.Services.csproj", "EAFC.Services/"]
COPY ["EAFC.Core/EAFC.Core.csproj", "EAFC.Core/"]
COPY ["EAFC.Data/EAFC.Data.csproj", "EAFC.Data/"]
COPY ["EAFC.DiscordBot/EAFC.DiscordBot.csproj", "EAFC.DiscordBot/"]
COPY ["EAFC.Notifications/EAFC.Notifications.csproj", "EAFC.Notifications/"]
COPY ["EAFC.Jobs/EAFC.Jobs.csproj", "EAFC.Jobs/"]
RUN dotnet restore "EAFC.Api/EAFC.Api.csproj"
COPY . .
WORKDIR "/src/EAFC.Api"
RUN dotnet build "EAFC.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "EAFC.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EAFC.Api.dll"]
