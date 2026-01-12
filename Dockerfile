# -------------------------------
# Build stage
# -------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore first (better layer caching)
COPY SqlVersioningService.csproj ./
RUN dotnet restore ./SqlVersioningService.csproj

# Copy everything else
COPY . ./

# Publish API
RUN dotnet publish ./SqlVersioningService.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# Publish migration runner (under ops/)
RUN dotnet publish ./ops/MigrateRunner/MigrateRunner.csproj \
    -c Release \
    -o /app/migrate \
    /p:UseAppHost=false

# NEW: Publish create-api-key tool
RUN dotnet publish ./ops/create-api-key/CreateApiKey.csproj \
    -c Release \
    -o /app/tools/create-api-key \
    /p:UseAppHost=false


# -------------------------------
# Runtime stage
# -------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# ASP.NET Core should listen on 8080 for Azure Container Apps
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Copy published API output
COPY --from=build /app/publish ./

# Copy migration runner output
COPY --from=build /app/migrate ./migrate/

# Copy create-api-key tool output
COPY --from=build /app/tools/create-api-key ./tools/create-api-key/

# Copy raw SQL migration scripts (ops/migrations -> /app/ops/migrations)
COPY ops/migrations ./ops/migrations

# Default entrypoint: API
ENTRYPOINT ["dotnet", "SqlVersioningService.dll"]
