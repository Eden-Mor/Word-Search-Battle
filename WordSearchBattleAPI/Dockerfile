FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WordSearchBattleAPI/WordSearchBattleAPI.csproj", "WordSearchBattleAPI/"]
RUN dotnet restore "./WordSearchBattleAPI/./WordSearchBattleAPI.csproj"
COPY . .
WORKDIR "/src/WordSearchBattleAPI"
RUN dotnet build "./WordSearchBattleAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WordSearchBattleAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set permissions for DataWords.xml if it exists in the Resources directory
USER root
RUN [ -f /app/Resources/DataWords.xml ] && chmod 444 /app/Resources/DataWords.xml || true
USER app


ENTRYPOINT ["dotnet", "WordSearchBattleAPI.dll"]