# ---- Build Stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj và restore dependencies
COPY src/Shared/Shared.csproj Shared/
COPY src/LibraryApi/LibraryApi.csproj LibraryApi/
RUN dotnet restore LibraryApi/LibraryApi.csproj

# Copy toàn bộ source code
COPY src/Shared/ Shared/
COPY src/LibraryApi/ LibraryApi/

# Build và publish
WORKDIR /src/LibraryApi
RUN dotnet publish -c Release -o /app/publish --no-restore

# ---- Runtime Stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "LibraryApi.dll"]
