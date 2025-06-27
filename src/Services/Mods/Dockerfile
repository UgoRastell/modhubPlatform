FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copier le fichier projet et restaurer les dépendances
COPY ["ModsService.csproj", "./"]
RUN dotnet restore "ModsService.csproj"

# Copier le reste des fichiers
COPY . .

# Publier l'application
RUN dotnet publish "ModsService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Créer un utilisateur non-root pour plus de sécurité
RUN adduser --disabled-password --home /app --gecos '' appuser && \
    chown -R appuser:appuser /app
USER appuser

ENTRYPOINT ["dotnet", "ModsService.dll"]
