# Web app image (multi-stage). Build context = repo root: docker build -f backend/DefectTriage.Web/Dockerfile .

# Stage 1: build with the full SDK.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first: this layer is cached until the csproj changes.
COPY backend/DefectTriage.Web/DefectTriage.Web.csproj DefectTriage.Web/
RUN dotnet restore DefectTriage.Web/DefectTriage.Web.csproj

COPY backend/DefectTriage.Web/ DefectTriage.Web/
RUN dotnet publish DefectTriage.Web/DefectTriage.Web.csproj -c Release -o /out --no-restore

# Stage 2: only the runtime + published output ship.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /out .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
# Built-in non-root user of the Microsoft images.
USER app
ENTRYPOINT ["dotnet", "DefectTriage.Web.dll"]
