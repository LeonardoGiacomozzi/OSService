# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Copy solution and projects
COPY ["FiapOficina.OSService/src/FiapOficina.OSService.Api/FiapOficina.OSService.Api.csproj", "FiapOficina.OSService/src/FiapOficina.OSService.Api/"]
COPY ["FiapOficina.Contracts/FiapOficina.Contracts.csproj", "FiapOficina.Contracts/"]
COPY ["FiapOficina.ServiceDefaults/FiapOficina.ServiceDefaults.csproj", "FiapOficina.ServiceDefaults/"]

# Restore
RUN dotnet restore "FiapOficina.OSService/src/FiapOficina.OSService.Api/FiapOficina.OSService.Api.csproj"

# Copy everything else
COPY . .

# Build and Publish
WORKDIR "/source/FiapOficina.OSService/src/FiapOficina.OSService.Api"
RUN dotnet publish "FiapOficina.OSService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "FiapOficina.OSService.Api.dll"]
