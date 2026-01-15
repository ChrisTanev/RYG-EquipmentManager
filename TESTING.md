# Complete Testing Guide

This guide will walk you through testing the entire RYG Equipment Manager system from scratch.

## Prerequisites

- Docker Desktop running
- .NET 9.0 SDK installed
- Terminal/Command Prompt

## Step-by-Step Testing

### Step 1: Start All Services

Open a terminal in the project root:

```bash
cd docker
./start.sh prod
# On Windows: start.bat prod
```

**Expected Output:**
```
Starting full stack in PRODUCTION mode...
[+] Running 6/6
 ✔ Container mssql                  Started
 ✔ Container azurite                Started
 ✔ Container servicebus-emulator    Started
 ✔ Container ryg-functions          Started
 ✔ Container ryg-signalr-hub        Started

Services started successfully!
```

**Wait Time:** Service Bus can take 30-60 seconds to become healthy.

### Step 2: Verify All Services Are Running

```bash
docker-compose ps
```

**Expected Output:**
```
NAME                  STATUS
azurite               Up
mssql                 Up (healthy)
ryg-functions         Up
ryg-signalr-hub       Up
servicebus-emulator   Up (healthy)
```

**If any service is "Restarting" or "Unhealthy":**
```bash
# Check logs for the specific service
docker logs ryg-functions
docker logs servicebus-emulator
```

### Step 3: Test Functions API Health

```bash
# Get all equipment (should be empty initially)
curl http://localhost:7071/api/equipment
```

**Expected Output:**
```json
[]
```

**If this fails:**
- Check Functions logs: `docker logs -f ryg-functions`
- Verify port 7071 is not in use: `lsof -i :7071` (Mac/Linux) or `netstat -ano | findstr :7071` (Windows)

### Step 4: Test SignalR Hub Health

```bash
curl http://localhost:5000/api/broadcast/health
```

**Expected Output:**
```json
{"status":"healthy","service":"SignalR Hub"}
```

**If this fails:**
- Check SignalR Hub logs: `docker logs -f ryg-signalr-hub`
- Verify port 5000 is not in use

### Step 5: Start a Client

Open a **NEW terminal** (keep Docker running in the first one):

```bash
cd /home/bitwise/Workspace/RYG-EquipmentManager
dotnet run --project clients/RYG.HmiClient
```

**Expected Output:**
```
RYG Equipment Monitor
Connecting to SignalR Hub: http://localhost:5000/equipmentHub
Connected!
Press Ctrl+C to exit

[Shows a table with "Waiting for equipment data..."]
```

**If connection fails:**
- Check SignalR Hub is running: `docker ps | grep signalr`
- Check firewall isn't blocking port 5000
- Check client logs for error details

### Step 6: Create Equipment

Open a **THIRD terminal**:

```bash
curl -X POST http://localhost:7071/api/equipment \
  -H "Content-Type: application/json" \
  -d '{"name": "Machine 1", "initialState": 2}'
```

**Expected Output:**
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "name": "Machine 1",
  "state": 2,
  "currentOrderId": null,
  "stateChangedAt": "2025-01-15T10:00:00Z"
}
```

**Copy the `id` value** - you'll need it for the next step.

**Note:** The client won't update yet because we haven't changed state (only state changes trigger Service Bus events).

### Step 7: Change Equipment State (THE BIG TEST!)

This tests the **complete flow**: HTTP → Functions → Service Bus → Functions → SignalR Hub → Client

```bash
# Replace {id} with the actual ID from Step 6
curl -X PATCH http://localhost:7071/api/equipment/{id}/state \
  -H "Content-Type: application/json" \
  -d '{"state": 0}'
```

**Example:**
```bash
curl -X PATCH http://localhost:7071/api/equipment/123e4567-e89b-12d3-a456-426614174000/state \
  -H "Content-Type: application/json" \
  -d '{"state": 0}'
```

**Expected Output in Terminal:**
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "name": "Machine 1",
  "state": 0,
  "currentOrderId": null,
  "stateChangedAt": "2025-01-15T10:01:00Z"
}
```

### Step 8: Verify Client Received Update ✨

**Go back to the client terminal** - you should see:

```
┌─────────────────────────────────────────────────────┐
│ Equipment Status                                     │
├────────────────┬─────────────┬────────────┬─────────┤
│ Equipment      │ State       │ Order      │ Changed │
├────────────────┼─────────────┼────────────┼─────────┤
│ Machine 1      │   RED       │ None       │ 10:01:00│
└────────────────┴─────────────┴────────────┴─────────┘

┌─────────────────────────────────────────────────────┐
│ Event Log                                            │
├─────────────────────────────────────────────────────┤
│ 10:01:00 Machine 1 -> RED                           │
└─────────────────────────────────────────────────────┘
```

**🎉 SUCCESS!** If you see this, the entire flow is working:
1. ✅ HTTP request received by Functions
2. ✅ Event published to Service Bus
3. ✅ Service Bus triggered Functions
4. ✅ Functions sent message to SignalR Hub
5. ✅ SignalR Hub broadcast to client
6. ✅ Client received and displayed the update

### Step 9: Test Multiple State Changes

Try changing the state a few more times to see real-time updates:

```bash
# Change to Yellow
curl -X PATCH http://localhost:7071/api/equipment/{id}/state \
  -H "Content-Type: application/json" \
  -d '{"state": 1}'

# Wait a second, then change to Green
curl -X PATCH http://localhost:7071/api/equipment/{id}/state \
  -H "Content-Type: application/json" \
  -d '{"state": 2}'

# Back to Red
curl -X PATCH http://localhost:7071/api/equipment/{id}/state \
  -H "Content-Type: application/json" \
  -d '{"state": 0}'
```

**Watch the client update in real-time** with each state change!

### Step 10: Create Multiple Equipment Items

```bash
# Create Machine 2
curl -X POST http://localhost:7071/api/equipment \
  -H "Content-Type: application/json" \
  -d '{"name": "Machine 2", "initialState": 1}'

# Create Machine 3
curl -X POST http://localhost:7071/api/equipment \
  -H "Content-Type: application/json" \
  -d '{"name": "Machine 3", "initialState": 0}'
```

Now get all equipment to see IDs:

```bash
curl http://localhost:7071/api/equipment
```

### Step 11: Test Multiple Clients

Open a **FOURTH terminal** and start another client:

```bash
# Operator Client
dotnet run --project clients/RYG.OperatorClient
```

**Expected Output:**
```
=== RYG Operator Dashboard ===
Connecting to SignalR Hub: http://localhost:5000/equipmentHub

[10:05:00] Connected to SignalR hub
Listening for order processing events...
Press Ctrl+C to exit
```

Now change equipment state again - **both clients should receive the update!**

### Step 12: Test Supervisor Client

Open a **FIFTH terminal**:

```bash
dotnet run --project clients/RYG.SupervisorClient
```

This client listens for different events but should also receive updates.

## Verification Checklist

After completing all steps, verify:

- [ ] All Docker containers are running (`docker-compose ps`)
- [ ] Functions API responds to HTTP requests
- [ ] SignalR Hub health check passes
- [ ] Clients can connect to SignalR Hub
- [ ] State changes appear in clients in real-time
- [ ] Multiple clients can connect simultaneously
- [ ] Event log shows state change history

## Advanced Testing

### Test Service Bus Flow

Check that Service Bus is processing events:

```bash
# Watch Functions logs for Service Bus processing
docker logs -f ryg-functions | grep "Received state changed event"
```

Change equipment state and you should see:
```
Received state changed event: {"equipmentId":"123...","equipmentName":"Machine 1",...}
```

### Test SignalR Broadcasting

Check that SignalR Hub receives and broadcasts messages:

```bash
# Watch SignalR Hub logs
docker logs -f ryg-signalr-hub | grep "Broadcasting"
```

Change equipment state and you should see:
```
Broadcasting SignalR message: equipmentStateChanged
```

### Test Direct SignalR Broadcasting

Bypass Functions and test SignalR Hub directly:

```bash
curl -X POST http://localhost:5000/api/broadcast \
  -H "Content-Type: application/json" \
  -d '{
    "methodName": "equipmentStateChanged",
    "data": {
      "equipmentId": "00000000-0000-0000-0000-000000000000",
      "equipmentName": "Test Direct",
      "newState": 1,
      "currentOrderId": null,
      "changedAt": "2025-01-15T10:00:00Z"
    }
  }'
```

**The client should receive this message immediately** (proves SignalR Hub → Client works).

### Test All API Endpoints

```bash
# Get all equipment
curl http://localhost:7071/api/equipment

# Get specific equipment
curl http://localhost:7071/api/equipment/{id}

# Create equipment
curl -X POST http://localhost:7071/api/equipment \
  -H "Content-Type: application/json" \
  -d '{"name": "Test", "initialState": 2}'

# Change state
curl -X PATCH http://localhost:7071/api/equipment/{id}/state \
  -H "Content-Type: application/json" \
  -d '{"state": 1}'
```

## Performance Testing

### Create Multiple Equipment Items Rapidly

```bash
# Create 10 equipment items
for i in {1..10}; do
  curl -X POST http://localhost:7071/api/equipment \
    -H "Content-Type: application/json" \
    -d "{\"name\": \"Machine $i\", \"initialState\": $((RANDOM % 3))}"
  echo ""
done
```

### Rapid State Changes

```bash
# Get equipment ID
EQUIPMENT_ID="your-equipment-id-here"

# Change state 20 times rapidly
for i in {1..20}; do
  STATE=$((RANDOM % 3))
  curl -X PATCH http://localhost:7071/api/equipment/$EQUIPMENT_ID/state \
    -H "Content-Type: application/json" \
    -d "{\"state\": $STATE}"
  echo " (Change $i to state $STATE)"
  sleep 0.5
done
```

**Watch the clients handle rapid updates!**

## Troubleshooting During Testing

### If Client Doesn't Receive Updates

1. **Check complete flow in logs:**
   ```bash
   # Terminal 1: Functions logs
   docker logs -f ryg-functions

   # Terminal 2: SignalR Hub logs
   docker logs -f ryg-signalr-hub

   # Terminal 3: Make state change
   curl -X PATCH http://localhost:7071/api/equipment/{id}/state \
     -H "Content-Type: application/json" \
     -d '{"state": 0}'
   ```

2. **Look for these log entries in order:**
   - Functions: "Changing state of equipment..."
   - Functions: "Published EquipmentStateChangedEvent event to Service Bus"
   - Functions: "Received state changed event..."
   - Functions: "Sent SignalR message: equipmentStateChanged"
   - SignalR Hub: "Broadcasting SignalR message: equipmentStateChanged"

3. **If any step is missing, that's where the problem is.**

### If Service Bus Isn't Processing

Service Bus can be slow to start. Wait 60 seconds and try again:

```bash
# Check Service Bus status
docker logs servicebus-emulator | tail -20

# Should see "Service Bus Emulator is running"
```

### If Port Conflicts

```bash
# Check what's using ports
lsof -i :7071  # Functions
lsof -i :5000  # SignalR Hub

# Or change ports in docker-compose.yml
```

## Clean Up After Testing

```bash
# Stop all services
cd docker
./start.sh stop

# Or completely clean (removes all data)
./start.sh clean
```

## Success Criteria

Your system is working correctly if:

✅ All services start without errors
✅ Functions API responds to requests
✅ SignalR Hub health check passes
✅ Clients connect successfully
✅ State changes appear in clients within 1-2 seconds
✅ Multiple clients receive updates simultaneously
✅ Logs show complete flow: HTTP → Service Bus → SignalR → Client

## Next Steps

Once everything is working:

1. Read [ARCHITECTURE.md](ARCHITECTURE.md) to understand the system design
2. Explore the code in `src/` directories
3. Try debugging with VS Code (F5 → "Docker: Attach to Functions")
4. Modify the code and see hot-reload in debug mode
5. Create your own client applications

Congratulations! You have a fully functional self-hosted SignalR equipment management system! 🎉
