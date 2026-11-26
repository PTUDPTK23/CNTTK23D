# ---- Build Stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj và restore dependencies
COPY src/Shared/Shared.csproj Shared/
COPY src/ReservationWorker/ReservationWorker.csproj ReservationWorker/
RUN dotnet restore ReservationWorker/ReservationWorker.csproj

# Copy toàn bộ source code
COPY src/Shared/ Shared/
COPY src/ReservationWorker/ ReservationWorker/

# Build và publish
WORKDIR /src/ReservationWorker
RUN dotnet publish -c Release -o /app/publish --no-restore

# ---- Runtime Stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ReservationWorker.dll"]
