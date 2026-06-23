from contextlib import asynccontextmanager
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from prometheus_client import make_asgi_app

from routers import analysis
from services.scheduler import start_scheduler, stop_scheduler


@asynccontextmanager
async def lifespan(app: FastAPI):
    await start_scheduler()
    yield
    await stop_scheduler()


app = FastAPI(title="AutoPilot AI Service", version="1.0.0", lifespan=lifespan)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(analysis.router, prefix="/api")

metrics_app = make_asgi_app()
app.mount("/metrics", metrics_app)


@app.get("/")
def root():
    return {"service": "AutoPilot AI Service", "status": "running"}


@app.get("/health")
def health():
    return {"status": "healthy"}
