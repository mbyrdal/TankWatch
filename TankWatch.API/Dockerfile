FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["TankWatch.API/TankWatch.API.csproj", "TankWatch.API/"]
COPY ["TankWatch.Core/TankWatch.Core.csproj", "TankWatch.Core/"]
COPY ["TankWatch.Infrastructure/TankWatch.Infrastructure.csproj", "TankWatch.Infrastructure/"]
RUN dotnet restore "TankWatch.API/TankWatch.API.csproj"
COPY . .
WORKDIR "/src/TankWatch.API"
RUN dotnet build "TankWatch.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TankWatch.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TankWatch.API.dll"]