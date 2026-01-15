# Docker Quick Reference

## Start/Stop Commands

### Production (Full Stack)
```bash
# Start all services
docker-compose up -d

# Stop all services
docker-compose down

# Stop and remove volumes (clean slate)
docker-compose down -v
```

### Development (Infrastructure Only)
```bash
# Start Azurite only
docker-compose -f docker-compose.dev.yml up -d

# Stop
docker-compose -f docker-compose.dev.yml down
```

### Debug Mode
```bash
# Start with debugging enabled
docker-compose -f docker-compose.yml -f docker-compose.debug.yml up --build

# Stop (Ctrl+C or)
docker-compose -f docker-compose.yml -f docker-compose.debug.yml down
```

## Service Management

```bash
# View all services status
docker-compose ps

# View all containers
docker ps

# Restart a specific service
docker-compose restart functions
docker-compose restart signalr-hub

# Rebuild and restart
docker-compose up --build -d functions

# Remove a specific service
docker-compose rm -f functions
```

## Logs

```bash
# All services logs (follow)
docker-compose logs -f

# Specific service
docker-compose logs -f functions
docker-compose logs -f signalr-hub
docker-compose logs -f azurite
docker-compose logs -f servicebus-emulator

# Last 100 lines
docker-compose logs --tail=100 functions

# Logs for specific container
docker logs -f ryg-functions
docker logs -f ryg-signalr-hub
```

## Access Containers

```bash
# Interactive shell into Functions container
docker exec -it ryg-functions bash

# Interactive shell into SignalR Hub container
docker exec -it ryg-signalr-hub bash

# Interactive shell into SQL Server
docker exec -it mssql bash

# Run a command in a container
docker exec ryg-functions ls -la /src
```

## Debugging

```bash
# Check if vsdbg is installed
docker exec ryg-functions ls /vsdbg

# Check running processes
docker exec ryg-functions ps aux

# Check .NET processes
docker exec ryg-functions dotnet --list-runtimes
```

## Health Checks

```bash
# Check service health
docker-compose ps

# Inspect a specific container
docker inspect ryg-functions

# Check Service Bus health
docker exec servicebus-emulator curl -f http://localhost:5672

# Check SQL Server health
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Password123" -Q "SELECT 1" -C
```

## Volume Management

```bash
# List volumes
docker volume ls

# Inspect a volume
docker volume inspect docker_functions-data

# Remove all unused volumes
docker volume prune

# Remove specific volume (must stop containers first)
docker-compose down
docker volume rm docker_functions-data
```

## Network

```bash
# List networks
docker network ls

# Inspect the RYG network
docker network inspect docker_ryg-network

# Test connectivity between containers
docker exec ryg-functions ping azurite
docker exec ryg-signalr-hub ping functions
```

## Database Access

### SQLite (in Functions container)
```bash
# Access SQLite database
docker exec -it ryg-functions ls /data/
docker exec -it ryg-functions cat /data/equipment.db
```

### SQL Server (Service Bus backing store)
```bash
# Connect to SQL Server
docker exec -it mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Password123" -C

# Run a query
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Password123" -Q "SELECT name FROM sys.databases" -C
```

## Cleanup

```bash
# Stop and remove containers, networks
docker-compose down

# Stop and remove containers, networks, volumes
docker-compose down -v

# Remove all stopped containers
docker container prune

# Remove all unused images
docker image prune -a

# Nuclear option (remove everything)
docker system prune -a --volumes
```

## Build Management

```bash
# Rebuild specific service
docker-compose build functions
docker-compose build signalr-hub

# Rebuild all services
docker-compose build

# Build with no cache
docker-compose build --no-cache

# Build and start
docker-compose up --build -d
```

## Performance & Monitoring

```bash
# View resource usage
docker stats

# View resource usage for specific containers
docker stats ryg-functions ryg-signalr-hub

# Disk usage
docker system df

# View container processes
docker top ryg-functions
```

## Troubleshooting Commands

```bash
# Check environment variables
docker exec ryg-functions env

# Check network connectivity
docker exec ryg-functions curl http://azurite:10000
docker exec ryg-signalr-hub curl http://functions:80

# Tail logs in real-time
docker logs -f --tail 50 ryg-functions

# Check container IP address
docker inspect -f '{{range.NetworkSettings.Networks}}{{.IPAddress}}{{end}}' ryg-functions

# Check open ports
docker port ryg-functions
docker port ryg-signalr-hub
```

## Quick Service URLs

When services are running:

- Functions API: http://localhost:7071
- SignalR Hub: http://localhost:5000/equipmentHub
- Azurite Blob: http://localhost:10000
- Azurite Queue: http://localhost:10001
- Azurite Table: http://localhost:10002
- SQL Server: localhost:1433

## Common Issues

### Port Already in Use
```bash
# Find what's using port 7071
lsof -i :7071  # macOS/Linux
netstat -ano | findstr :7071  # Windows

# Change port in docker-compose.yml
ports:
  - "7072:80"  # Changed from 7071
```

### Service Won't Start
```bash
# Check logs
docker-compose logs servicename

# Check health status
docker inspect servicename | grep Health

# Restart service
docker-compose restart servicename
```

### Out of Disk Space
```bash
# Check disk usage
docker system df

# Clean up
docker system prune -a --volumes
```

### Container Keeps Restarting
```bash
# Check logs for errors
docker logs --tail 100 ryg-functions

# Check container status
docker inspect ryg-functions | grep -A 10 State
```
