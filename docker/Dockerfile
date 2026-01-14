# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln .
COPY src/RYG.Domain/*.csproj ./src/RYG.Domain/
COPY src/RYG.Application/*.csproj ./src/RYG.Application/
COPY src/RYG.Infrastructure/*.csproj ./src/RYG.Infrastructure/
COPY src/RYG.Functions/*.csproj ./src/RYG.Functions/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ ./src/

# Build and publish
WORKDIR /src/src/RYG.Functions
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated9.0
WORKDIR /home/site/wwwroot
COPY --from=build /app/publish .

ENV AzureWebJobsScriptRoot=/home/site/wwwroot
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true
