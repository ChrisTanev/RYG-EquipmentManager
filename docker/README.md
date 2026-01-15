# RYG Equipment Manager - Docker Setup

This directory contains all the Docker configuration files needed to run the RYG Equipment Manager solution in containers.

## Infrastructure Components

The complete Docker setup includes:

1. **Azurite** - Azure Storage Emulator (Blob, Queue, Table)
2. **Azure Service Bus Emulator** - Message broker for equipment events
3. **SQL Server 2022** - Backing store for Service Bus Emulator
4. **Azure Functions App** - Main API and business logic
5. **SignalR Hub Host** - Real-time communication hub for clients

## Prerequisites

- Docker Desktop or Docker Engine (20.10+)
- Docker Compose (2.0+)
- For debugging: Visual Studio Code with C# extension

## Quick Start

### Production Mode

Start all services in production mode:

```bash
cd docker
docker-compose up -d
```

This will start all infrastructure and application services. Access points:
- Azure Functions API: http://localhost:7071
- SignalR Hub: http://localhost:5000 (WebSocket: ws://localhost:5000/equipmentHub)
- Azurite Blob: http://localhost:10000
- Azurite Queue: http://localhost:10001
- Azurite Table: http://localhost:10002
- SQL Server: localhost:1433

**Important:** Clients connect to the SignalR Hub at `http://localhost:5000/equipmentHub`, not the Functions endpoint.

### Development Mode (Infrastructure Only)

For local development when running Functions/SignalR on your machine:

```bash
cd docker
docker-compose -f docker-compose.dev.yml up -d
```

This starts only Azurite storage emulator.

### Debug Mode (With Source Mounting)

For debugging with hot-reload support:

```bash
cd docker
docker-compose -f docker-compose.yml -f docker-compose.debug.yml up --build
```

This builds debug-enabled containers with:
- Source code mounted as volumes
- Debugger (vsdbg) installed
- Hot reload enabled
- Debug ports exposed (5678 for Functions, 5679 for SignalR)

## Docker Files Overview

### Dockerfiles

- **Dockerfile** - Production build for Azure Functions
- **Dockerfile.debug** - Debug build for Azure Functions with vsdbg
- **Dockerfile.signalr** - Production build for SignalR Hub
- **Dockerfile.signalr.debug** - Debug build for SignalR Hub with vsdbg

### Docker Compose Files

- **docker-compose.yml** - Main configuration with all services
- **docker-compose.dev.yml** - Minimal setup (Azurite only)
- **docker-compose.debug.yml** - Debug overrides for development
- **servicebus-config.json** - Service Bus Emulator configuration

## Debugging in VS Code

### Attach to Running Container

1. Start containers in debug mode:
   ```bash
   cd docker
   docker-compose -f docker-compose.yml -f docker-compose.debug.yml up --build
   ```

2. In VS Code, press F5 and select:
   - "Docker: Attach to Functions" - Debug Azure Functions
   - "Docker: Attach to SignalR Hub" - Debug SignalR Hub
   - "Docker: All Services" - Debug both at once

3. Select the .NET process from the list (usually `dotnet` or the assembly name)

4. Set breakpoints and debug normally

### Local Debugging (Without Docker)

1. Start infrastructure only:
   ```bash
   cd docker
   docker-compose -f docker-compose.dev.yml up -d
   ```

2. In VS Code, press F5 and select:
   - "Local: Functions (F5)" - Run Functions locally
   - "Local: SignalR Hub (F5)" - Run SignalR Hub locally
   - "Local: Full System" - Run both locally

## Client Applications

The client applications (HMI, Operator, Supervisor) are console applications that should run on your local machine:

### Run from VS Code

Use the debug configurations:
- "Local: HMI Client"
- "Local: Operator Client"
- "Local: Supervisor Client"

### Run from Terminal

```bash
# HMI Client (visual dashboard)
dotnet run --project clients/RYG.HmiClient

# Operator Client (console monitoring)
dotnet run --project clients/RYG.OperatorClient

# Supervisor Client (management)
dotnet run --project clients/RYG.SupervisorClient
```

**Connection Info:**
- SignalR Hub: http://localhost:5000/equipmentHub (for real-time updates)
- Functions API: http://localhost:7071 (for HTTP requests)

The clients automatically connect to the correct endpoints.

## Testing the Complete Flow

Once all services are running, test the end-to-end flow:

1. **Start all services:**
   ```bash
   cd docker
   docker-compose up -d
   ```

2. **Wait for services to be healthy:**
   ```bash
   docker-compose ps
   # Wait until all services show "Up" or "healthy"
   ```

3. **Start a client (in a new terminal):**
   ```bash
   cd ..
   dotnet run --project clients/RYG.HmiClient
   ```

4. **Create equipment (in another terminal):**
   ```bash
   curl -X POST http://localhost:7071/api/equipment \
     -H "Content-Type: application/json" \
     -d '{"name": "Test Machine", "initialState": 2}'
   ```

5. **Change equipment state:**
   ```bash
   curl -X PATCH http://localhost:7071/api/equipment/{id}/state \
     -H "Content-Type: application/json" \
     -d '{"state": 0}'
   ```

6. **Observe the flow:**
   - HTTP request → Functions API
   - Functions → Service Bus
   - Service Bus → Functions (trigger)
   - Functions → SignalR Hub (HTTP POST)
   - SignalR Hub → Client (WebSocket)
   - Client displays the state change in real-time!

## Environment Variables

### Azure Functions

Key environment variables (set in docker-compose.yml):

- `AzureWebJobsStorage` - Connection to Azurite
- `FUNCTIONS_WORKER_RUNTIME` - Set to "dotnet-isolated"
- `ServiceBusConnection` - Connection to Service Bus Emulator
- `ConnectionStrings__Database` - SQLite database path
- `ServiceBus__TopicName` - Topic name for events

### SignalR Hub

- `ASPNETCORE_URLS` - Listening URL (http://+:80)
- `ASPNETCORE_ENVIRONMENT` - Development or Production

## Networking

All services are on the `ryg-network` bridge network, allowing inter-container communication by service name:

- Functions can reach: `azurite`, `servicebus-emulator`, `mssql`
- SignalR Hub can reach: `functions`
- Clients connect to exposed ports on `localhost`

## Data Persistence

Docker volumes are used for data persistence:

- `azurite-data` - Azure Storage data
- `mssql-data` - SQL Server data for Service Bus
- `functions-data` - SQLite database for equipment state

To reset all data:
```bash
docker-compose down -v
```

## Useful Commands

### View Logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f functions
docker-compose logs -f signalr-hub

# Or use VS Code tasks
```

### Restart a Service

```bash
docker-compose restart functions
docker-compose restart signalr-hub
```

### Rebuild After Code Changes

```bash
docker-compose up --build -d
```

### Stop All Services

```bash
docker-compose down
```

### Stop and Remove Volumes

```bash
docker-compose down -v
```

### Check Service Health

```bash
docker-compose ps
docker ps
```

## Troubleshooting

### Service Bus Emulator Won't Start

The Service Bus Emulator requires SQL Server to be healthy first. Check:

```bash
docker logs servicebus-emulator
docker logs mssql
```

Wait for SQL Server health check to pass before Service Bus starts.

### Functions App Can't Connect to Azurite

Ensure Azurite is running and check the connection string uses the container name:

```bash
docker logs azurite
docker logs functions
```

### Debug Attach Fails

1. Ensure containers are running in debug mode
2. Verify vsdbg is installed: `docker exec ryg-functions ls /vsdbg`
3. Check the process is running: `docker exec ryg-functions ps aux`
4. Make sure source file paths match in launch.json

### Port Conflicts

If ports are already in use, modify the port mappings in docker-compose.yml:

```yaml
ports:
  - "7072:80"  # Change from 7071 to 7072
```

## VS Code Tasks

Available tasks (Ctrl+Shift+P > Tasks: Run Task):

- `build` - Build the solution
- `test` - Run tests
- `docker-up` - Start Docker services (production)
- `docker-up-debug` - Start Docker services (debug mode)
- `docker-down` - Stop Docker services
- `docker-logs-functions` - Follow Functions logs
- `docker-logs-signalr` - Follow SignalR Hub logs

## Architecture Overview

Self-Hosted SignalR Architecture:

```
┌─────────────────────────────────────────────────────────────┐
│                     Docker Network (ryg-network)             │
│                                                              │
│  ┌──────────┐  ┌──────────────┐  ┌────────────────┐       │
│  │ Azurite  │  │ Service Bus  │  │   SQL Server   │       │
│  │ Storage  │  │   Emulator   │  │   (for SB)     │       │
│  └────┬─────┘  └──────┬───────┘  └───────┬────────┘       │
│       │               │                   │                 │
│       └───────────────┼───────────────────┘                 │
│                       │                                     │
│           ┌───────────▼──────────┐                         │
│           │  Azure Functions     │  HTTP Triggers          │
│           │  ┌────────────────┐  │  Service Bus Triggers   │
│           │  │ HttpSignalR-   │  │                         │
│           │  │ Publisher      │  │  Publishes to Service   │
│           │  └───────┬────────┘  │  Bus, reads from SB     │
│           └──────────┼───────────┘                         │
│                      │ HTTP POST                            │
│                      │ /api/broadcast                       │
│           ┌──────────▼───────────┐                         │
│           │   SignalR Hub Host   │  Self-Hosted SignalR    │
│           │  ┌────────────────┐  │                         │
│           │  │ Broadcast      │  │  Receives HTTP from     │
│           │  │ Controller     │  │  Functions, broadcasts  │
│           │  └───────┬────────┘  │  via WebSocket          │
│           │          │            │                         │
│           │  ┌───────▼────────┐  │                         │
│           │  │ EquipmentHub   │  │  /equipmentHub          │
│           │  │ (WebSocket)    │  │                         │
│           │  └────────────────┘  │                         │
│           └──────────────────────┘                         │
│                      │ WebSocket                            │
└──────────────────────┼──────────────────────────────────────┘
                       │
           ┌───────────┴───────────┐
           │    Client Apps        │
           │  (HMI/Operator/       │
           │   Supervisor)         │
           │                       │
           │  Connect to:          │
           │  ws://localhost:5000/ │
           │     equipmentHub      │
           └───────────────────────┘
```

**Flow:** HTTP Request → Functions → Service Bus → Functions (trigger) → HTTP POST → SignalR Hub → WebSocket → Clients

See [ARCHITECTURE.md](../ARCHITECTURE.md) for detailed architecture documentation.

## Next Steps

1. Start the infrastructure: `docker-compose up -d`
2. Verify all services are healthy: `docker-compose ps`
3. Run a client application to test connectivity
4. Check logs if any issues: `docker-compose logs -f`

For production deployment, consider:
- Using Azure Storage instead of Azurite
- Using Azure Service Bus instead of the emulator
- Deploying Functions to Azure Functions
- Using Azure SignalR Service for scaling
