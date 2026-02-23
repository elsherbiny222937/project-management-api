# 🗂️ Project Management API

A comprehensive **Project Management REST API** built with **.NET 8**, **Clean Architecture**, **CQRS**, **SignalR**, and **Hangfire**. Designed for real-world team collaboration with role-based access, real-time notifications, and background automation.

---

## 📋 Table of Contents

- [Features](#-features)
- [Tech Stack](#-tech-stack)
- [Architecture](#-architecture)
- [Prerequisites](#-prerequisites)
- [Getting Started (Local)](#-getting-started-local)
- [Environment Variables](#-environment-variables)
- [Running with Docker](#-running-with-docker)
- [Kubernetes Deployment](#-kubernetes-deployment)
- [API Documentation](#-api-documentation)
- [Default Seed Data](#-default-seed-data)
- [SignalR Usage](#-signalr-real-time-usage)
- [Background Jobs](#-background-jobs)
- [Project Structure](#-project-structure)

---

## ✨ Features

| Feature | Details |
|---|---|
| **Projects** | CRUD, pagination, sorting, filtering, duplication, member management |
| **Tasks** | Full CRUD, bulk status/assignee update, bulk delete, Sprint & Epic assignment, time logging, blockers, comments |
| **Epics & Sprints** | Organize tasks into epics and time-boxed sprints with auto state transitions |
| **Auth** | JWT + Refresh Tokens, Role-based Authorization (Admin / ProjectManager / Developer) |
| **Real-Time** | SignalR hub — live task created/updated/deleted/status-changed events |
| **Background Jobs** | Hangfire — daily overdue task email notifications + hourly sprint state transitions |
| **Caching** | In-memory cache with prefix-based invalidation |
| **Security** | Rate limiting, security headers, input sanitization, OWASP best practices |
| **Audit Log** | Automatic change tracking for all entities |
| **Soft Delete** | Global query filter — deleted records never appear in results |
| **API Versioning** | URL-based versioning (`/api/v1/...`) |
| **Swagger** | Full interactive documentation with JWT support |
| **Health Checks** | `/health/live` and `/health/ready` endpoints |
| **Idempotency** | Idempotency key filter for safe retries |

---

## 🛠️ Tech Stack

- **.NET 8** (LTS)
- **ASP.NET Core Web API**
- **Entity Framework Core** (SQLite for dev / SQL Server for prod)
- **MediatR** — CQRS pattern
- **Autofac** — Dependency injection container
- **FluentValidation** — Request validation pipeline
- **AutoMapper** — Object mapping
- **Hangfire** — Background jobs (memory storage for dev)
- **SignalR** — Real-time WebSocket communication
- **ASP.NET Core Identity** — User management
- **JWT Bearer** — Authentication
- **Swagger / Swashbuckle** — API documentation
- **Asp.Versioning** — API versioning

---

## 🏗️ Architecture

```
ProjectManagementApi.sln
└── src/
    ├── ProjectManagement.Domain          # Entities, Enums, Interfaces, Domain Events
    ├── ProjectManagement.Application     # CQRS Commands/Queries, Validators, DTOs, Behaviors
    ├── ProjectManagement.Infrastructure  # EF Core, Repositories, Identity, SignalR, Jobs, Services
    └── ProjectManagement.Api             # Controllers, Middleware, Filters, Program.cs
```

**Dependency Direction:** `Api → Application → Domain` ← `Infrastructure`

**Patterns Used:**
- CQRS (Command Query Responsibility Segregation) via MediatR
- Repository + Unit of Work
- Clean Architecture (Domain-centric)
- Pipeline Behaviors (Validation, Caching, Logging)
- Domain Events

---

## 📦 Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 8.0+ |
| Git | Any |
| Docker (optional) | 24+ |
| kubectl (optional) | 1.28+ |

---

## 🚀 Getting Started (Local)

### 1. Clone the Repository

```bash
git clone https://github.com/YOUR_USERNAME/project-management-api.git
cd project-management-api
```

### 2. Configure Settings

Copy and edit the settings file:

```bash
cp src/ProjectManagement.Api/appsettings.Development.json.example src/ProjectManagement.Api/appsettings.Development.json
```

Edit `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=ProjectManagement.db"
  },
  "Jwt": {
    "Secret": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "ProjectManagementApi",
    "Audience": "ProjectManagementApi"
  },
  "Email": {
    "From": "noreply@example.com",
    "SmtpHost": "localhost",
    "SmtpPort": 587
  }
}
```

### 3. Apply Database Migrations

```bash
cd src/ProjectManagement.Api
dotnet ef database update --project ../ProjectManagement.Infrastructure
```

> **Note:** Migrations are in `ProjectManagement.Infrastructure`. The DB is auto-seeded with default users and roles on first run.

### 4. Run the API

```bash
dotnet run --project src/ProjectManagement.Api
```

The API will be available at:
- **API:** `http://localhost:5000`
- **Swagger UI:** `http://localhost:5000/swagger`
- **Hangfire Dashboard:** `http://localhost:5000/hangfire`
- **Health (live):** `http://localhost:5000/health/live`
- **Health (ready):** `http://localhost:5000/health/ready`

---

## ⚙️ Environment Variables

| Variable | Description | Default |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | Database connection string | SQLite file |
| `Jwt__Secret` | JWT signing key (min 32 chars) | Dev key |
| `Jwt__Issuer` | JWT issuer name | `ProjectManagementApi` |
| `Jwt__Audience` | JWT audience name | `ProjectManagementApi` |
| `Email__From` | Sender email address | — |
| `Email__SmtpHost` | SMTP server host | — |
| `Email__SmtpPort` | SMTP server port | `587` |
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Development` |

---

## 🐳 Running with Docker

### Build and Run (Single Container)

```bash
docker build -t project-management-api .
docker run -p 8080:8080 \
  -e Jwt__Secret="YourSuperSecretKeyThatIsAtLeast32CharactersLong!" \
  project-management-api
```

### Docker Compose (API + SQL Server)

```bash
docker-compose up -d
```

This starts:
- **API** on `http://localhost:8080`
- **SQL Server** on port `1433`
- **Swagger** at `http://localhost:8080/swagger`

To stop:

```bash
docker-compose down
```

To rebuild after code changes:

```bash
docker-compose up -d --build
```

---

## ☸️ Kubernetes Deployment

### Prerequisites

```bash
# Install kubectl and have a cluster (minikube, kind, or cloud)
minikube start
```

### Deploy

```bash
# Create namespace
kubectl apply -f k8s/namespace.yaml

# Create secrets
kubectl apply -f k8s/secret.yaml

# Deploy SQL Server
kubectl apply -f k8s/sqlserver-deployment.yaml

# Deploy the API
kubectl apply -f k8s/api-deployment.yaml

# Expose via ingress (optional)
kubectl apply -f k8s/ingress.yaml
```

### Verify

```bash
kubectl get pods -n project-management
kubectl get services -n project-management

# Port-forward for local testing
kubectl port-forward svc/project-management-api 8080:80 -n project-management
```

### Scale

```bash
kubectl scale deployment project-management-api --replicas=3 -n project-management
```

---

## 📖 API Documentation

Full interactive docs available at **`/swagger`** when running.

### Authentication Flow

```
1. POST /api/v1/auth/register   → Create account
2. POST /api/v1/auth/login      → Get access_token + refresh_token
3. Add header: Authorization: Bearer {access_token}
4. POST /api/v1/auth/refresh-token → Renew expired token
```

### Key Endpoints Summary

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| POST | `/api/v1/auth/register` | Register user | Public |
| POST | `/api/v1/auth/login` | Login | Public |
| POST | `/api/v1/auth/refresh-token` | Refresh JWT | Public |
| GET | `/api/v1/projects` | List projects (paginated) | Any role |
| POST | `/api/v1/projects` | Create project | Any role |
| GET | `/api/v1/projects/{id}` | Get project details | Member |
| PUT | `/api/v1/projects/{id}` | Update project | Member |
| DELETE | `/api/v1/projects/{id}` | Delete project | Admin/PM |
| POST | `/api/v1/projects/{id}/duplicate` | Duplicate project | Member |
| POST | `/api/v1/projects/{id}/members` | Add member | Admin/PM |
| GET | `/api/v1/projects/{projectId}/tasks` | List tasks | Member |
| POST | `/api/v1/projects/{projectId}/tasks` | Create task | Member |
| GET | `/api/v1/tasks/{id}` | Get task | Authenticated |
| PUT | `/api/v1/tasks/{id}` | Update task | Authenticated |
| DELETE | `/api/v1/tasks/{id}` | Delete task | Authenticated |
| PUT | `/api/v1/tasks/bulk-status-update` | Bulk status update | Authenticated |
| PUT | `/api/v1/tasks/bulk-assignee-update` | Bulk assignee update | Authenticated |
| DELETE | `/api/v1/tasks/bulk-delete` | Bulk delete | Authenticated |
| POST | `/api/v1/tasks/{id}/log-time` | Log hours | Authenticated |
| POST | `/api/v1/tasks/{id}/blockers` | Add blocker | Authenticated |
| POST | `/api/v1/tasks/{id}/comments` | Add comment | Authenticated |

### Pagination & Filtering

All list endpoints support:

```
GET /api/v1/projects?pageNumber=1&pageSize=10&searchTerm=web&sortBy=name&sortDescending=false
GET /api/v1/projects/{id}/tasks?statusFilter=InProgress&priorityFilter=High&assigneeFilter=userId
```

---

## 🔑 Default Seed Data

On first run, the database is seeded with:

| Role | Email | Password |
|---|---|---|
| Admin | `admin@projectmgmt.com` | `Admin@123` |
| ProjectManager | `pm@projectmgmt.com` | `Manager@123` |
| Developer | `dev@projectmgmt.com` | `Dev@123` |

> ⚠️ **Change these credentials immediately in production!**

---

## 📡 SignalR Real-Time Usage

### Connect

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5000/hubs/tasks?access_token=YOUR_JWT")
  .build();

await connection.start();
```

### Join a Project Room

```javascript
await connection.invoke("JoinProjectGroup", "your-project-id");
```

### Listen for Events

```javascript
connection.on("TaskCreated",       (data) => console.log("Task created:", data));
connection.on("TaskUpdated",       (data) => console.log("Task updated:", data));
connection.on("TaskDeleted",       (data) => console.log("Task deleted:", data));
connection.on("TaskStatusChanged", (data) => console.log("Status changed:", data));
```

### Leave a Project Room

```javascript
await connection.invoke("LeaveProjectGroup", "your-project-id");
```

---

## ⏰ Background Jobs

Managed by **Hangfire** — dashboard at `/hangfire`

| Job | Schedule | Description |
|---|---|---|
| `overdue-task-notification` | Daily | Sends email notifications for all overdue tasks |
| `sprint-state-transition` | Hourly | Auto-transitions sprint states (Planned → Active → Completed) |

---

## 📁 Project Structure

```
src/
├── ProjectManagement.Domain/
│   ├── Common/          # BaseEntity, IDomainEvent
│   ├── Entities/        # Project, ProjectTask, Epic, Sprint, ApplicationUser, ...
│   ├── Enums/           # ProjectStatus, TaskPriority, StatusCategory, ...
│   ├── Events/          # Domain events (TaskCreated, TaskStatusChanged, ...)
│   └── Interfaces/      # IRepository, IUnitOfWork, IServices
│
├── ProjectManagement.Application/
│   ├── Behaviors/       # Validation & Caching pipeline behaviors
│   ├── DTOs/            # Response DTOs
│   ├── EventHandlers/   # MediatR domain event handlers
│   ├── Features/
│   │   ├── Auth/        # Register, Login, RefreshToken commands
│   │   ├── Projects/    # CRUD + Duplicate + AddMember commands & queries
│   │   ├── Tasks/       # CRUD + Bulk + Sprint/Epic assignment commands & queries
│   │   ├── Epics/       # Epic CRUD
│   │   └── Sprints/     # Sprint CRUD
│   ├── Mappings/        # AutoMapper profiles
│   └── Validators/      # FluentValidation validators
│
├── ProjectManagement.Infrastructure/
│   ├── BackgroundJobs/  # Hangfire job implementations
│   ├── DependencyInjection/ # MediatR, FluentValidation, AutoMapper registration
│   ├── Identity/        # JWT TokenService, AuthHandlers
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   ├── Migrations/
│   │   ├── Repositories/
│   │   └── Seed/
│   ├── Services/        # EmailService, CacheService, CurrentUserService
│   └── SignalR/         # TaskHub
│
└── ProjectManagement.Api/
    ├── Authorization/   # ProjectAuthorizationHandler
    ├── Controllers/v1/  # Auth, Projects, Tasks, Epics, Sprints, Users
    ├── Filters/         # IdempotencyFilter
    ├── Middleware/       # GlobalExceptionHandler, SecurityHeaders
    └── Program.cs
```

---

## 🔒 Security Considerations

- JWT tokens expire after **2 hours**; use refresh tokens for renewal
- Rate limiting on auth endpoints: **5 requests/minute**
- Security headers applied via `UseSecurityHeaders()` middleware
- Input sanitization middleware in pipeline
- Project-level authorization — users only access their own projects (unless Admin)
- Soft delete — data is never hard-deleted from the database
- Passwords require uppercase, lowercase, and digit (min 6 chars)

---

## 🧪 Testing

```bash
dotnet test
```

---

## 📄 License

MIT License — see [LICENSE](LICENSE) for details.
