# CommunityService/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copie la solution entière (rajoute éventuellement un .dockerignore pour ne pas alourdir)
COPY . .

# Restaurer et publier uniquement CommunityService
RUN dotnet publish src/Services/Community/CommunityService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CommunityService.dll"]