# AutoPilot DevOps

An **AI-powered AIOps platform** that combines a full-stack application with a complete DevOps toolchain: containerization, Kubernetes orchestration, GitOps continuous delivery, observability, ML-based incident analysis, security scanning, and Infrastructure as Code.

Built as a portfolio-grade, local-first project that runs entirely on a laptop and deploys to Azure in a cost-controlled way.

---

## What it does

Three application services work together, monitored by an observability stack and analyzed by a machine-learning service:

- **Backend (ASP.NET Core 8)** — JWT auth, uptime-monitor CRUD, a background scheduler that polls monitors, and a Prometheus `/metrics` endpoint.
- **Frontend (React + Vite)** — dashboard for monitors and an **AI Incident Analysis** panel that shows live health, severity, root-cause guess, and recommended action.
- **AI Service (Python FastAPI + scikit-learn)** — an Isolation Forest model that pulls metrics from Prometheus every 30s, detects anomalies, and returns a diagnosis.

The platform emits metrics → Prometheus scrapes them → the AI service analyzes them → the frontend surfaces incidents → operators fix issues through Git (GitOps).

---

## Architecture

```
                        ┌──────────────┐
       browser  ─────▶  │   Frontend   │  (React, NodePort 30173)
                        └──────┬───────┘
                               │ REST
                        ┌──────▼───────┐        ┌──────────────┐
                        │   Backend    │ ─────▶ │  PostgreSQL  │
                        │ (ASP.NET 8)  │        └──────────────┘
                        └──────┬───────┘
                    /metrics   │
                        ┌──────▼───────┐        ┌──────────────┐
                        │  Prometheus  │ ─────▶ │   Grafana    │
                        └──────┬───────┘        └──────────────┘
                     scrape    │
                        ┌──────▼───────┐
                        │  AI Service  │  (FastAPI + Isolation Forest)
                        │  anomaly ML  │
                        └──────────────┘

  Delivery:  Git  ──▶  ArgoCD  ──▶  Kubernetes (kind)      [GitOps]
  Secrets:   Sealed Secrets (encrypted in Git)
  CI:        GitHub Actions  +  Jenkins
  Security:  Trivy (image + dependency scanning)
  IaC:       Terraform  ──▶  Azure
```

---

## Tech stack

| Area | Technology |
|------|-----------|
| Backend | ASP.NET Core 8, EF Core, PostgreSQL, JWT |
| Frontend | React 18, Vite |
| AI/ML | Python, FastAPI, scikit-learn (Isolation Forest) |
| Containers | Docker, Docker Compose |
| Orchestration | Kubernetes (kind), Helm |
| GitOps CD | ArgoCD |
| Secrets | Sealed Secrets |
| Observability | Prometheus, Grafana |
| CI | GitHub Actions, Jenkins |
| Security | Trivy |
| IaC | Terraform (Azure) |
| Cloud | Azure Container Apps, Azure Database for PostgreSQL |

---

## Repository layout

```
backend/                ASP.NET Core API + tests
frontend/               React app
services/ai-service/    Python FastAPI anomaly-detection service
observability/          Prometheus + Grafana provisioning (compose)
k8s/                    Kubernetes manifests (kind cluster + base/)
gitops/argocd/          ArgoCD Application (GitOps)
helm/autopilot/         Helm chart (alternative deployment)
infra/terraform/azure/  Terraform IaC for Azure
.github/workflows/      GitHub Actions (CI, release, deploy, security scan)
Jenkinsfile             Jenkins pipeline mirroring CI
docker-compose.yml      Full local stack
```

---

## Run it locally

### Option A — Docker Compose (simplest)

1. Copy `.env.example` to `.env` and set `POSTGRES_PASSWORD`, `JWT_SIGNING_KEY`, `GRAFANA_ADMIN_PASSWORD`.
2. `docker compose up --build`
3. Open:
   - Frontend: `http://localhost:5173`
   - Backend health: `http://localhost:8080/api/health`
   - AI analysis: `http://localhost:8000/api/analysis/latest`
   - Prometheus: `http://localhost:9090`
   - Grafana: `http://localhost:3000`

### Option B — Kubernetes (kind) with GitOps

1. Create the cluster: `kind create cluster --config k8s/kind-cluster.yaml`
2. Build and load images:
   ```
   docker build -f backend/AutoPilot.Api/Dockerfile -t autopilot-backend:local .
   docker build -f frontend/Dockerfile -t autopilot-frontend:local .
   docker build -f services/ai-service/Dockerfile -t autopilot-ai-service:local services/ai-service
   kind load docker-image autopilot-backend:local autopilot-frontend:local autopilot-ai-service:local --name autopilot
   ```
3. Create the namespace and the secret (from local values, never committed):
   ```
   kubectl apply -f k8s/base/00-namespace.yaml
   kubectl apply -f k8s/base/01-sealedsecret.yaml
   ```
4. Install ArgoCD and apply the Application — ArgoCD then syncs `k8s/base` from Git automatically:
   ```
   kubectl apply -f gitops/argocd/autopilot-app.yaml
   ```
5. Access via NodePorts: frontend `30173`, backend `30080`, ai-service `30800`.

### Option C — Helm

```
helm template autopilot helm/autopilot        # render/preview
helm install autopilot helm/autopilot         # deploy
```

---

## DevOps highlights

- **GitOps with ArgoCD** — the cluster state is reconciled from Git automatically (auto-sync, prune, self-heal). Deployments happen by merging to `main`, not by manual `kubectl apply`. Rollback is a `git revert`.
- **Sealed Secrets** — secrets are encrypted with the cluster's public key and committed safely to Git; only the in-cluster controller can decrypt them. No plaintext credentials in the repo.
- **Dual CI** — the same quality gates run in both GitHub Actions and a Jenkins pipeline (`Jenkinsfile`), using Docker agents for reproducible builds.
- **Security scanning** — Trivy scans dependencies and container images for CRITICAL/HIGH vulnerabilities in CI.
- **AI incident analysis** — an Isolation Forest model turns raw metrics into actionable incident diagnoses surfaced on the dashboard.
- **Infrastructure as Code** — Azure resources (resource group, PostgreSQL Flexible Server, Container Apps environment) are defined in Terraform with cost-controlled SKUs.

---

## Security & configuration

- No secrets in Git. Local dev uses `.env`; CI/CD uses GitHub Secrets; Kubernetes uses Sealed Secrets.
- All sensitive settings are environment-based.
- `main` is the source of truth; feature branches merge in via PRs with CI checks.

---

## Quality gates

Enforced in `.github/workflows/ci.yml` and mirrored in `Jenkinsfile`:

- Backend: `dotnet build` + `dotnet test`
- Frontend: `npm run lint` + `npm run test` + `npm run build`
- Docker image builds validated on `main`
- Trivy security scan (`.github/workflows/security-scan.yml`)

---

## Azure deployment (cost-controlled)

- Terraform provisions the infrastructure (`infra/terraform/azure/`).
- `.github/workflows/deploy-azure.yml` deploys to Azure Container Apps via manual trigger (`workflow_dispatch`) to control credit usage.
- Scale-to-zero and lowest-cost SKUs keep idle cost near zero; tear down with `terraform destroy` after demos.

See the workflow file for the required `AZURE_*` and `GHCR_*` GitHub Secrets.
