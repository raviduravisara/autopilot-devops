import { useEffect, useMemo, useState } from "react";
import { API_BASE_URL, getMe, loginUser, registerUser } from "./api/client";
import { clearToken, loadToken, saveToken } from "./api/tokenStore";

const initialRegister = {
  fullName: "",
  email: "",
  password: ""
};

const initialLogin = {
  email: "",
  password: ""
};

function App() {
  const [mode, setMode] = useState("login");
  const [registerForm, setRegisterForm] = useState(initialRegister);
  const [loginForm, setLoginForm] = useState(initialLogin);
  const [token, setToken] = useState(() => loadToken());
  const [me, setMe] = useState(null);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const isAuthenticated = useMemo(() => !!token, [token]);

  useEffect(() => {
    if (!token) {
      setMe(null);
      return;
    }

    let mounted = true;
    getMe(token)
      .then((result) => {
        if (!mounted) return;
        setMe(result);
      })
      .catch((err) => {
        if (!mounted) return;
        clearToken();
        setToken("");
        setError(err.message);
      });

    return () => {
      mounted = false;
    };
  }, [token]);

  function onRegisterChange(event) {
    const { name, value } = event.target;
    setRegisterForm((prev) => ({ ...prev, [name]: value }));
  }

  function onLoginChange(event) {
    const { name, value } = event.target;
    setLoginForm((prev) => ({ ...prev, [name]: value }));
  }

  async function handleRegister(event) {
    event.preventDefault();
    setLoading(true);
    setError("");
    setMessage("");

    try {
      const result = await registerUser(registerForm);
      saveToken(result.accessToken);
      setToken(result.accessToken);
      setMessage("Registration successful. You are logged in.");
      setRegisterForm(initialRegister);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  async function handleLogin(event) {
    event.preventDefault();
    setLoading(true);
    setError("");
    setMessage("");

    try {
      const result = await loginUser(loginForm);
      saveToken(result.accessToken);
      setToken(result.accessToken);
      setMessage("Login successful.");
      setLoginForm(initialLogin);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  function handleLogout() {
    clearToken();
    setToken("");
    setMe(null);
    setMessage("You are logged out.");
    setError("");
  }

  return (
    <main className="app-shell">
      <section className="card">
        <header className="card-header">
          <h1>AutoPilot DevOps</h1>
          <p>Backend API: {API_BASE_URL}</p>
        </header>

        {!isAuthenticated && (
          <div className="tabs">
            <button
              type="button"
              className={mode === "login" ? "tab active" : "tab"}
              onClick={() => setMode("login")}
            >
              Login
            </button>
            <button
              type="button"
              className={mode === "register" ? "tab active" : "tab"}
              onClick={() => setMode("register")}
            >
              Register
            </button>
          </div>
        )}

        {!isAuthenticated && mode === "login" && (
          <form className="form" onSubmit={handleLogin}>
            <label>
              Email
              <input name="email" type="email" value={loginForm.email} onChange={onLoginChange} required />
            </label>
            <label>
              Password
              <input name="password" type="password" value={loginForm.password} onChange={onLoginChange} required />
            </label>
            <button type="submit" disabled={loading}>
              {loading ? "Logging in..." : "Login"}
            </button>
          </form>
        )}

        {!isAuthenticated && mode === "register" && (
          <form className="form" onSubmit={handleRegister}>
            <label>
              Full Name
              <input name="fullName" value={registerForm.fullName} onChange={onRegisterChange} required />
            </label>
            <label>
              Email
              <input name="email" type="email" value={registerForm.email} onChange={onRegisterChange} required />
            </label>
            <label>
              Password
              <input name="password" type="password" value={registerForm.password} onChange={onRegisterChange} minLength={8} required />
            </label>
            <button type="submit" disabled={loading}>
              {loading ? "Creating account..." : "Register"}
            </button>
          </form>
        )}

        {isAuthenticated && (
          <section className="profile">
            <h2>Current User</h2>
            {me ? (
              <ul>
                <li><strong>Name:</strong> {me.fullName}</li>
                <li><strong>Email:</strong> {me.email}</li>
                <li><strong>Created:</strong> {new Date(me.createdAtUtc).toLocaleString()}</li>
              </ul>
            ) : (
              <p>Loading profile...</p>
            )}
            <button type="button" className="danger" onClick={handleLogout}>Logout</button>
          </section>
        )}

        {message && <p className="feedback success">{message}</p>}
        {error && <p className="feedback error">{error}</p>}
      </section>
    </main>
  );
}

export default App;