# GitLabClone

A full-featured GitLab clone built with **.NET 9** (ASP.NET Core) and **React 19**, designed for deployment on **Azure Container Apps**.

![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)
![React 19](https://img.shields.io/badge/React-19-61DAFB?logo=react)
![TypeScript](https://img.shields.io/badge/TypeScript-6.0-3178C6?logo=typescript)
![Tailwind CSS](https://img.shields.io/badge/Tailwind-v4-06B6D4?logo=tailwindcss)
![Azure](https://img.shields.io/badge/Azure-Container%20Apps-0078D4?logo=microsoftazure)

---

## Features

### Core
- **User Authentication** — JWT + Entra ID, RBAC (Admin / Maintainer / Developer / Reporter / Guest)
- **Project Management** — Create, update, delete with visibility controls (Private / Internal / Public)
- **Git Repository Hosting** — HTTP clone, push, pull via Smart HTTP protocol
- **Repository Browser** — File tree, file viewer with syntax line numbers, commit log
- **Issue Tracking** — Full CRUD with labels, assignees, comments, open/close status
- **CI/CD Pipelines** — YAML parser + simulated job runner with real-time status
- **Real-time Notifications** — SignalR with Redis backplane

### Architecture
- **Clean Architecture** — Domain / Application / Infrastructure / API layers
- **CQRS** — MediatR with validation and logging pipeline behaviors
- **Domain Events** — Dispatched after `SaveChangesAsync` for cross-cutting concerns
- **Soft Delete** — EF Core interceptors with global query filters

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| **Backend** | .NET 9, ASP.NET Core, C# 13, EF Core 9, MediatR, FluentValidation |
| **Frontend** | React 19, TypeScript 6, Vite 8, Tailwind CSS v4, React Query, Zustand |
| **Database** | SQL Server 2022 (Azure SQL) |
| **Cache** | Redis 7 (Azure Cache for Redis) |
| **Storage** | Azure Blob Storage (repository backups + CI artifacts) |
| **Auth** | JWT Bearer + HTTP Basic Auth (Git) + Entra ID (optional) |
| **Real-time** | SignalR with Redis backplane |
| **Git** | LibGit2Sharp (browsing) + git CLI (Smart HTTP protocol) |
| **Infrastructure** | Azure Bicep, Container Apps, Key Vault, ACR |
| **CI/CD** | GitHub Actions + GitLab CI templates |

---

## Project Structure

```
GitLabClone/
├── src/
│   ├── backend/
│   │   ├── src/
│   │   │   ├── GitLabClone.Domain/          # Entities, enums, value objects, events
│   │   │   ├── GitLabClone.Application/     # CQRS commands/queries, interfaces, DTOs
│   │   │   ├── GitLabClone.Infrastructure/  # EF Core, Git, Blob, JWT, CI parser
│   │   │   └── GitLabClone.Api/             # Controllers, middleware, SignalR hubs
│   │   └── Dockerfile
│   └── frontend/
│       ├── src/
│       │   ├── api/           # ky HTTP client, types, endpoints
│       │   ├── components/    # UI components (Button, Input, Badge, etc.)
│       │   ├── hooks/         # React Query hooks
│       │   ├── pages/         # Route pages (issues, pipelines, repository)
│       │   ├── routes/        # Router config, layouts, guards
│       │   ├── stores/        # Zustand stores (auth, notifications)
│       │   └── lib/           # Utilities, constants
│       ├── Dockerfile
│       └── nginx.conf
├── infra/
│   ├── main.bicep             # Orchestrator (subscription-scoped)
│   ├── modules/               # SQL, Redis, Storage, ACR, Key Vault, Container Apps
│   ├── deploy.sh              # One-command deployment script
│   └── setup-entra-id.sh      # Entra ID app registration
├── .github/workflows/
│   ├── ci.yml                 # Build, test, lint, security scan
│   └── cd.yml                 # Docker build, push, deploy, smoke test
├── .gitlab-ci.yml             # GitLab CI equivalent
├── docker-compose.yml         # Local dev (SQL + Redis + Azurite)
└── docs/
    └── security-checklist.md  # Security, scaling, monitoring guide
```

---

## Quick Start

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 22+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### 1. Start infrastructure services

```bash
docker compose up -d
```

This starts SQL Server (port 1433), Redis (port 6379), and Azurite (port 10000).

### 2. Run database migrations

```bash
cd src/backend
dotnet ef database update --project src/GitLabClone.Infrastructure --startup-project src/GitLabClone.Api
```

### 3. Start the API

```bash
dotnet run --project src/backend/src/GitLabClone.Api
```

API available at `http://localhost:5072` | Swagger at `http://localhost:5072/swagger`

### 4. Start the frontend

```bash
cd src/frontend
npm install
npm run dev
```

Frontend available at `http://localhost:5173`

---

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/v1/auth/register` | Register new user |
| `POST` | `/api/v1/auth/login` | Login (returns JWT) |
| `GET` | `/api/v1/auth/me` | Current user profile |
| `GET` | `/api/v1/projects` | List projects (paginated) |
| `POST` | `/api/v1/projects` | Create project |
| `PUT` | `/api/v1/projects/{slug}` | Update project |
| `DELETE` | `/api/v1/projects/{slug}` | Delete project |
| `GET` | `/api/v1/projects/{slug}/repository/tree` | File tree |
| `GET` | `/api/v1/projects/{slug}/repository/files` | File content |
| `GET` | `/api/v1/projects/{slug}/repository/commits` | Commit log |
| `GET` | `/api/v1/projects/{slug}/issues` | List issues |
| `POST` | `/api/v1/projects/{slug}/issues` | Create issue |
| `PUT` | `/api/v1/projects/{slug}/issues/{num}` | Update issue |
| `POST` | `/api/v1/projects/{slug}/issues/{num}/comments` | Add comment |
| `GET` | `/api/v1/projects/{slug}/pipelines` | List pipelines |
| `POST` | `/api/v1/projects/{slug}/pipelines` | Trigger pipeline |
| `GET` | `/api/v1/projects/{slug}/labels` | List labels |

Git Smart HTTP: `/{slug}.git/info/refs`, `/{slug}.git/git-upload-pack`, `/{slug}.git/git-receive-pack`

---

## Azure Deployment

### One-command deploy

```bash
chmod +x infra/deploy.sh
./infra/deploy.sh
```

### Manual Bicep deployment

```bash
az deployment sub create \
  --location eastus2 \
  --template-file infra/main.bicep \
  --parameters infra/main.parameters.json
```

### Architecture

```
Internet
  ├── Container Apps (Frontend / nginx)
  │     └── Reverse proxy → API
  └── Container Apps (API / .NET 9)
        ├── Azure SQL (data)
        ├── Azure Cache for Redis (cache + SignalR backplane)
        ├── Azure Blob Storage (repo backups + artifacts)
        └── Azure Key Vault (secrets)
```

---

## License

This project is for educational purposes. Not affiliated with GitLab Inc.
