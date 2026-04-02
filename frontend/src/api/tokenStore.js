const TOKEN_KEY = "autopilot_access_token";

export function loadToken() {
  return localStorage.getItem(TOKEN_KEY) || "";
}

export function saveToken(token) {
  localStorage.setItem(TOKEN_KEY, token);
}

export function clearToken() {
  localStorage.removeItem(TOKEN_KEY);
}