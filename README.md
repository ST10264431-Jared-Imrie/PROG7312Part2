# Smart-X IoT Mesh System — Data Ingestion Engine (PoE Part 1)

## System Overview
Smart-X is a high-performance Internet of Things (IoT) monitoring and management ecosystem designed for modern South African edge computing deployments (such as automated real-time agricultural operations, utility trackers, and smart grid installations). The system is engineered to ingest, validate, and process continuous multi-typed telemetry streams transmitted from edge microcontrollers (e.g., ESP32 chips).

This repository contains the backend API infrastructure and interactive desktop configuration dashboard for Part 1 of the Portfolio of Evidence (PoE).

---

## Architectural Pillars
The client-facing dashboard is designed around three primary architectural pillars:
1. **Sensor Data Ingestion and Telemetry:** [ENABLED] Core ingestion layer, live sensor configuration, payload attachment, and visual status tracking.
2. **Real-Time Command Stream and History:** [DISABLED] To be implemented in Part 2.
3. **Network Topology and Mesh Routing:** [DISABLED] To be implemented in the final PoE.

---

## Technical & Object-Oriented C# Implementation
To ensure optimal performance and eliminate operational bottlenecks on resource-constrained gateway environments, the following C# concepts are integrated:

- **Generics (`TelemetryPacket<T>`):** A reusable generic wrapper class that handles disparate incoming data structures (floats for temperature/moisture, integers for power wattage, booleans for switch states) without triggering costly boxing and unboxing performance overhead.
- **Operator Overloading (`SmartMeter`):** Overloaded arithmetic (`+`) and comparison (`>`, `<`) operators on telemetry data structures to enable direct power load aggregation and fast threshold delta comparisons in code.
- **Jagged Arrays & Collections (`IngestionBuffer`):** Multi-dimensional/jagged array structures (`double[][]`) to handle incoming historical telemetry batches sequentially before transferring them into optimized `List<T>` collections.
- **Recursion (`DeploymentNode`):** A recursive validation algorithm that parses hierarchical device deployment trees to verify node configuration safety across multi-tier zones (e.g., Sub-Zone B -> Zone 1 -> Facility A).
- **Minimal Web API (`ASP.NET Core`):** High-throughput backend API receiving telemetry payloads and supporting multipart file/log attachments.
- **Dynamic Engagement Heatmap:** Pre-attentive visual status indicators that dynamically update color states (Green for normal, High-Contrast Red for anomalous threshold spikes) to prevent operator drop-off and reduce cognitive load.

---

## Prerequisites
Ensure the following tools are installed on your system before running the project:
* [.NET 8.0 SDK or .NET 10 SDK](https://dotnet.microsoft.com/download)
* [Visual Studio 2022](https://visualstudio.microsoft.com/) (with *.NET desktop development* and *ASP.NET and web development* workloads)
* [Docker Desktop](https://www.docker.com/) (Optional for container deployment)
* [Git](https://git-scm.com/)

---

## Setup, Build, and Run Instructions

### 1. Clone the Repository
```bash
git clone https://github.com/ST10264431/ST10264431PROG7312POEPART1.git
cd SmartX-IoT-Mesh
```

### 2. Restore Dependencies
```bash
dotnet restore
```

### 3. Build the Solution
```bash
dotnet build
```

### 4. Run the API Backend
Open your terminal and run the API project:

```bash
dotnet run --project SmartX.API
```
The API will start hosting locally at `http://localhost:5000`. You can navigate to `http://localhost:5000/swagger` to inspect endpoints.

### 5. Launch the Client Dashboard
Open a second terminal window and start the WPF dashboard client:

```bash
dotnet run --project SmartX.Dashboard
```

### 6. Docker Container Execution (Optional)
```bash
docker-compose up --build
```

---

## File and Log Upload Usage
1. Open and authenticate on the **SmartX Dashboard** application (Default: `admin@smartx.io` / `AdminPassword123!`).
2. Navigate to the Sensor Payload Management panel in the application.
3. Enter device metadata (MAC Address, Location, Category, Value).
4. Click **Attach Log/Photo** to trigger an `OpenFileDialog`.
5. Select hardware configuration files (`.log`, `.txt`) or deployment photos (`.png`, `.jpg`).
6. Click **Submit Telemetry Data** to send the payload and attached media to the API server.
