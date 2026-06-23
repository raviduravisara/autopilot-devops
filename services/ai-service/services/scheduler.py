import asyncio
import logging

from prometheus_client import Counter, Gauge

from services.prometheus_client import fetch_current_metrics
from services.anomaly_detector import analyze

logger = logging.getLogger(__name__)

ANOMALY_COUNTER = Counter(
    "ai_anomalies_detected_total",
    "Total number of anomalies detected by the AI service",
    ["severity"],
)

LAST_CONFIDENCE = Gauge(
    "ai_last_confidence_score",
    "Confidence score from the most recent analysis",
)

_task: asyncio.Task | None = None
_latest_result = None


def get_latest_result():
    return _latest_result


async def _run_loop():
    global _latest_result
    while True:
        try:
            metrics = await fetch_current_metrics()
            result = analyze(metrics)
            _latest_result = result
            LAST_CONFIDENCE.set(result.confidence)
            if result.anomaly_detected:
                ANOMALY_COUNTER.labels(severity=result.severity).inc()
                logger.warning(
                    "Anomaly detected | severity=%s confidence=%.2f cause=%s",
                    result.severity,
                    result.confidence,
                    result.probable_cause,
                )
        except Exception as exc:
            logger.error("Analysis cycle failed: %s", exc)
        await asyncio.sleep(30)


async def start_scheduler():
    global _task
    _task = asyncio.create_task(_run_loop())
    logger.info("AI analysis scheduler started (30s interval)")


async def stop_scheduler():
    global _task
    if _task:
        _task.cancel()
        logger.info("AI analysis scheduler stopped")
