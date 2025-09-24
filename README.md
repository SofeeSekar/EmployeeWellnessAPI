# EmployeeWellnessAPI

**Employee Wellness Program — Backend Service**  
.NET 7 Web API that manages wellness challenges, accepts high-volume progress submissions asynchronously, persists progress entries, and serves a cached real-time leaderboard.

---

## Overview

This service implements:
- `POST /api/challenges` — create a new challenge
- `POST /api/challenges/{challengeId}/progress` — submit progress (async via RabbitMQ)
- `GET /api/challenges/{challengeId}/leaderboard` — top-10 leaderboard (cached in Redis)
- `GET /api/users/{userId}/challenges/active` — get active challenges for a user

Stack: **.NET 7 (ASP.NET Core)**, **Entity Framework Core**, **RabbitMQ** (message queue), **Redis** (cache), **SQL Server** (or your choice DB). The solution is layered (Controllers → Services → Data/EF Core → Background Consumer).

---

## Architecture (high level)

1. Client → API (Controllers)
2. API publishes `ProgressMessage` to RabbitMQ (non-blocking)
3. A background `ProgressConsumer` (IHostedService) consumes messages:
   - persists `ProgressEntry` in DB (EF Core)
   - recomputes top-10 leaderboard aggregation
   - writes leaderboard JSON to Redis cache (e.g. `leaderboard:{challengeId}`)
4. `GET /leaderboard` reads from Redis (fast), falls back to DB if cache miss.

> This pattern decouples write traffic (high volume) from read traffic (read-heavy leaderboard).

---

## Project Structure

EmployeeWellnessAPI/
├─ EmployeeWellnessAPI.sln
├─ src/
│ ├─ EmployeeWellnessAPI.Api (Controllers)
│ ├─ EmployeeWellnessAPI.Services (Background consumer, RabbitMQ service)
│ ├─ EmployeeWellnessAPI.Data (DbContext, Migrations)
│ └─ EmployeeWellnessAPI.Models (DTOs, domain models)
├─ docker-compose.yml (optional for RabbitMQ + Redis)
└─ README.md


---

## Design decisions & trade-offs

- **RabbitMQ** for reliable, ordered, persisted message queue and decoupling producers/consumers.
- **BackgroundService (IHostedService)**: consumer runs in separate worker thread inside API process; scalable by running multiple consumers if needed.
- **Redis** as leaderboard cache to serve read-heavy requests with low latency.
- **EF Core** for data modeling and migrations. For extremely high write volumes, consider a write-optimized store or bulk-batch writes in consumer.
- **Acknowledgements**: consumer acknowledges (Ack/Nack) messages explicitly to avoid message-loss.
- **Id types**: Use GUIDs for `UserId`/`ChallengeId` for safe distributed IDs.

---

## Running locally — prerequisites

- .NET 7 SDK
- Docker (to run RabbitMQ and Redis easily)
- Git
- (Optional) SQL Server or LocalDB / Dockerized SQL Server

---

## Quick start (local)

1. Start RabbitMQ + Redis:
   - Use the `docker-compose.yml` provided (or create one, see below).
   - From repo root:
     ```powershell
     docker-compose up -d
     ```
   - RabbitMQ UI: `http://localhost:15672` (guest/guest)  
   - Redis: `localhost:6379`

2. Create `appsettings.Development.json` (or set environment variables) with proper connection strings:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EmployeeWellnessDb;Trusted_Connection=True;"
     },
     "RabbitMQ": {
       "Host": "localhost",
       "Username": "guest",
       "Password": "guest"
     },
     "Redis": {
       "Configuration": "localhost:6379"
     }
   }
Do not commit secrets. Add appsettings.Development.json to .gitignore if it contains credentials.

Apply EF migrations (from project folder containing DbContext):

dotnet tool install --global dotnet-ef     # if not installed
dotnet ef database update


(Or use Update-Database in Package Manager Console.)

Run the API:

dotnet run


Default URL: http://localhost:5151 (your logs will show actual port).

Test endpoints (PowerShell examples):

Create challenge:

$headers = @{"Content-Type"="application/json"}
$body = @{name="10k Steps"; startDate="2025-09-22T00:00:00"; endDate="2025-10-22T00:00:00"; goal="Walk 10,000 steps"} | ConvertTo-Json
Invoke-WebRequest -Uri "http://localhost:5151/api/challenges" -Method POST -Headers $headers -Body $body


Add participant:

$challengeId = "<paste-challenge-id>"
$body = @{userId="<user-guid>"} | ConvertTo-Json
Invoke-WebRequest -Uri "http://localhost:5151/api/challenges/$challengeId/participants" -Method POST -Headers $headers -Body $body


Submit progress:

$body = @{userId="<user-guid>"; value=5000; timestamp=(Get-Date -Format "yyyy-MM-ddTHH:mm:ss")} | ConvertTo-Json
Invoke-WebRequest -Uri "http://localhost:5151/api/challenges/$challengeId/progress" -Method POST -Headers $headers -Body $body


Get leaderboard:

Invoke-WebRequest -Uri "http://localhost:5151/api/challenges/$challengeId/leaderboard" -Method GET

Example docker-compose.yml
version: "3.8"
services:
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest

  redis:
    image: redis:7
    ports:
      - "6379:6379"

How to run EF migrations (if you added or changed models)
dotnet ef migrations add <Name> --project EmployeeWellnessAPI.Data --startup-project EmployeeWellnessAPI.Api
dotnet ef database update --project EmployeeWellnessAPI.Data --startup-project EmployeeWellnessAPI.Api

Known limitations & future work

Current consumer processes messages one-by-one; for very high write throughput consider batching and bulk inserts.

Leaderboard TTL is short; consider incremental updates and optimistic locking for high concurrency.

Add integration tests & end-to-end test harness.

Contact / Submission

Repo should contain:

Source code (solution + project folders)

Migrations folder

README.md

docker-compose.yml

sample appsettings.json.example

For assessment: include a short architecture diagram (svg/png) and the README explaining choices and trade-offs.


---

# 2) Create the files locally (README, appsettings.example, .gitignore)

In VS Code you can create files by right click → New File. Or use PowerShell:

```powershell
# from your solution root (where EmployeeWellnessAPI.sln exists)
New-Item -Path . -Name "README.md" -ItemType File -Value (Get-Content -Raw ".\README_template.md")
# if you prefer to open VS Code and paste the README content: code .


Create appsettings.json.example (do not commit real credentials):

{
  "ConnectionStrings": {
    "DefaultConnection": "Server=<YOUR_SERVER>;Database=EmployeeWellnessDb;User Id=<USER>;Password=<PASSWORD>;"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest"
  },
  "Redis": {
    "Configuration": "localhost:6379"
  }
}