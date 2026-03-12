import { useMemo, useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { ApiError } from "../shared/api/apiClient";
import { useAuth } from "../shared/auth/AuthContext";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5201";

export function LoginPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const { login } = useAuth();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  const reason = useMemo(() => {
    const params = new URLSearchParams(location.search);
    return params.get("reason") ?? "";
  }, [location.search]);

  const infoMessage = useMemo(() => {
    const map: Record<string, string> = {
      blocked: "Your account is blocked.",
      unverified_sent: "Verification email sent. You can still sign in, and verify later.",
      resent: "Verification email resent. You can still sign in, and verify later.",
      verify_invalid: "Verification link is invalid.",
      verify_expired: "Verification link expired. Please request a new one.",
      verified: "Email verified successfully.",
      external_auth_failed: "External login failed.",
      external_auth_missing_profile: "External login did not return a usable profile."
    };

    return map[reason] ?? "";
  }, [reason]);

  const showResendLink =
    reason === "unverified" ||
    reason === "unverified_sent" ||
    reason === "resent" ||
    reason === "verify_invalid" ||
    reason === "verify_expired";

  return (
    <div className="auth-page-root">
      <div className="container-fluid p-0">
        <div className="row g-0 min-vh-100">
          <div className="col-12 col-lg-6 d-flex align-items-center justify-content-center p-4">
            <div className="surface-card auth-card w-100">
              <div className="mb-4">
                <h2 className="fw-bold mb-0">Sign In to InventoryManager</h2>
              </div>

              {infoMessage && <div className="alert alert-warning py-2">{infoMessage}</div>}

              {showResendLink && (
                <div className="mb-3">
                  <Link className="link-primary" to="/resend-verification">
                    Resend verification email
                  </Link>
                </div>
              )}

              {errorMessage && <div className="alert alert-danger py-2">{errorMessage}</div>}

              <form
                onSubmit={async (event) => {
                  event.preventDefault();

                  try {
                    setErrorMessage("");
                    await login(email, password);
                    navigate("/");
                  } catch (error) {
                    if (error instanceof ApiError) {
                      setErrorMessage(error.message);
                      return;
                    }

                    setErrorMessage("Login failed.");
                  }
                }}
              >
                <div className="mb-3">
                  <label className="form-label">E-mail</label>
                  <input
                    type="email"
                    className="form-control"
                    placeholder="test@example.com"
                    value={email}
                    onChange={(event) => setEmail(event.target.value)}
                    required
                  />
                </div>

                <div className="mb-3">
                  <label className="form-label">Password</label>
                  <div className="input-group">
                    <input
                      type={showPassword ? "text" : "password"}
                      className="form-control"
                      placeholder="••••••••"
                      value={password}
                      onChange={(event) => setPassword(event.target.value)}
                      required
                    />
                    <button
                      type="button"
                      className="btn btn-outline-secondary"
                      onClick={() => setShowPassword((value) => !value)}
                    >
                      {showPassword ? "Hide" : "Show"}
                    </button>
                  </div>
                </div>

                <button className="btn btn-primary w-100 py-2" type="submit">
                  Sign In
                </button>
              </form>

              <div className="auth-divider">
                <span>or continue with</span>
              </div>

              <div className="d-grid gap-2">
                <a
                  href={`${API_BASE_URL}/api/auth/google/start`}
                  className="btn btn-outline-danger w-100" // Updated to match Task4 button alignment
                >
                  Login with Google
                </a>

                <a
                  href={`${API_BASE_URL}/api/auth/github/start`}
                  className="btn btn-outline-dark w-100" // Updated to match Task4 button alignment
                >
                  Login with GitHub
                </a>
              </div>

              <div className="d-flex justify-content-between mt-4">
                <div className="text-muted">
                  Don&apos;t have an account?{" "}
                  <Link className="link-primary" to="/register">
                    Sign up
                  </Link>
                </div>
              </div>
            </div>
          </div>

          <div className="col-12 col-lg-6 d-none d-lg-block auth-wallpaper"></div>
        </div>
      </div>
    </div>
  );
}