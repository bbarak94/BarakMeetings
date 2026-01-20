# Backend API Dockerfile for Railway
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY BookingPlatform.sln ./
COPY src/BookingPlatform.Domain/*.csproj ./src/BookingPlatform.Domain/
COPY src/BookingPlatform.Application/*.csproj ./src/BookingPlatform.Application/
COPY src/BookingPlatform.Infrastructure/*.csproj ./src/BookingPlatform.Infrastructure/
COPY src/BookingPlatform.API/*.csproj ./src/BookingPlatform.API/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build
RUN dotnet build -c Release --no-restore

# Publish
RUN dotnet publish src/BookingPlatform.API -c Release -o /app/publish --no-build

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Railway uses PORT environment variable
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
ENTRYPOINT ["dotnet", "BookingPlatform.API.dll"]
