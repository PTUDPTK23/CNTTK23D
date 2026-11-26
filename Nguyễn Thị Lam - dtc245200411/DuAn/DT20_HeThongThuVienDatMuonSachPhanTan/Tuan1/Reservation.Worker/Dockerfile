FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Library.Contracts/Library.Contracts.csproj", "Library.Contracts/"]
COPY ["Reservation.Worker/Reservation.Worker.csproj", "Reservation.Worker/"]
RUN dotnet restore "Reservation.Worker/Reservation.Worker.csproj"
COPY . .
WORKDIR /src/Reservation.Worker
RUN dotnet publish "Reservation.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Reservation.Worker.dll"]
