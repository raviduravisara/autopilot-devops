from fastapi import APIRouter, HTTPException

from services.prometheus_client import fetch_current_metrics
from services.anomaly_detector import analyze
from services.scheduler import get_latest_result
from models.schemas import IncidentAnalysis

router = APIRouter()


@router.get("/analysis/latest", response_model=IncidentAnalysis)
async def get_latest_analysis():
    """Return the most recent analysis result from the background scheduler."""
    result = get_latest_result()
    if result is None:
        raise HTTPException(status_code=503, detail="Analysis not yet available — wait 30 seconds after startup")
    return result


@router.post("/analysis/run", response_model=IncidentAnalysis)
async def run_analysis_now():
    """Trigger an immediate analysis and return the result."""
    metrics = await fetch_current_metrics()
    return analyze(metrics)
