# Troubleshooting Guide

## Self-Hosted SignalR Issues

### Client Can't Connect to SignalR Hub

**Symptom:** Client shows "Failed to connect" or connection timeout

**Check:**
1. Is the SignalR Hub running?
   ```bash
   docker ps | grep signalr-hub
   # or locally:
   curl http://localhost:5000/api/broadcast/health
   ```

2. Is the client using the correct URL?
   - Should be: `http://localhost:5000/equipmentHub`
   - NOT: `http://localhost:7071/api/negotiate`

3. Check SignalR Hub logs:
   ```bash
   docker logs -f ryg-signalr-hub
   ```

**Fix:**
- Ensure SignalR Hub is running: `docker-compose ps`
- Restart SignalR Hub: `docker-compose restart signalr-hub`
- Check client code uses correct URL (see clients/*/Program.cs)

### Functions Can't Send Messages to SignalR Hub

**Symptom:** Equipment state changes don't appear in clients

**Check:**
1. Is the SignalR Hub URL configured correctly?
   ```bash
   docker exec ryg-functions env | grep SignalR
   # Should show: SignalR__HubUrl=http://signalr-hub:80
   ```

2. Can Functions reach SignalR Hub?
   ```bash
   docker exec ryg-functions curl http://signalr-hub:80/api/broadcast/health
   ```

3. Check Functions logs for HTTP errors:
   ```bash
   docker logs -f ryg-functions | grep "SignalR\|broadcast"
   ```

**Fix:**
- Verify `SignalR__HubUrl` in docker-compose.yml
- Ensure both containers are on same network: `docker network inspect docker_ryg-network`
- Restart Functions: `docker-compose restart functions`

### Service Bus Events Not Triggering

**Symptom:** HTTP requests work, but SignalR updates don't happen

**Check:**
1. Is Service Bus running and healthy?
   ```bash
   docker-compose ps servicebus-emulator
   ```

2. Check Service Bus logs:
   ```bash
   docker logs -f servicebus-emulator
   ```

3. Verify topic and subscription exist:
   ```bash
   cat docker/servicebus-config.json
   # Should show equipment-events topic with state-changed-subscription
   ```

**Fix:**
- Wait for Service Bus to be healthy (can take 30-60 seconds)
- Restart Service Bus: `docker-compose restart servicebus-emulator`
- Check Service Bus connection string in docker-compose.yml

### Client Receives No Updates

**Symptom:** Client connects successfully but receives no messages

**Check:**
1. Test the complete flow manually:
   ```bash
   # 1. Create equipment
   curl -X POST http://localhost:7071/api/equipment \
     -H "Content-Type: application/json" \
     -d '{"name": "Test", "initialState": 2}'

   # 2. Note the equipment ID from response

   # 3. Change state
   curl -X PATCH http://localhost:7071/api/equipment/{id}/state \
     -H "Content-Type: application/json" \
     -d '{"state": 0}'
   ```

2. Check each step in the logs:
   ```bash
   # Functions receives HTTP request
   docker logs ryg-functions | grep "Changing state"

   # Service Bus receives event
   docker logs servicebus-emulator | tail -20

   # Functions trigger fires
   docker logs ryg-functions | grep "Received state changed event"

   # SignalR Hub receives broadcast
   docker logs ryg-signalr-hub | grep "Broadcasting"
   ```

**Fix:**
- Identify which step fails from logs above
- Ensure all services are running: `docker-compose ps`
- Try the manual test from "Testing the Complete Flow" in docker/README.md

### Port Already in Use

**Symptom:** Docker fails to start with port binding error

**Check:**
```bash
# Check what's using port 7071
lsof -i :7071  # macOS/Linux
netstat -ano | findstr :7071  # Windows

# Check port 5000
lsof -i :5000  # macOS/Linux
netstat -ano | findstr :5000  # Windows
```

**Fix:**
- Kill the process using the port
- OR change port in docker-compose.yml:
  ```yaml
  ports:
    - "7072:80"  # Changed from 7071
  ```

### SQLite Database Errors

**Symptom:** "Database is locked" or "Cannot open database"

**Fix:**
```bash
# Stop containers
docker-compose down

# Remove volume
docker volume rm docker_functions-data

# Start fresh
docker-compose up -d
```

### CORS Errors in Browser

**Symptom:** Browser console shows CORS errors when testing

**Note:** SignalR Hub has CORS enabled for all origins in development.

**Check:**
- Verify CORS is configured in SignalR Hub Program.cs
- Check browser console for exact error

**Fix:**
- SignalR Hub already configured with `AllowAnyOrigin()`
- If still seeing errors, check browser extensions blocking requests

## General Docker Issues

### Container Keeps Restarting

**Check:**
```bash
docker logs --tail 100 ryg-functions
docker logs --tail 100 ryg-signalr-hub
```

**Common Causes:**
- Missing connection strings
- Port conflicts
- Out of memory
- Application crashes on startup

**Fix:**
- Fix the error shown in logs
- Increase Docker memory limit in Docker Desktop settings
- Check all environment variables are set correctly

### Out of Disk Space

**Check:**
```bash
docker system df
```

**Fix:**
```bash
# Clean up old containers, images, volumes
docker system prune -a --volumes

# Be careful - this removes everything!
```

### Network Issues Between Containers

**Check:**
```bash
# Check network exists
docker network ls | grep ryg-network

# Inspect network
docker network inspect docker_ryg-network

# Test connectivity
docker exec ryg-functions ping signalr-hub
```

**Fix:**
```bash
# Recreate network
docker-compose down
docker network rm docker_ryg-network
docker-compose up -d
```

## Local Development Issues

### Local Functions Can't Connect to Docker SignalR Hub

**Symptom:** Running Functions locally, SignalR Hub in Docker

**Check:**
- Local appsettings.json has: `"SignalR": { "HubUrl": "http://localhost:5000" }`
- NOT: `http://signalr-hub:80` (that's for Docker)

**Fix:**
- Update appsettings.json to use localhost:5000
- Restart Functions app

### Local Service Bus Connection Failed

**Symptom:** Running locally, Service Bus emulator in Docker

**Check:**
- Service Bus connection string uses `localhost`
- Service Bus emulator is running and healthy

**Fix:**
- Update connection string to use `localhost`
- Or use `docker-compose.dev.yml` and skip Service Bus for local dev

## Debugging Tips

### Enable Detailed Logging

**Functions (local):**
Edit `appsettings.json`:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    }
  }
}
```

**Functions (Docker):**
Add to docker-compose.yml:
```yaml
environment:
  Logging__LogLevel__Default: "Debug"
```

**SignalR Hub:**
Add to docker-compose.yml:
```yaml
environment:
  Logging__LogLevel__Default: "Debug"
  Logging__LogLevel__Microsoft.AspNetCore.SignalR: "Debug"
```

### Test SignalR Hub Directly

Bypass Functions and test SignalR Hub:

```bash
# Send a test message
curl -X POST http://localhost:5000/api/broadcast \
  -H "Content-Type: application/json" \
  -d '{
    "methodName": "equipmentStateChanged",
    "data": {
      "equipmentId": "00000000-0000-0000-0000-000000000000",
      "equipmentName": "Test Equipment",
      "newState": 2,
      "currentOrderId": null,
      "changedAt": "2025-01-15T10:00:00Z"
    }
  }'
```

If client receives this message, the issue is between Functions and SignalR Hub.

### Verify Service Bus Topic

Check if topic and subscription exist:

```bash
# View Service Bus config
cat docker/servicebus-config.json

# Should show:
# - Namespace: equipment-namespace
# - Topic: equipment-events
# - Subscription: state-changed-subscription
```

### Check Database State

View SQLite database contents:

```bash
# Copy database from container
docker cp ryg-functions:/data/equipment.db ./equipment.db

# View with SQLite
sqlite3 equipment.db "SELECT * FROM Equipment;"

# Or use a GUI tool like DB Browser for SQLite
```

## Performance Issues

### High Memory Usage

**Check:**
```bash
docker stats
```

**Fix:**
- Increase Docker Desktop memory limit
- Check for memory leaks in logs
- Restart containers: `docker-compose restart`

### Slow Response Times

**Check:**
- Functions logs for slow operations
- Database locks
- Service Bus message processing backlog

**Fix:**
- Enable debug logging to identify bottleneck
- Check database indexes
- Verify Service Bus is processing messages

## Getting Help

If you're still stuck:

1. **Collect Logs:**
   ```bash
   docker-compose logs > logs.txt
   ```

2. **Check Service Health:**
   ```bash
   docker-compose ps > status.txt
   ```

3. **Test Each Component:**
   - Can you reach Functions API? `curl http://localhost:7071/api/equipment`
   - Can you reach SignalR Hub? `curl http://localhost:5000/api/broadcast/health`
   - Are clients connecting? Check client logs

4. **Review Architecture:**
   - Read [ARCHITECTURE.md](ARCHITECTURE.md) to understand the flow
   - Verify each step in the flow is working

5. **Clean Slate:**
   ```bash
   docker-compose down -v
   docker-compose up --build -d
   ```
