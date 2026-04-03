import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import App from "./App";

const {
  mockLoadToken,
  mockSaveToken,
  mockClearToken,
  mockRegisterUser,
  mockLoginUser,
  mockGetMe,
  mockListMonitors,
  mockGetMonitorsSummary,
  mockGetRecentChecks,
  mockCreateMonitor,
  mockDeleteMonitor,
  mockRunMonitorCheck,
  mockUpdateMonitor
} = vi.hoisted(() => ({
  mockLoadToken: vi.fn(),
  mockSaveToken: vi.fn(),
  mockClearToken: vi.fn(),
  mockRegisterUser: vi.fn(),
  mockLoginUser: vi.fn(),
  mockGetMe: vi.fn(),
  mockListMonitors: vi.fn(),
  mockGetMonitorsSummary: vi.fn(),
  mockGetRecentChecks: vi.fn(),
  mockCreateMonitor: vi.fn(),
  mockDeleteMonitor: vi.fn(),
  mockRunMonitorCheck: vi.fn(),
  mockUpdateMonitor: vi.fn()
}));

vi.mock("./api/tokenStore", () => ({
  loadToken: mockLoadToken,
  saveToken: mockSaveToken,
  clearToken: mockClearToken
}));

vi.mock("./api/client", () => ({
  API_BASE_URL: "http://localhost:8080",
  registerUser: mockRegisterUser,
  loginUser: mockLoginUser,
  getMe: mockGetMe,
  listMonitors: mockListMonitors,
  getMonitorsSummary: mockGetMonitorsSummary,
  getRecentChecks: mockGetRecentChecks,
  createMonitor: mockCreateMonitor,
  deleteMonitor: mockDeleteMonitor,
  runMonitorCheck: mockRunMonitorCheck,
  updateMonitor: mockUpdateMonitor
}));

describe("App", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockLoadToken.mockReturnValue("");
    mockGetMe.mockResolvedValue({
      fullName: "Ravidu",
      email: "ravidu@gmail.com",
      createdAtUtc: "2026-04-03T10:00:00Z"
    });
    mockListMonitors.mockResolvedValue([]);
    mockGetMonitorsSummary.mockResolvedValue({ total: 0, up: 0, down: 0, paused: 0, unknown: 0 });
    mockGetRecentChecks.mockResolvedValue([]);
  });

  it("shows login form by default", () => {
    render(<App />);

    expect(screen.getByText("AutoPilot DevOps Monitoring")).toBeInTheDocument();
    expect(screen.getByLabelText("Email")).toBeInTheDocument();
    expect(screen.getByLabelText("Password")).toBeInTheDocument();
  });

  it("switches to register form", () => {
    render(<App />);

    fireEvent.click(screen.getAllByRole("button", { name: "Register" })[0]);

    expect(screen.getByLabelText("Full Name")).toBeInTheDocument();
  });

  it("logs in and renders dashboard", async () => {
    mockLoginUser.mockResolvedValue({ accessToken: "token-123" });

    render(<App />);

    const emailInput = screen.getByLabelText("Email");
    const passwordInput = screen.getByLabelText("Password");

    fireEvent.change(emailInput, { target: { value: "ravidu@gmail.com" } });
    fireEvent.change(passwordInput, { target: { value: "123456789" } });
    fireEvent.submit(passwordInput.closest("form"));

    await waitFor(() => {
      expect(mockLoginUser).toHaveBeenCalledWith({ email: "ravidu@gmail.com", password: "123456789" });
    });

    await waitFor(() => {
      expect(screen.getByText("Current User")).toBeInTheDocument();
      expect(screen.getByText("Login successful.")).toBeInTheDocument();
    });

    expect(mockSaveToken).toHaveBeenCalledWith("token-123");
    expect(mockGetMe).toHaveBeenCalledWith("token-123");
  });

  it("shows API error message on login failure", async () => {
    mockLoginUser.mockRejectedValue(new Error("Invalid credentials."));

    render(<App />);

    const emailInput = screen.getByLabelText("Email");
    const passwordInput = screen.getByLabelText("Password");

    fireEvent.change(emailInput, { target: { value: "ravidu@gmail.com" } });
    fireEvent.change(passwordInput, { target: { value: "wrong-password" } });
    fireEvent.submit(passwordInput.closest("form"));

    await waitFor(() => {
      expect(screen.getByText("Invalid credentials.")).toBeInTheDocument();
    });
  });
});
