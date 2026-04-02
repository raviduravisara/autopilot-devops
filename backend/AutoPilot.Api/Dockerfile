FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["backend/AutoPilot.Api/AutoPilot.Api.csproj", "backend/AutoPilot.Api/"]
RUN dotnet restore "backend/AutoPilot.Api/AutoPilot.Api.csproj"

COPY . .
WORKDIR /src/backend/AutoPilot.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AutoPilot.Api.dll"]