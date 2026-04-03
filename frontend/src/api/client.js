const API_BASE_URL = import.meta.env.VITE_API_BASE_URL?.trim() || "http://localhost:8080";

async function request(path, { method = "GET", body, token } = {}) {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {})
    },
    body: body ? JSON.stringify(body) : undefined
  });

  const contentType = response.headers.get("content-type") || "";
  const payload = contentType.includes("application/json") ? await response.json() : null;

  if (!response.ok) {
    const message = payload?.message || `Request failed with status ${response.status}`;
    throw new Error(message);
  }

  return payload;
}

export async function registerUser(data) {
  return request("/api/auth/register", { method: "POST", body: data });
}

export async function loginUser(data) {
  return request("/api/auth/login", { method: "POST", body: data });
}

export async function getMe(token) {
  return request("/api/users/me", { token });
}

export async function listMonitors(token) {
  return request("/api/monitors", { token });
}

export async function getMonitorsSummary(token) {
  return request("/api/monitors/summary", { token });
}

export async function getRecentChecks(token, limit = 20) {
  return request(`/api/monitors/recent-checks?limit=${limit}`, { token });
}

export async function createMonitor(token, data) {
  return request("/api/monitors", { method: "POST", token, body: data });
}

export async function updateMonitor(token, monitorId, data) {
  return request(`/api/monitors/${monitorId}`, { method: "PUT", token, body: data });
}

export async function deleteMonitor(token, monitorId) {
  return request(`/api/monitors/${monitorId}`, { method: "DELETE", token });
}

export async function runMonitorCheck(token, monitorId) {
  return request(`/api/monitors/${monitorId}/run-check`, { method: "POST", token });
}

export { API_BASE_URL };