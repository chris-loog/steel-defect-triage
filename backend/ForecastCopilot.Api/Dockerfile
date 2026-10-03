# Web app image (multi-stage). Build context = repo root: docker build -f backend/ForecastCopilot.Api/Dockerfile .

# Stage 1: build with the full SDK.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first: this layer is cached until the csproj changes.
COPY backend/ForecastCopilot.Api/ForecastCopilot.Api.csproj ForecastCopilot.Api/
RUN dotnet restore ForecastCopilot.Api/ForecastCopilot.Api.csproj

COPY backend/ForecastCopilot.Api/ ForecastCopilot.Api/
RUN dotnet publish ForecastCopilot.Api/ForecastCopilot.Api.csproj -c Release -o /out --no-restore

# Stage 2: only the runtime + published output ship.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /out .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
# Built-in non-root user of the Microsoft images.
USER app
ENTRYPOINT ["dotnet", "ForecastCopilot.Api.dll"]
