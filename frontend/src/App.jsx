import { useEffect, useMemo, useState } from "react";
import {
  API_BASE_URL,
  createMonitor,
  deleteMonitor,
  getMe,
  getMonitorsSummary,
  getRecentChecks,
  listMonitors,
  loginUser,
  registerUser,
  runMonitorCheck,
  updateMonitor
} from "./api/client";
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

const initialMonitorForm = {
  name: "",
  targetUrl: "",
  method: "GET",
  checkIntervalSeconds: 60,
  isActive: true
};

const initialSummary = {
  total: 0,
  up: 0,
  down: 0,
  paused: 0,
  unknown: 0
};

function App() {
  const [mode, setMode] = useState("login");
  const [registerForm, setRegisterForm] = useState(initialRegister);
  const [loginForm, setLoginForm] = useState(initialLogin);
  const [token, setToken] = useState(() => loadToken());
  const [me, setMe] = useState(null);
  const [monitors, setMonitors] = useState([]);
  const [summary, setSummary] = useState(initialSummary);
  const [recentChecks, setRecentChecks] = useState([]);
  const [monitorForm, setMonitorForm] = useState(initialMonitorForm);
  const [editingMonitorId, setEditingMonitorId] = useState("");
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const isAuthenticated = useMemo(() => !!token, [token]);

  async function loadDashboardData(activeToken) {
    const [profile, monitorList, summaryData, checks] = await Promise.all([
      getMe(activeToken),
      listMonitors(activeToken),
      getMonitorsSummary(activeToken),
      getRecentChecks(activeToken, 25)
    ]);

    setMe(profile);
    setMonitors(monitorList);
    setSummary(summaryData);
    setRecentChecks(checks);
  }

  useEffect(() => {
    if (!token) {
      setMe(null);
      setMonitors([]);
      setSummary(initialSummary);
      setRecentChecks([]);
      return;
    }

    let mounted = true;

    loadDashboardData(token)
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

  function onMonitorFormChange(event) {
    const { name, value, type, checked } = event.target;
    setMonitorForm((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : name === "checkIntervalSeconds" ? Number(value) : value
    }));
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

  async function refreshDashboard() {
    if (!token) return;
    await loadDashboardData(token);
  }

  async function handleMonitorSubmit(event) {
    event.preventDefault();
    setLoading(true);
    setError("");
    setMessage("");

    try {
      if (editingMonitorId) {
        await updateMonitor(token, editingMonitorId, monitorForm);
        setMessage("Monitor updated.");
      } else {
        await createMonitor(token, monitorForm);
        setMessage("Monitor created.");
      }

      setMonitorForm(initialMonitorForm);
      setEditingMonitorId("");
      await refreshDashboard();
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  function startEditMonitor(monitor) {
    setEditingMonitorId(monitor.id);
    setMonitorForm({
      name: monitor.name,
      targetUrl: monitor.targetUrl,
      method: monitor.method,
      checkIntervalSeconds: monitor.checkIntervalSeconds,
      isActive: monitor.isActive
    });
    setMessage("");
    setError("");
  }

  function cancelEditMonitor() {
    setEditingMonitorId("");
    setMonitorForm(initialMonitorForm);
  }

  async function handleDeleteMonitor(monitorId) {
    setLoading(true);
    setError("");
    setMessage("");

    try {
      await deleteMonitor(token, monitorId);
      setMessage("Monitor deleted.");
      if (editingMonitorId === monitorId) {
        cancelEditMonitor();
      }
      await refreshDashboard();
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  async function handleRunCheck(monitorId) {
    setLoading(true);
    setError("");
    setMessage("");

    try {
      await runMonitorCheck(token, monitorId);
      setMessage("Check executed.");
      await refreshDashboard();
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
    setMonitors([]);
    setSummary(initialSummary);
    setRecentChecks([]);
    setMonitorForm(initialMonitorForm);
    setEditingMonitorId("");
    setMessage("You are logged out.");
    setError("");
  }

  return (
    <main className="app-shell">
      <section className="card">
        <header className="card-header">
          <h1>AutoPilot DevOps Monitoring</h1>
          <p>Backend API: {API_BASE_URL}</p>
        </header>

        {!isAuthenticated && (
          <div className="tabs">
            <button type="button" className={mode === "login" ? "tab active" : "tab"} onClick={() => setMode("login")}>
              Login
            </button>
            <button type="button" className={mode === "register" ? "tab active" : "tab"} onClick={() => setMode("register")}>
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
            <button type="submit" disabled={loading}>{loading ? "Logging in..." : "Login"}</button>
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
            <button type="submit" disabled={loading}>{loading ? "Creating account..." : "Register"}</button>
          </form>
        )}

        {isAuthenticated && (
          <section className="dashboard">
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

            <section className="summary-cards">
              <article className="summary-card"><h3>Total</h3><p>{summary.total}</p></article>
              <article className="summary-card up"><h3>Up</h3><p>{summary.up}</p></article>
              <article className="summary-card down"><h3>Down</h3><p>{summary.down}</p></article>
              <article className="summary-card paused"><h3>Paused</h3><p>{summary.paused}</p></article>
              <article className="summary-card unknown"><h3>Unknown</h3><p>{summary.unknown}</p></article>
            </section>

            <section className="monitor-editor">
              <h2>{editingMonitorId ? "Edit Monitor" : "Create Monitor"}</h2>
              <form className="form" onSubmit={handleMonitorSubmit}>
                <label>
                  Name
                  <input name="name" value={monitorForm.name} onChange={onMonitorFormChange} required />
                </label>
                <label>
                  Target URL
                  <input name="targetUrl" type="url" value={monitorForm.targetUrl} onChange={onMonitorFormChange} required />
                </label>
                <label>
                  Method
                  <select name="method" value={monitorForm.method} onChange={onMonitorFormChange}>
                    <option value="GET">GET</option>
                    <option value="HEAD">HEAD</option>
                    <option value="POST">POST</option>
                    <option value="PUT">PUT</option>
                    <option value="PATCH">PATCH</option>
                    <option value="DELETE">DELETE</option>
                  </select>
                </label>
                <label>
                  Check Interval (seconds)
                  <input
                    name="checkIntervalSeconds"
                    type="number"
                    min="30"
                    max="3600"
                    value={monitorForm.checkIntervalSeconds}
                    onChange={onMonitorFormChange}
                    required
                  />
                </label>
                <label className="checkbox-label">
                  <input name="isActive" type="checkbox" checked={monitorForm.isActive} onChange={onMonitorFormChange} />
                  Active Monitor
                </label>
                <div className="row-actions">
                  <button type="submit" disabled={loading}>{editingMonitorId ? "Save Monitor" : "Create Monitor"}</button>
                  {editingMonitorId && (
                    <button type="button" className="secondary" onClick={cancelEditMonitor}>Cancel Edit</button>
                  )}
                </div>
              </form>
            </section>

            <section className="monitor-list">
              <h2>Monitors</h2>
              {monitors.length === 0 && <p>No monitors yet.</p>}
              <div className="monitor-grid">
                {monitors.map((monitor) => (
                  <article key={monitor.id} className="monitor-card">
                    <h3>{monitor.name}</h3>
                    <p><strong>URL:</strong> {monitor.targetUrl}</p>
                    <p><strong>Method:</strong> {monitor.method}</p>
                    <p><strong>Interval:</strong> {monitor.checkIntervalSeconds}s</p>
                    <p><strong>Status:</strong> {monitor.isActive ? "Active" : "Paused"}</p>
                    <p>
                      <strong>Latest Check:</strong>{" "}
                      {monitor.lastCheckedAtUtc
                        ? `${monitor.lastCheckSucceeded ? "UP" : "DOWN"} | ${monitor.lastStatusCode ?? "-"} | ${monitor.lastResponseTimeMs ?? "-"}ms`
                        : "Not run yet"}
                    </p>
                    <p><strong>Consecutive Success:</strong> {monitor.consecutiveSuccessCount}</p>
                    <p><strong>Consecutive Fail:</strong> {monitor.consecutiveFailureCount}</p>
                    <div className="row-actions">
                      <button type="button" onClick={() => handleRunCheck(monitor.id)} disabled={loading}>Run Check</button>
                      <button type="button" className="secondary" onClick={() => startEditMonitor(monitor)} disabled={loading}>Edit</button>
                      <button type="button" className="danger" onClick={() => handleDeleteMonitor(monitor.id)} disabled={loading}>Delete</button>
                    </div>
                  </article>
                ))}
              </div>
            </section>

            <section className="recent-checks">
              <h2>Recent Checks</h2>
              {recentChecks.length === 0 && <p>No check history yet.</p>}
              {recentChecks.length > 0 && (
                <div className="table-wrapper">
                  <table>
                    <thead>
                      <tr>
                        <th>Monitor</th>
                        <th>Time</th>
                        <th>Result</th>
                        <th>Status</th>
                        <th>Response</th>
                      </tr>
                    </thead>
                    <tbody>
                      {recentChecks.map((check) => (
                        <tr key={check.id}>
                          <td>{check.monitorName}</td>
                          <td>{new Date(check.executedAtUtc).toLocaleString()}</td>
                          <td>{check.isSuccess ? "UP" : "DOWN"}</td>
                          <td>{check.statusCode ?? "-"}</td>
                          <td>{check.responseTimeMs ? `${check.responseTimeMs}ms` : "-"}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </section>
          </section>
        )}

        {message && <p className="feedback success">{message}</p>}
        {error && <p className="feedback error">{error}</p>}
      </section>
    </main>
  );
}

export default App;