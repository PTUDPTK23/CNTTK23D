FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Library.Contracts/Library.Contracts.csproj", "Library.Contracts/"]
COPY ["Library.Client/Library.Client.csproj", "Library.Client/"]
RUN dotnet restore "Library.Client/Library.Client.csproj"
COPY . .
WORKDIR /src/Library.Client
RUN dotnet publish "Library.Client.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Library.Client.dll"]
