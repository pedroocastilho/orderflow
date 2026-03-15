FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/OrderFlow.Worker/OrderFlow.Worker.csproj", "src/OrderFlow.Worker/"]
COPY ["src/OrderFlow.Domain/OrderFlow.Domain.csproj", "src/OrderFlow.Domain/"]
COPY ["src/OrderFlow.Infrastructure/OrderFlow.Infrastructure.csproj", "src/OrderFlow.Infrastructure/"]
COPY ["src/OrderFlow.Application/OrderFlow.Application.csproj", "src/OrderFlow.Application/"]

RUN dotnet restore "src/OrderFlow.Worker/OrderFlow.Worker.csproj"

COPY . .

RUN dotnet build "src/OrderFlow.Worker/OrderFlow.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/OrderFlow.Worker/OrderFlow.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "OrderFlow.Worker.dll"]
