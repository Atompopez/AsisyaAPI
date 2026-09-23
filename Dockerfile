# syntax=docker/dockerfile:1

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero solo los .csproj para cachear el restore entre builds.
COPY src/AsisyaApi.Domain/AsisyaApi.Domain.csproj src/AsisyaApi.Domain/
COPY src/AsisyaApi.Application/AsisyaApi.Application.csproj src/AsisyaApi.Application/
COPY src/AsisyaApi.Infrastructure/AsisyaApi.Infrastructure.csproj src/AsisyaApi.Infrastructure/
COPY src/AsisyaApi.Api/AsisyaApi.Api.csproj src/AsisyaApi.Api/
RUN dotnet restore src/AsisyaApi.Api/AsisyaApi.Api.csproj

COPY src/ src/
RUN dotnet publish src/AsisyaApi.Api/AsisyaApi.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

# Usuario sin privilegios incluido en las imágenes oficiales de .NET.
USER $APP_UID
ENTRYPOINT ["dotnet", "AsisyaApi.Api.dll"]
