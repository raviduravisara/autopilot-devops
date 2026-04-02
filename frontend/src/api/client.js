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

export { API_BASE_URL };