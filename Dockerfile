FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["backend/InventoryManager.Domain/InventoryManager.Domain.csproj", "backend/InventoryManager.Domain/"]
COPY ["backend/InventoryManager.Application/InventoryManager.Application.csproj", "backend/InventoryManager.Application/"]
COPY ["backend/InventoryManager.Infrastructure/InventoryManager.Infrastructure.csproj", "backend/InventoryManager.Infrastructure/"]
COPY ["backend/InventoryManager.WebApi/InventoryManager.WebApi.csproj", "backend/InventoryManager.WebApi/"]

RUN dotnet restore "backend/InventoryManager.WebApi/InventoryManager.WebApi.csproj"

COPY backend/. ./backend/

WORKDIR /src/backend/InventoryManager.WebApi
RUN dotnet publish "InventoryManager.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "InventoryManager.WebApi.dll"]