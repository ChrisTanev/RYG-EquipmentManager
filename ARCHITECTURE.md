# RYG Equipment Manager - Architecture

## Self-Hosted SignalR Architecture

This system uses a **self-hosted SignalR Hub** for real-time communication between the backend and client applications.

## System Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                         Complete Flow                            │
└─────────────────────────────────────────────────────────────────┘

1. HTTP Request (Change Equipment State)
   └─> Azure Functions (port 7071)
       └─> EquipmentService.ChangeStateAsync()
           ├─> Update Database (SQLite)
           └─> Publish Event to Service Bus

2. Service Bus Event
   └─> StateChangedSubscriber (Azure Function)
       └─> SignalRPublisher.SendToClientAsync()
           └─> HTTP POST to SignalR Hub (port 5000)

3. SignalR Hub (port 5000)
   └─> BroadcastController receives HTTP POST
       └─> IHubContext.Clients.All.SendAsync()
           └─> Broadcast to all connected clients

4. Client Applications
   └─> Receive real-time updates via SignalR WebSocket connection
```

## Component Diagram

```
┌──────────────────────────────────────────────────────────────────┐
│                    Docker Container Network                       │
│                                                                   │
│  ┌────────────┐         ┌─────────────────┐                     │
│  │  Azurite   │         │  Service Bus    │                     │
│  │  Storage   │         │    Emulator     │                     │
│  └─────┬──────┘         └────────┬────────┘                     │
│        │                         │                               │
│        │    ┌────────────────────┼──────────────────┐           │
│        │    │                    │                  │           │
│        ▼    ▼                    ▼                  │           │
│  ┌──────────────────────────────────────┐          │           │
│  │     Azure Functions App              │          │           │
│  │  ┌────────────────────────────────┐  │          │           │
│  │  │  HTTP Triggers                 │  │          │           │
│  │  │  - GET /api/equipment          │  │          │           │
│  │  │  - POST /api/equipment         │  │          │           │
│  │  │  - PATCH /api/equipment/{id}   │  │          │           │
│  │  └────────────────────────────────┘  │          │           │
│  │  ┌────────────────────────────────┐  │          │           │
│  │  │  Service Bus Trigger           │  │          │           │
│  │  │  - StateChangedSubscriber      │  │          │           │
│  │  └─────────────┬──────────────────┘  │          │           │
│  │                │                      │          │           │
│  │         HttpSignalRPublisher         │          │           │
│  │                │ HTTP POST            │          │           │
│  │                └──────────────────────┼──────────┘           │
│  └───────────────────────────────────────┘                      │
│                                          │                       │
│                                          ▼                       │
│  ┌────────────────────────────────────────────────┐            │
│  │     SignalR Hub Host (port 5000)               │            │
│  │  ┌──────────────────────────────────────────┐  │            │
│  │  │  BroadcastController                     │  │            │
│  │  │  POST /api/broadcast                     │  │            │
│  │  │  - Receives messages from Functions      │  │            │
│  │  └────────────────┬─────────────────────────┘  │            │
│  │                   │                             │            │
│  │  ┌────────────────▼─────────────────────────┐  │            │
│  │  │  EquipmentHub                            │  │            │
│  │  │  /equipmentHub (SignalR WebSocket)      │  │            │
│  │  │  - Broadcasts to all connected clients  │  │            │
│  │  └──────────────────────────────────────────┘  │            │
│  └────────────────────────────────────────────────┘            │
│                          │                                      │
└──────────────────────────┼──────────────────────────────────────┘
                           │ WebSocket
                           │ (ws://localhost:5000/equipmentHub)
                           ▼
              ┌────────────────────────┐
              │  Client Applications   │
              │  - HMI Client          │
              │  - Operator Client     │
              │  - Supervisor Client   │
              └────────────────────────┘
```

## Key Components

### 1. Azure Functions App (RYG.Functions)
- **Port:** 7071
- **Purpose:** REST API for equipment management
- **Responsibilities:**
  - Handle HTTP requests (CRUD operations)
  - Publish events to Service Bus
  - Subscribe to Service Bus events
  - Forward events to SignalR Hub via HTTP

### 2. SignalR Hub Host (RYG.SignalRHubHost)
- **Port:** 5000
- **Purpose:** Self-hosted real-time communication hub
- **Endpoints:**
  - `GET /api/broadcast/health` - Health check
  - `POST /api/broadcast` - Receive messages from Functions
  - `WS /equipmentHub` - SignalR WebSocket endpoint for clients
- **Responsibilities:**
  - Accept HTTP requests from Functions
  - Broadcast messages to all connected clients via SignalR
  - Manage WebSocket connections

### 3. HttpSignalRPublisher
- **Location:** RYG.Infrastructure
- **Purpose:** Bridge between Functions and SignalR Hub
- **How it works:**
  ```csharp
  // In Functions app
  await signalRPublisher.SendToClientAsync(event, "methodName");

  // Translates to HTTP POST
  POST http://signalr-hub:80/api/broadcast
  {
    "methodName": "equipmentStateChanged",
    "data": { /* event data */ }
  }
  ```

### 4. Client Applications
- **HMI Client:** Visual dashboard with live equipment states
- **Operator Client:** Console-based equipment monitoring
- **Supervisor Client:** Management dashboard
- **Connection:** All clients connect to `http://localhost:5000/equipmentHub`

## Event Flow Example

### Scenario: Change Equipment State

1. **User Action:**
   ```bash
   PATCH http://localhost:7071/api/equipment/123/state
   { "state": "Green" }
   ```

2. **Functions Processes Request:**
   ```csharp
   // ChangeEquipmentStateFunction.cs
   var equipment = await equipmentService.ChangeStateAsync(id, request);
   ```

3. **Service Publishes Event:**
   ```csharp
   // EquipmentService.cs
   await eventPublisher.PublishAsync(stateChangedEvent);  // → Service Bus
   ```

4. **Service Bus Triggers Function:**
   ```csharp
   // StateChangedSubscriber.cs
   [ServiceBusTrigger("equipment-events", "state-changed-subscription")]
   public async Task Run(string messageBody)
   {
       await signalRPublisher.SendToClientAsync(event, "equipmentStateChanged");
   }
   ```

5. **HttpSignalRPublisher Sends HTTP Request:**
   ```csharp
   // HttpSignalRPublisher.cs
   await httpClient.PostAsJsonAsync("/api/broadcast", new {
       MethodName = "equipmentStateChanged",
       Data = event
   });
   ```

6. **SignalR Hub Broadcasts:**
   ```csharp
   // BroadcastController.cs
   await hubContext.Clients.All.SendAsync("equipmentStateChanged", request.Data);
   ```

7. **Clients Receive Update:**
   ```csharp
   // HmiClient Program.cs
   connection.On<JsonElement>("equipmentStateChanged", eventData => {
       // Update UI
   });
   ```

## Why Self-Hosted SignalR?

### Advantages:
1. **No Cloud Dependency:** Works entirely offline/local
2. **No Azure Account Required:** Free to run and develop
3. **Full Control:** Complete control over WebSocket behavior
4. **Debugging:** Easy to debug and inspect traffic
5. **Cost:** Zero ongoing costs for development

### Trade-offs:
1. **Scaling:** Manual scaling (no automatic scale-out like Azure SignalR Service)
2. **High Availability:** Need to implement own HA/failover
3. **Management:** Self-managed infrastructure

## Configuration

### Docker Environment Variables

**Functions App:**
```yaml
environment:
  SignalR__HubUrl: "http://signalr-hub:80"  # Container name
```

**Local Development:**
```json
{
  "SignalR": {
    "HubUrl": "http://localhost:5000"
  }
}
```

### Client Connection Strings

**Docker (from host machine):**
```csharp
var hubUrl = "http://localhost:5000/equipmentHub";
```

**Docker (from another container):**
```csharp
var hubUrl = "http://signalr-hub:80/equipmentHub";
```

## SignalR Messages

### equipmentStateChanged
Sent when equipment state changes (Red/Yellow/Green)

**Payload:**
```json
{
  "equipmentId": "guid",
  "equipmentName": "string",
  "newState": 0,  // 0=Red, 1=Yellow, 2=Green
  "currentOrderId": "guid?",
  "changedAt": "datetime"
}
```

### equipmentStatesOverview
Periodic broadcast of all equipment states

**Payload:**
```json
[
  {
    "name": "Equipment 1",
    "state": 2
  },
  ...
]
```

### orderProcessing
Order processing events for operators

**Payload:**
```json
{
  "equipmentName": "string",
  "orderId": "guid",
  "scheduledOrders": [...]
}
```

### equipmentWithOrders
Dashboard for supervisors

**Payload:**
```json
[
  {
    "equipmentId": "guid",
    "equipmentName": "string",
    "state": 2,
    "currentOrderId": "guid?"
  },
  ...
]
```

## Debugging Tips

### Check SignalR Hub Health
```bash
curl http://localhost:5000/api/broadcast/health
```

### View Functions → Hub Communication
```bash
docker logs -f ryg-signalr-hub | grep "Broadcasting"
```

### Test SignalR Broadcasting Manually
```bash
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

## Migration to Azure SignalR Service (Future)

If you later want to use Azure SignalR Service:

1. Remove HttpSignalRPublisher
2. Restore Azure SignalR NuGet packages
3. Add `[SignalROutput]` attribute back
4. Update configuration to use Azure SignalR connection string
5. Remove self-hosted SignalR Hub Host

The rest of the code remains unchanged.
