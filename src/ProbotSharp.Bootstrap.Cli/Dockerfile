# syntax=docker/dockerfile:1

# Base runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution and project files for restore
COPY Directory.Build.props ./
COPY stylecop.json ./
COPY ProbotSharp.sln ./
COPY src/ProbotSharp.Domain/ProbotSharp.Domain.csproj ./src/ProbotSharp.Domain/
COPY src/ProbotSharp.Application/ProbotSharp.Application.csproj ./src/ProbotSharp.Application/
COPY src/ProbotSharp.Infrastructure/ProbotSharp.Infrastructure.csproj ./src/ProbotSharp.Infrastructure/
COPY src/ProbotSharp.Adapters.Http/ProbotSharp.Adapters.Http.csproj ./src/ProbotSharp.Adapters.Http/
COPY src/ProbotSharp.Adapters.Cli/ProbotSharp.Adapters.Cli.csproj ./src/ProbotSharp.Adapters.Cli/
COPY src/ProbotSharp.Adapters.Workers/ProbotSharp.Adapters.Workers.csproj ./src/ProbotSharp.Adapters.Workers/
COPY src/ProbotSharp.Bootstrap.Api/ProbotSharp.Bootstrap.Api.csproj ./src/ProbotSharp.Bootstrap.Api/
COPY src/ProbotSharp.Bootstrap.Cli/ProbotSharp.Bootstrap.Cli.csproj ./src/ProbotSharp.Bootstrap.Cli/
COPY src/ProbotSharp.Shared/ProbotSharp.Shared.csproj ./src/ProbotSharp.Shared/

# Restore dependencies
RUN dotnet restore src/ProbotSharp.Bootstrap.Cli/ProbotSharp.Bootstrap.Cli.csproj

# Copy source code
COPY src/ ./src/

# Build and publish
WORKDIR /src/src/ProbotSharp.Bootstrap.Cli
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

# Final stage - copy only runtime artifacts
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ProbotSharp.Bootstrap.Cli.dll"]
