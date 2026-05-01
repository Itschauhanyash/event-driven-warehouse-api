FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["WarehouseIntegrationAPI.csproj", "./"]
# Run restore (this step might fail if we don't have nuget configured right, but is standard for .NET containers)
RUN dotnet restore "./WarehouseIntegrationAPI.csproj"

COPY . .
RUN dotnet publish "WarehouseIntegrationAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WarehouseIntegrationAPI.dll"]
