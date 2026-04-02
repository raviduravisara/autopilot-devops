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

## Project Status
- Planning complete
- Phase 2 scaffold ready
- Phase 3 backend foundation (migrations + JWT auth + role baseline) ready

## Notes
- Planning files are kept local and excluded from git tracking.
- Setup instructions for advanced tools can be provided step-by-step in chat.