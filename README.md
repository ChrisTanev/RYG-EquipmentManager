# RYG Equipment Manager

A LEGO factory equipment management system that tracks equipment states (Red/Yellow/Green) using Azure Functions, self-hosted SignalR, and .NET 9.

## Architecture

- **Azure Functions** - REST API and business logic
- **Self-Hosted SignalR Hub** - Real-time WebSocket communication
- **Azure Service Bus** - Event messaging (emulator for local dev)
- **SQLite** - Equipment state persistence
- **Clients** - HMI, Operator, and Supervisor console applications

**Key Feature:** Uses a self-hosted SignalR Hub (not Azure SignalR Service) for complete local development without cloud dependencies.

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed architecture documentation.

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Docker Desktop (for infrastructure)
- Visual Studio Code (recommended)

### Quick Start with Docker

1. **Start all services:**
   ```bash
   cd docker
   docker-compose up -d
   ```

2. **Verify services are running:**
   ```bash
   docker-compose ps
   ```

3. **Run a client:**
   ```bash
   dotnet run --project clients/RYG.HmiClient
   ```

### Development Setup

1. **Start infrastructure only:**
   ```bash
   cd docker
   docker-compose -f docker-compose.dev.yml up -d
   ```

2. **Run Functions locally:**
   ```bash
   dotnet run --project src/RYG.Functions
   ```

3. **Run SignalR Hub locally:**
   ```bash
   dotnet run --project src/RYG.SignalRHubHost
   ```

4. **Run a client:**
   ```bash
   dotnet run --project clients/RYG.HmiClient
   ```

## Docker Setup

See [docker/README.md](docker/README.md) for detailed Docker setup, debugging, and troubleshooting.

### Available Configurations

- **Production Mode**: `docker-compose up -d`
- **Development Mode**: `docker-compose -f docker-compose.dev.yml up -d`
- **Debug Mode**: `docker-compose -f docker-compose.yml -f docker-compose.debug.yml up --build`

### Service Endpoints

When running in Docker:
- Azure Functions API: http://localhost:7071
- SignalR Hub: http://localhost:5000
- Azurite Blob: http://localhost:10000
- Azurite Queue: http://localhost:10001
- Azurite Table: http://localhost:10002
- SQL Server: localhost:1433 (user: sa, password: YourStrong@Password123)

## Project Structure

```
RYG-EquipmentManager/
├── src/
│   ├── RYG.Domain/              # Domain entities and interfaces
│   ├── RYG.Application/         # Business logic and services
│   ├── RYG.Infrastructure/      # Data access and external services
│   ├── RYG.Shared/              # Shared types and utilities
│   ├── RYG.Functions/           # Azure Functions API
│   └── RYG.SignalRHubHost/      # SignalR Hub hosting
├── clients/
│   ├── RYG.HmiClient/           # Human-Machine Interface client
│   ├── RYG.OperatorClient/      # Operator console client
│   └── RYG.SupervisorClient/    # Supervisor console client
├── tests/
│   ├── RYG.Domain.Tests/
│   ├── RYG.Application.Tests/
│   └── RYG.Infrastructure.Tests/
└── docker/                       # Docker configuration
    ├── Dockerfile                # Functions production build
    ├── Dockerfile.debug          # Functions debug build
    ├── Dockerfile.signalr        # SignalR production build
    ├── Dockerfile.signalr.debug  # SignalR debug build
    ├── docker-compose.yml        # Full stack
    ├── docker-compose.dev.yml    # Infrastructure only
    ├── docker-compose.debug.yml  # Debug configuration
    └── README.md                 # Docker documentation
```

## Development

### Building

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Debugging

VS Code launch configurations are available:
- Docker: Attach to Functions
- Docker: Attach to SignalR Hub
- Local: Functions (F5)
- Local: SignalR Hub (F5)
- Local: HMI Client
- Local: Operator Client
- Local: Supervisor Client

Press F5 and select the desired configuration.

## Infrastructure Components

### Required Services

1. **Azurite** - Azure Storage Emulator
2. **Azure Service Bus Emulator** - Message broker
3. **SQL Server** - Service Bus backing store

All infrastructure services are included in the Docker setup.

## Documentation

- [ARCHITECTURE.md](ARCHITECTURE.md) - Detailed system architecture and flow
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - Common issues and solutions
- [docker/README.md](docker/README.md) - Docker setup and configuration
- [docker/QUICKREF.md](docker/QUICKREF.md) - Quick reference for Docker commands

## License

See [LICENSE](LICENSE) file for details.