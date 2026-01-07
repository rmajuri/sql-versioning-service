# -------------------------------
# Build stage
# -------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore first (better layer caching)
COPY SqlVersioningService.csproj ./
RUN dotnet restore ./SqlVersioningService.csproj

# Copy everything else and publish
COPY . ./
RUN dotnet publish ./SqlVersioningService.csproj -c Release -o /app/publish /p:UseAppHost=false

# -------------------------------
# Runtime stage
# -------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# ASP.NET Core should listen on 8080 for Azure Container Apps
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Copy published output
COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "SqlVersioningService.dll"]

