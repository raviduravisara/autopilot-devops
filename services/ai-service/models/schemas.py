from pydantic import BaseModel


class MetricSnapshot(BaseModel):
    timestamp: float
    request_rate: float
    error_rate: float
    p95_response_time: float
    memory_mb: float


class IncidentAnalysis(BaseModel):
    anomaly_detected: bool
    severity: str
    confidence: float
    probable_cause: str
    recommended_action: str
    metrics: MetricSnapshot
