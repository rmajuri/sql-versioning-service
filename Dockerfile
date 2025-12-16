# -------------------------------
# Build stage
# -------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore first (better layer caching)
COPY SqlVersioningService/*.csproj ./SqlVersioningService/
RUN dotnet restore SqlVersioningService/SqlVersioningService.csproj

# Copy everything else and build
COPY . .
WORKDIR /src/SqlVersioningService
RUN dotnet publish -c Release -o /app/publish

# -------------------------------
# Runtime stage
# -------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# ASP.NET Core listens on 8080 by default in containers
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SqlVersioningService.dll"]
