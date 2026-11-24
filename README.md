# Corporate Responsibility Generator

Generates random corporate responsibility statements paired with thematically matched background photos.

## Architecture

This repository is a monorepo with separate frontend and backend services:

- **`frontend/`**: React 19 + Vite 8 + CSS SPA (`Vitest` + React Testing Library)
- **`backend/`**: C# / ASP.NET Core 10 Minimal API (`CorporateResponsibility.Api` + `xUnit` integration tests, `IHttpClientFactory`, and 6-hour `IMemoryCache` for Pixabay hits)

### API Endpoints (`backend/`)

- `GET /api/responsibility` — Generates a corporate phrase, extracts a search term, and returns a cached/matched Pixabay photo URL (`{ "phrase": "...", "imageUrl": "...", "searchQuery": "..." }`)
- `GET /` — Returns a generated corporate phrase (`{ "phrase": "..." }`)
- `GET /api/phrase` — Returns a generated corporate phrase (`{ "phrase": "..." }`)
- `GET /api/phrases?count=5` — Returns a batch of `1`–`20` generated phrases
- `GET /health` — Service health check (`{ "status": "ok" }`)
- `GET /openapi/v1.json` — OpenAPI 3 specification

## Local Development

### 1. Configure `PIXABAY_API_KEY`

Copy `backend/.env.example` to `backend/.env` and set your Pixabay API key:
```bash
cp backend/.env.example backend/.env
```

### 2. Run the C# Backend (`http://localhost:5080`)

**Using Docker (no local .NET SDK required):**
```bash
docker build --target test -t corporate-responsibility-api-test ./backend
docker build -t corporate-responsibility-api ./backend
docker run --rm --env-file ./backend/.env -p 5080:5080 corporate-responsibility-api
```

**Or using the .NET 10 SDK natively:**
```bash
cd backend
dotnet test
PIXABAY_API_KEY=your_key dotnet run --project CorporateResponsibility.Api --urls http://localhost:5080
```

### 3. Run the React Frontend (`http://localhost:5173`)

```bash
cd frontend
npm install
npm run dev
```

In development, Vite proxies `/api/*` requests to `http://localhost:5080`.

## Live Demo & Railway Deployment

- **Railway (Production):** https://ajlato-corporate-responsibility.up.railway.app/

Deploy as two Railway services from this repository:
1. **Backend service:** Set **Root Directory** to `backend` (Railway builds `backend/Dockerfile` and injects `PORT`) and set `PIXABAY_API_KEY` in service variables. Optionally set `ALLOWED_ORIGINS` to the frontend origin.
2. **Frontend service:** Set **Root Directory** to `frontend` and set `VITE_API_URL` to the backend service's public URL (e.g. `https://<backend-service>.up.railway.app`).
