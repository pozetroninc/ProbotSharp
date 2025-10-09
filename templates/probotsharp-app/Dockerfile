# Multi-stage Dockerfile for ProbotSharp Application
# This Dockerfile builds a production-ready container for your GitHub App

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["MyBot.csproj", "./"]
RUN dotnet restore "MyBot.csproj"

# Copy source code and build
COPY . .
RUN dotnet build "MyBot.csproj" -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish "MyBot.csproj" -c Release -o /app/publish \
    --no-restore \
    --no-build \
    /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user for security
RUN groupadd -r probotsharp && useradd -r -g probotsharp probotsharp

# Copy published application
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R probotsharp:probotsharp /app

# Switch to non-root user
USER probotsharp

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000 \
    DOTNET_ENVIRONMENT=Production

# Expose port
EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:5000/health || exit 1

# Start application
ENTRYPOINT ["dotnet", "MyBot.dll"]
