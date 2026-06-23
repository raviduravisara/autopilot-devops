import time
import httpx

PROMETHEUS_URL = "http://prometheus:9090"


async def query(metric: str) -> float:
    url = f"{PROMETHEUS_URL}/api/v1/query"
    async with httpx.AsyncClient(timeout=5.0) as client:
        response = await client.get(url, params={"query": metric})
        response.raise_for_status()
        data = response.json()
        results = data.get("data", {}).get("result", [])
        if not results:
            return 0.0
        return float(results[0]["value"][1])


async def fetch_current_metrics() -> dict:
    now = time.time()

    request_rate = await query(
        'rate(http_requests_received_total{job="autopilot-backend"}[2m])'
    )
    error_rate = await query(
        'rate(http_requests_received_total{job="autopilot-backend",code=~"5.."}[2m])'
    )
    p95 = await query(
        'histogram_quantile(0.95, rate(http_request_duration_seconds_bucket{job="autopilot-backend"}[2m]))'
    )
    memory_bytes = await query(
        'process_working_set_bytes{job="autopilot-backend"}'
    )

    return {
        "timestamp": now,
        "request_rate": round(request_rate, 4),
        "error_rate": round(error_rate, 4),
        "p95_response_time": round(p95, 4),
        "memory_mb": round(memory_bytes / 1024 / 1024, 2),
    }
