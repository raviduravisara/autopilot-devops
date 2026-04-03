# AutoPilot DevOps Project

End-to-end full-stack project using ASP.NET (backend), React (frontend), Docker, GitHub Actions, and Azure.

## Goals
- Build and run the application locally with Docker.
- Follow a clean Git workflow using `feature/*`, `development`, and `main`.
- Apply secure configuration practices with environment variables and GitHub Secrets.
- Implement CI/CD and deploy to Azure.

## Planned Tech Stack
- Backend: ASP.NET Web API
- Frontend: React
- Database: PostgreSQL (local via Docker, cloud via Azure Database for PostgreSQL)
- Containerization: Docker, Docker Compose
- CI/CD: GitHub Actions
- Cloud: Azure (Student subscription)

## Branching Strategy
- `main`: production-ready, protected, default branch
- `development`: integration branch for completed features
- `feature/<feature-name>`: implementation branches

Merge flow:
1. `feature/*` -> `development`
2. `development` -> `main`

## Security Principles
- Never commit secrets, API keys, or credentials.
- Use `.env` for local development only.
- Use GitHub Secrets for CI/CD and deployment configuration.
- Keep all sensitive settings environment-based.

## Local Run (Docker)
1. Copy `.env.example` to `.env`.
2. Set `POSTGRES_PASSWORD` and `JWT_SIGNING_KEY` in `.env`.
3. Run `docker compose up --build`.
4. Open frontend: `http://localhost:5173`.
5. Check backend health: `http://localhost:8080/api/health`.

## High-Level Implementation Phases
1. Foundation and repository setup
2. Solution skeleton and Docker baseline
3. Backend core implementation (ASP.NET)
4. Frontend core implementation (React)
5. Security and configuration hardening
6. Testing and quality gates
7. CI/CD with GitHub Actions
8. Azure deployment
9. Kubernetes/Jenkins guidance (chat-only, no setup files)

## Quality Gates (Phase 6)
Minimum checks required before merging to `development` and `main`:
- Backend build: `dotnet build backend/AutoPilot.Api/AutoPilot.Api.csproj --configuration Release`
- Backend tests: `dotnet test backend/AutoPilot.Api.Tests/AutoPilot.Api.Tests.csproj --configuration Release`
- Frontend lint: `npm run lint` (in `frontend`)
- Frontend tests: `npm run test` (in `frontend`)
- Frontend build: `npm run build` (in `frontend`)

These gates are enforced in `.github/workflows/ci.yml`.

## CI/CD (Phase 7)
Branch-aware GitHub Actions setup:
- `feature/*` and PRs: fast validation (`backend` + `frontend` jobs).
- `development`: full CI (`backend` + `frontend` + Docker validation).
- `main`: full CI plus release workflow to publish Docker images to GHCR.

Workflows:
- `.github/workflows/ci.yml`
- `.github/workflows/release-images.yml`

Published images from `main`:
- `ghcr.io/<owner>/autopilot-backend:latest`
- `ghcr.io/<owner>/autopilot-frontend:latest`
- SHA tags are also generated for traceable releases.

## Azure Deployment (Phase 8)
Deployment target: Azure Container Apps (cost-controlled baseline).

Workflow:
- `.github/workflows/deploy-azure.yml`
- Trigger mode: manual (`workflow_dispatch`) so you control credit usage.

### Required GitHub Secrets (Repository Secrets)
- `AZURE_CREDENTIALS` (service principal JSON for `azure/login`)
- `AZURE_RESOURCE_GROUP`
- `AZURE_LOCATION` (for example `southeastasia`)
- `AZURE_CONTAINERAPPS_ENV`
- `AZURE_BACKEND_APP_NAME`
- `AZURE_FRONTEND_APP_NAME`
- `AZURE_POSTGRES_CONNECTION_STRING`
- `AZURE_JWT_SIGNING_KEY`
- `AZURE_JWT_ISSUER`
- `AZURE_JWT_AUDIENCE`
- `AZURE_FRONTEND_ORIGIN` (frontend URL, used by backend CORS)
- `AZURE_BACKEND_API_BASE_URL` (backend base URL used by frontend)
- `GHCR_USERNAME`
- `GHCR_TOKEN` (PAT with `read:packages` to pull private GHCR images)

### Deployment Flow
1. Merge to `main` to publish images (`release-images.yml`).
2. In GitHub Actions, run **Deploy to Azure** workflow manually.
3. Choose image tag (`latest` or a SHA tag).
4. Workflow creates/updates Azure Container Apps and prints backend/frontend URLs.

### Cost-Control Defaults
- Min replicas set to `0` (scale to zero when idle).
- Manual deploy trigger only (no auto deploy on every push).
- Start with small usage and monitor Azure cost dashboard weekly.

## GitHub Settings Needed
- Ensure Actions permission allows package write for this repository.
- Keep `main` protected and require CI checks before merge.
- Keep secrets in GitHub Secrets only (no plaintext in repo).

## Project Status
- Planning complete
- Phase 2 scaffold ready
- Phase 3 backend foundation ready
- Phase 4 monitoring MVP ready
- Phase 5 security hardening ready
- Phase 6 testing and quality gates ready
- Phase 7 CI/CD pipeline ready
- Phase 8 Azure deployment baseline ready

## Notes
- Planning files are kept local and excluded from git tracking.
- Setup instructions for advanced tools can be provided step-by-step in chat.
