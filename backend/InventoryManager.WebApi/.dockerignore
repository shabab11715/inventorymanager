FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["InventoryManager.Domain/InventoryManager.Domain.csproj", "InventoryManager.Domain/"]
COPY ["InventoryManager.Application/InventoryManager.Application.csproj", "InventoryManager.Application/"]
COPY ["InventoryManager.Infrastructure/InventoryManager.Infrastructure.csproj", "InventoryManager.Infrastructure/"]
COPY ["InventoryManager.WebApi/InventoryManager.WebApi.csproj", "InventoryManager.WebApi/"]

RUN dotnet restore "InventoryManager.WebApi/InventoryManager.WebApi.csproj"

COPY . .
WORKDIR "/src/InventoryManager.WebApi"
RUN dotnet publish "InventoryManager.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "InventoryManager.WebApi.dll"]