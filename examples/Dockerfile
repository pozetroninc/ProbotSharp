# Shared Dockerfile for all ProbotSharp examples
# Usage: docker build -f examples/Dockerfile --build-arg EXAMPLE_NAME=MinimalBot -t minimalbot:latest .

ARG EXAMPLE_NAME
ARG EXAMPLE_PATH=examples/${EXAMPLE_NAME}

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Accept build arg in this stage
ARG EXAMPLE_NAME
ARG EXAMPLE_PATH

# Copy project files for dependency resolution (monorepo structure)
COPY ["${EXAMPLE_PATH}/${EXAMPLE_NAME}.csproj", "${EXAMPLE_PATH}/"]
COPY ["src/ProbotSharp.Domain/ProbotSharp.Domain.csproj", "src/ProbotSharp.Domain/"]
COPY ["src/ProbotSharp.Application/ProbotSharp.Application.csproj", "src/ProbotSharp.Application/"]
COPY ["src/ProbotSharp.Infrastructure/ProbotSharp.Infrastructure.csproj", "src/ProbotSharp.Infrastructure/"]
COPY ["src/ProbotSharp.Adapters.Http/ProbotSharp.Adapters.Http.csproj", "src/ProbotSharp.Adapters.Http/"]
COPY ["src/ProbotSharp.Shared/ProbotSharp.Shared.csproj", "src/ProbotSharp.Shared/"]

# Restore dependencies
RUN dotnet restore "${EXAMPLE_PATH}/${EXAMPLE_NAME}.csproj"

# Copy all source code
COPY . .

# Build and publish
WORKDIR /src/${EXAMPLE_PATH}
RUN dotnet build "${EXAMPLE_NAME}.csproj" -c Release --no-restore
RUN dotnet publish "${EXAMPLE_NAME}.csproj" -c Release -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Accept build arg and convert to env var for entrypoint
ARG EXAMPLE_NAME
ENV EXAMPLE_NAME=${EXAMPLE_NAME}

# Copy published application
COPY --from=build /app/publish .

# Set environment variables for ASP.NET Core
ENV ASPNETCORE_URLS=http://+:5000 \
    DOTNET_ENVIRONMENT=Production

# Expose port
EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:5000/health || exit 1

# Dynamic entrypoint based on example name
ENTRYPOINT sh -c "dotnet ${EXAMPLE_NAME}.dll"
