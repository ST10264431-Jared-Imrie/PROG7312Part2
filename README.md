# Smart-X IoT Mesh System — Real-Time Processing & Device Management (PoE Part 2)

## System Overview
**Smart-X** is an enterprise-grade Internet of Things (IoT) monitoring, real-time telemetry processing, and edge actuation ecosystem engineered for modern South African edge computing deployments—including automated agricultural operations (hydroponics, borehole pumps), utility trackers, and substation smart grid installations.

In **Part 2**, the system advances from a static data ingestion gateway into an asynchronous, high-throughput, **real-time processing engine**. It manages a live network of simulated and real microcontrollers (ESP32 nodes, smart plugs, high-voltage meters, and agricultural actuators) using **advanced computer science data structures** (`Queue<T>`, `PriorityQueue<T, P>`, `Stack<T>`, `Dictionary<K, V>`, `SortedDictionary<K, V>`, and `HashSet<T>`) alongside a **Predictive Action and Recommendation Engine**.

---

## Architectural 3-Pillar Navigation Menu

The WPF Client Dashboard (`SmartX.Dashboard`) features an organized, asynchronous navigation bar presenting the three core architectural modules:

1. **Pillar 1: Sensor Data Ingestion and Telemetry** `[ACTIVE]`
   - Core ingestion layer from Part 1 with generic packet dispatch (`TelemetryPacket<double>`), multipart diagnostic log/photo attachments, and a **Dynamic Engagement Heatmap** (`GREEN_NORMAL` vs `RED_ALERT`).
2. **Pillar 2: Real-Time Command Stream and History** `[ACTIVE - PART 2 CORE]`
   - Real-time command stream dashboard visualizing live device telemetry, interactive manual override controls (toggling relays, triggering pumps, emergency shutoff), search and category filtering, live data structure metrics, and automated predictive insights.
3. **Pillar 3: Network Topology and Mesh Routing** `[POE FINAL PREVIEW]`
   - Interactive spatial topology canvas displaying coordinator root gateways, repeater routers, and edge actuator nodes with signal metrics (RSSI, hop count), previewing Dijkstra multi-hop mesh routing for the Final PoE.

> **State Integrity:** Navigation between all three views is completely asynchronous and seamless. Switching views never resets form data, drops backend API connections, or clears in-memory command history.

---

## Advanced Data Structures Implementation (40 Marks)

To prevent gateway processing bottlenecks and optimize memory overhead on edge devices, the backend API (`SmartX.API/Services/DeviceStateService.cs`) implements the following core data structures:

| Data Structure | C# Type | Purpose & Operational Rationale | Complexity |
| :--- | :--- | :--- | :--- |
| **Message Queues** | `Queue<TelemetryPacket<double>>` | Standard First-In-First-Out (FIFO) queue buffering incoming sensor streams hitting `/api/telemetry` before dispatch. | $O(1)$ Enqueue / Dequeue |
| **Priority Queues** | `PriorityQueue<TelemetryAlert, int>` | Critical alerts (severe power surge > 80W / 3000W or hydroponic moisture drop < 20%) **bypass** the standard queue and are processed immediately with Priority 1. | $O(\log n)$ Enqueue / Dequeue |
| **Undo Command Stack** | `Stack<DeviceCommand>` | LIFO history of manual override commands sent to actuators. Serves as an instant **"Undo"** feature to safely revert the last issued command. | $O(1)$ Push / Pop |
| **Live Device Registry** | `Dictionary<string, LiveDevice>` | Stores active registered IoT devices keyed by MAC address for instantaneous lookups when telemetry arrives. | $O(1)$ Average Lookup |
| **Sequential Timeline** | `SortedDictionary<DateTime, HistoricalLog>` | Organizes historical telemetry logs chronologically by timestamp, enabling high-performance timeline rendering. | $O(\log n)$ Insert / Range |
| **Error & Disconnect Set** | `HashSet<string>` | Maintains a deduplicated set of active error states and disconnected nodes, preventing duplicate disconnect alerts. | $O(1)$ Add / Contains |

---

## Predictive Action and Recommendation Engine (30 Marks)

Implemented in `SmartX.API/Services/PredictiveEngineService.cs`, the **Automated Action Engine** proactively assists operators before critical hardware failures occur:

- **Pattern Analysis:** Analyzes historical queries and command correlations (e.g., operator searches for "Pump" or activates "Water Pump Alpha" after moisture drops below 20%).
- **Algorithmic Suggestions:** Evaluates live device conditions against learned heuristics:
  - *Hydroponics Anomaly:* When moisture plunges below 20%, suggests **"⚡ Trigger Water Pump Alpha (ON)"** with 95%+ confidence.
  - *Power Surge Anomaly:* When substation power exceeds 80.0 W (or voltage surges), suggests **"⚠️ Toggle Substation Relay (OPEN)"** to prevent transformer damage.
  - *Disconnect Anomaly:* When a node disconnects, suggests mesh self-healing ping.
- **UI Presentation:** Highlighted prominently in the UI banner as **"Suggested Actions / Automated Insights"** with a single-click **[Execute Suggested Action]** button. Executing an insight automatically pushes the action onto the `Stack<DeviceCommand>`, allowing the operator to undo it at any time!

---

## Prerequisites

Ensure the following tools are installed on your machine:
* [.NET 10.0 SDK](https://dotnet.microsoft.com/download) (or .NET 8.0 SDK)
* [Visual Studio 2022](https://visualstudio.microsoft.com/) (with *.NET desktop development* and *ASP.NET and web development* workloads)
* [Git](https://git-scm.com/)
* [Docker Desktop](https://www.docker.com/) (Optional for container deployment)

---

## Build, Test, and Execution Instructions

### 1. Build the Entire Solution
Open PowerShell or your terminal in the repository root:

```powershell
dotnet build SmartX-IoT-Mesh.sln
```

### 2. Run Automated Data Structure Unit Tests
Execute the comprehensive xUnit test suite covering all 5 data structures and the predictive engine:

```powershell
dotnet test SmartX-IoT-Mesh.sln
```
*Expected Result:* **8 Passed, 0 Failed (100% Success)**.

### 3. Launch the Backend API
In your first terminal window, start the API:

```powershell
dotnet run --project SmartX.API/SmartX.API.csproj
```
The API starts locally at `http://localhost:5000`. You can inspect the Swagger documentation at:
👉 **`http://localhost:5000/swagger`**

### 4. Launch the WPF Client Dashboard
In a second terminal window, launch the desktop dashboard:

```powershell
dotnet run --project SmartX.Dashboard/SmartX.Dashboard.csproj
```
1. Authenticate with the pre-filled credentials:
   - **Username:** `admin@smartx.io`
   - **Password:** `AdminPassword123!`
2. The Dashboard will open directly onto **Pillar 2: Real-Time Command Stream & History**.

---

## Explicit Testing & Verification Guide (For Lecturers / Markers)

### Test A: How to Test the Priority Queue Bypass
1. In the Dashboard (Pillar 2), observe the top metric counters:
   - **FIFO Message Queue:** Tracks normal packets.
   - **Priority Alert Queue:** Tracks critical bypasses.
2. Click the red button: **`🚨 Simulate High-Priority Alert`**.
3. *What happens:*
   - A critical power surge of `4850.0 W` is dispatched to node `12:34:56:78:9A:BC`.
   - The packet **bypasses the standard FIFO queue** and enters `PriorityQueue<TelemetryAlert, 1>`.
   - The device status immediately transitions to **`Critical`** (Red badge).
   - The unique error state is recorded in the `HashSet<string>`.
   - The timeline log reflects `[PriorityAlertQueue]` in bright red.

### Test B: How to Test the "Undo" Feature (Stack Implementation)
1. On any device card (e.g., *Irrigation Water Pump Alpha* or *Substation High-Power Relay*), click:
   - Click **`💧 Pump`** to turn it **ON**.
   - Notice the button updates, the status changes, and the command is pushed to the **Command History Stack** (`Stack<DeviceCommand>`).
2. Click **`⚡ Relay`** to toggle the relay from **CLOSED** to **OPEN**.
   - A second command is pushed on top of the stack.
3. Click the purple button: **`↺ Undo Last Command`**.
   - *What happens:*
   - The stack performs a **LIFO Pop**.
   - The inverse command is automatically generated (Relay restored to CLOSED).
   - The UI displays an alert: *"Command Undone: Reverted Relay to CLOSED"*.
   - Click **`↺ Undo Last Command`** again to undo the pump activation (restored to OFF).

### Test C: How to Test O(1) Dictionary Lookups & Real-Time Filtering
1. In the **Search Devices** box, type `Hydro` or `Pump` or a MAC like `00:1A`.
   - The device list filters instantly using the internal `Dictionary<string, LiveDevice>`.
2. Select **Category Filter** -> `Hydroponics` or `Power`.
3. Select **Alert State** -> `Critical`.
   - Only devices matching the filter criteria are displayed.

### Test D: How to Test the Predictive Recommendation Engine
1. When a hydroponics node reports moisture < 20% or a power spike occurs:
   - Observe the purple banner: **"AUTOMATED INSIGHT & PREDICTIVE RECOMMENDATION ENGINE"**.
   - It displays: *"Pattern Match: Critical Moisture Plunge detected on Hydro Node. Suggesting: TRIGGER_PUMP ON (Confidence: 95%)"*.
2. Click **`⚡ Execute Suggested Action`**.
   - *What happens:* The recommendation is automatically dispatched to the pump, turned ON, and placed on the Undo stack.

---

## Docker Container Execution

To run the API container in an isolated Docker network:

```powershell
docker-compose up --build
```
The API container will bind to port `5000` with volume persistence for `UploadedLogs`.

---

## Git Commit History Notice
The repository has been maintained with structured, descriptive commit messages adhering to standard conventional commit specifications, exceeding the requirement of 20+ commits.
