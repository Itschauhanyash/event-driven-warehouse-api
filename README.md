# Warehouse Integration API

> **Technologies:** C# / .NET 8, RabbitMQ, Docker, Prometheus, Grafana

This repository contains a robust, containerized microservices orchestration designed to handle real-time logistical data processing, including bin allocation and order assignment workflows for WMS/WCS platforms.

---

## 🏗️ System Architecture

The project consists of three core components running simultaneously through Docker Compose. This ensures scalability, reliability, and modularity:

1. **RESTful Web API (C# .NET 8)**
   - Exposes HTTP endpoints for interacting with the logistical system.
   - Responsible for receiving synchronous requests and transforming them into asynchronous background tasks to ensure high-throughput execution.
   
2. **Message Broker (RabbitMQ)**
   - Acts as the central pipeline for decoupling services.
   - When the API receives a task (like assigning an order), it doesn't process it immediately. Instead, it publishes a JSON payload to RabbitMQ.
   - This ensures that if there's a flood of incoming requests, the system won't crash—they are safely stacked in the queue.

3. **Background Worker Service**
   - An `IHostedService` continuously running inside the API container.
   - It subscribes to the RabbitMQ queues. The moment a message drops into the queue, the worker picks it up and processes the integration (simulating the time it takes to integrate with a WMS or sortation hardware).

4. **Observability Stack (Prometheus & Grafana)**
   - **Prometheus** scrapes system and application metrics securely exposed by the .NET API at the `/metrics` endpoint.
   - **Grafana** binds to Prometheus to offer live, visual instrumentation of request times, queue pressure, and container health.

---

## 📂 Project Structure

```text
📁 WarehouseIntegrationAPI
 ├── 📁 Controllers/
 │    └── WarehouseController.cs  <-- The REST endpoints (e.g., POST /api/warehouse/allocate-bin)
 ├── 📁 Services/
 │    ├── MessageProducer.cs      <-- Class built to push JSON payloads to RabbitMQ
 │    └── IntegrationWorker.cs    <-- Background daemon that consumes the RabbitMQ messages
 ├── Program.cs                   <-- Dependency injection setup and Prometheus route mapping
 ├── Dockerfile                   <-- Multi-stage build definition for compiling the C# project
 ├── docker-compose.yml           <-- The glue tying the API, RabbitMQ, Prometheus, and Grafana together
 ├── prometheus.yml               <-- Configuration dictating that Prometheus must scrape the API every 15s
 └── WarehouseIntegrationAPI.csproj
```

---

## 🚀 How to Run the Application

Because the entire ecosystem is containerized, you do not need the .NET SDK or RabbitMQ installed locally on your host machine.

### Prerequisites
- **Docker Desktop** (or Docker Engine + Docker Compose) installed and running.

### Execution
1. Open a terminal and navigate to the `WarehouseIntegrationAPI` directory:
   ```bash
   cd WarehouseIntegrationAPI
   ```
2. Run the Docker Compose up command to build the C# project and download necessary container images:
   ```bash
   docker-compose up --build -d
   ```
   *(The `-d` flag runs the containers in the background).*

---

## 🔬 Deep Dive: System Architecture & Data Flow

This project utilizes an **Event-Driven Architecture**, decoupling the receiving of web requests from the heavy-lifting logic.

### 1. The Entry Point: `WarehouseController.cs`
This is your standard HTTP REST interface. When a handheld scanner makes an API call here (e.g. `POST /api/warehouse/allocate-bin`), the controller accepts it. **Crucially, it does not process the operation.** To ensure scanners are never bottlenecked waiting for slow server operations, this controller simply validates the payload, passes it to the `MessageProducer`, and immediately returns an `HTTP 200 OK` to the user.

### 2. The Publisher: `MessageProducer.cs`
Injected directly into the API Controller, this class acts as the bridge to RabbitMQ. When it receives a validated payload, it serializes it to JSON UTF-8 bytes and publishes it directly into a queue (such as `bin_allocation_queue` or `order_assignment_queue`).

### 3. The Consumer: `IntegrationWorker.cs`
This is an `IHostedService` (a background daemon) running continuously in the `.NET` container.
It registers an `EventingBasicConsumer` that sits idle. The exact millisecond `MessageProducer` pushes a JSON payload into the queue, RabbitMQ dispatches it to this worker. The worker extracts the JSON, decodes it, and performs the theoretical heavy lifting (simulated here via `Thread.Sleep()`). Because this runs in a background thread, massive traffic spikes will just stack up in the queue instead of crashing the web server.

### 4. The Orchestration: `docker-compose.yml`
This glues the ecosystem together, spinning up exactly 4 sub-servers:
- **`rabbitmq`**: The message broker.
- **`api`**: The compiled .NET 8 codebase.
- **`prometheus`**: A background daemon that scrapes `/metrics` off the `.NET` runtime every 15 seconds.
- **`grafana`**: Provides the visual web dashboard for Prometheus's underlying metric data.

---

## 📊 Monitoring and Tooling Interfaces

Once the application is running via Docker Compose, you can access the following graphical interfaces via a web browser:

| Service | Local Address | Credentials | Purpose |
| :--- | :--- | :--- | :--- |
| **Warehouse API** | `http://localhost:8080` | N/A | Exposing endpoints |
| **RabbitMQ Admin** | `http://localhost:15672` | `guest` / `guest` | Visualizing message queue traffic and exchanges. |
| **Prometheus** | `http://localhost:9090` | N/A | Querying raw system metrics and scrape speeds. |
| **Grafana** | `http://localhost:3000` | `admin` / `admin` | Setting up visual dashboards for system health. |

### How to Verify Live Processing
To see the asynchronous processing in action, we need to monitor the logs while simultaneously triggering an API request.

1. Fetch the live tailing logs for the API/Worker container via:
   ```bash
   docker logs warehouseintegrationapi-api-1 -f
   ```
2. In a separate terminal tab, send an API request to trigger the queue.
   
   **For Mac/Linux users (cURL):**
   ```bash
   curl -X POST "http://localhost:8080/api/warehouse/allocate-bin" \
        -H "Content-Type: application/json" \
        -d '{"OrderId":"123", "Sku":"XYZ", "Quantity": 5}'
   ```

   **For Windows users (PowerShell):**  
   *(Windows uses `Invoke-RestMethod` instead of standard `curl` escaping)*
   ```powershell
   Invoke-RestMethod -Uri "http://localhost:8080/api/warehouse/allocate-bin" -Method Post -ContentType "application/json" -Body '{"OrderId":"123", "Sku":"XYZ", "Quantity": 5}'
   ```

3. Instantly, in your first terminal tab where docker logs are streaming, you will see output proving the API published the message and the Background Worker consumed it asynchronously:
   ```text
   [x] Sent to bin_allocation_queue: {"OrderId":"123","Sku":"XYZ","Quantity":5}
   [x] Processed message from ... {"OrderId":"123","Sku":"XYZ","Quantity":5}
   ```

---

## 🛠️ Future Improvements / Scaling
- **Dead-Letter Queues (DLQ):** To ensure that if the WCS systems are completely offline, messages that throw exceptions are parked into a retry-queue to avoid system halts.
- **Authorization:** Adding JWT validation middleware in `Program.cs` before accepting warehouse POST commands.
- **Unit Testing:** Using `xUnit` + `Moq` to assert the behavior of the `WarehouseController` ensuring the producer fires exactly once per request.
