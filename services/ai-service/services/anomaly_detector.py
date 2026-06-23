import numpy as np
from sklearn.ensemble import IsolationForest

from models.schemas import IncidentAnalysis, MetricSnapshot

# Isolation Forest trained on synthetic normal baselines.
# In production this would be trained on real historical data.
_NORMAL_SAMPLES = np.array([
    [0.1, 0.0, 0.05, 150],
    [0.2, 0.0, 0.06, 152],
    [0.15, 0.0, 0.04, 148],
    [0.3, 0.01, 0.07, 155],
    [0.25, 0.0, 0.05, 150],
    [0.1, 0.0, 0.04, 149],
    [0.4, 0.01, 0.08, 160],
    [0.2, 0.0, 0.06, 153],
    [0.35, 0.0, 0.05, 151],
    [0.1, 0.0, 0.04, 147],
    [0.5, 0.02, 0.10, 165],
    [0.3, 0.0, 0.07, 158],
])

_model = IsolationForest(contamination=0.1, random_state=42)
_model.fit(_NORMAL_SAMPLES)


def _classify_severity(score: float, metrics: dict) -> tuple[str, str, str]:
    """Return (severity, probable_cause, recommended_action) based on anomaly score and metric values."""
    if metrics["error_rate"] > 0.1:
        return (
            "critical",
            "High HTTP 5xx error rate detected — backend may be failing",
            "Check backend logs immediately; consider rolling back recent deployment",
        )
    if metrics["p95_response_time"] > 2.0:
        return (
            "high",
            "Response times are severely elevated — possible database or downstream bottleneck",
            "Inspect slow query logs and check database connection pool",
        )
    if metrics["p95_response_time"] > 0.5:
        return (
            "medium",
            "Response times above normal threshold — increased load or mild bottleneck",
            "Monitor trend; scale horizontally if sustained above 1s",
        )
    if metrics["memory_mb"] > 500:
        return (
            "medium",
            "Memory usage is unusually high — possible memory leak",
            "Review recent code changes; consider restarting the service if memory keeps climbing",
        )
    return (
        "low",
        "Metric pattern deviates from baseline — no single obvious cause",
        "Continue monitoring; investigate if anomaly persists for more than 5 minutes",
    )


def analyze(metrics: dict) -> IncidentAnalysis:
    feature_vector = np.array([[
        metrics["request_rate"],
        metrics["error_rate"],
        metrics["p95_response_time"],
        metrics["memory_mb"],
    ]])

    raw_score = _model.decision_function(feature_vector)[0]
    prediction = _model.predict(feature_vector)[0]

    anomaly_detected = prediction == -1

    # Convert raw score to a 0-1 confidence value.
    # decision_function returns negative values for anomalies (more negative = more anomalous).
    confidence = round(min(1.0, max(0.0, 1.0 - (raw_score + 0.5))), 2)

    if anomaly_detected:
        severity, probable_cause, recommended_action = _classify_severity(raw_score, metrics)
    else:
        severity = "none"
        probable_cause = "All metrics are within normal operating range"
        recommended_action = "No action required"

    return IncidentAnalysis(
        anomaly_detected=anomaly_detected,
        severity=severity,
        confidence=confidence,
        probable_cause=probable_cause,
        recommended_action=recommended_action,
        metrics=MetricSnapshot(**metrics),
    )
