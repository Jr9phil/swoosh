# Swoosh

A privacy-focused task management app. All task content is encrypted at rest — the database stores no plaintext.

## Stack

| Layer | Technology |
|---|---|
| Backend | .NET 8 ASP.NET Core Web API |
| Frontend | Vue 3 + TypeScript + Vite + Tailwind CSS v4 + DaisyUI |
| Database | PostgreSQL (via EF Core) |
| Auth | JWT (2-hour expiry) + BCrypt |
| Encryption | AES-GCM, per-user key derivation |

## Features

- **End-to-end encrypted tasks** — title, notes, deadline, priority, pinned status, rating, icon, and grace-period timer are all encrypted before hitting the database
- **Task organization** — priority levels (none/low/medium/high), pinning, drag-to-reorder within and across groups
- **Subtasks** — nest tasks under a parent; parent can't be completed while subtasks with deadlines are open
- **Grace-period timers** — optionally delay the "overdue" state by up to 8 hours (or until midnight, whichever is sooner) after a deadline passes
- **Timeline header** — horizontal week-view strip for jumping to tasks by date
- **CSV export** — downloads all decrypted tasks as a CSV file
- **Key rotation** — bump `ActiveKeyVersion` and the background service re-encrypts all tasks automatically

## Project structure

```
swoosh/
├── src/Swoosh.Api/          # .NET 8 Web API
│   ├── Controllers/         # Auth + Tasks endpoints
│   ├── Domain/              # TaskItem, User entities
│   ├── Dtos/                # Request/response models
│   ├── Security/            # AES-GCM encryption, JWT, key derivation
│   └── Services/            # TaskService, AuthService, ReencryptionService
├── frontend/swoosh-web/     # Vue 3 SPA
│   └── src/
│       ├── components/      # TaskItem, TaskEdit, TaskMenu, ImgHeader, …
│       ├── stores/          # Pinia stores (auth, tasks)
│       ├── views/           # TasksView, LoginView, RegisterView, …
│       ├── types/           # Task, Priority, Icon types
│       └── directives/      # v-animate-sync, v-click-outside
└── compose.yaml             # Docker Compose (PostgreSQL + API)
```

## Getting started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- PostgreSQL (or Docker)

### Option A — Docker (full stack)

Create a `.env` file in the repo root:

```env
DB_PASSWORD=yourpassword
JWT_SECRET=a-long-random-secret
ENCRYPTION_KEY=a-32-byte-base64-key
```

Then:

```bash
docker compose up --build
```

The API is available at `http://localhost:5250` and the Vite dev server proxies the frontend to it.

### Option B — local development

**1. Secrets** (one-time setup)

```bash
cd src/Swoosh.Api
dotnet user-secrets set "ConnectionStrings:SwooshDb" "Host=localhost;Database=swoosh;Username=postgres;Password=yourpassword"
dotnet user-secrets set "Jwt:Key" "a-long-random-secret"
dotnet user-secrets set "Jwt:Issuer" "swoosh"
dotnet user-secrets set "Jwt:Audience" "swoosh"
dotnet user-secrets set "Encryption:ActiveKeyVersion" "1"
dotnet user-secrets set "Encryption:Keys:1" "your-32-byte-base64-key"
```

**2. Database**

```bash
dotnet ef database update --project src/Swoosh.Api
```

**3. Run the API**

```bash
dotnet run --project src/Swoosh.Api
# → http://localhost:5250  (Swagger at /swagger)
```

**4. Run the frontend**

```bash
cd frontend/swoosh-web
npm install
npm run dev
# → http://localhost:5173
```

## Commands reference

### Backend

```bash
dotnet build
dotnet run --project src/Swoosh.Api
dotnet ef migrations add <Name> --project src/Swoosh.Api
dotnet ef database update --project src/Swoosh.Api
```

### Frontend

```bash
npm run dev      # Vite dev server with HMR
npm run build    # Type-check + production build
npm run lint     # ESLint
```

## Encryption design

Every sensitive field on a task is encrypted with **AES-GCM** before storage:

1. A master key (from config) is combined with a per-user random `EncryptionSalt` via PBKDF2 to produce a user-specific key.
2. Each field is encrypted independently with a random 12-byte nonce and a 16-byte auth tag.
3. The resulting blob (`nonce || tag || ciphertext`) is base64-encoded and stored as a text column.
4. `null` values are represented by a `__NULL__` sentinel before encryption so the column is never empty.

The active key version is tracked per-task (`KeyVersion`). The `ReencryptionService` background job migrates stale tasks to the current key in batches of 50, running every 10 minutes.

## Key rotation

1. Add a new key to config: `Encryption:Keys:2 = <new-key>`
2. Set `Encryption:ActiveKeyVersion = 2`
3. Restart the API — new tasks are written with key 2 immediately; the background job migrates existing tasks automatically.
