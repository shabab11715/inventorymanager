import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ApiError } from "../shared/api/apiClient";
import { useAuth } from "../shared/auth/AuthContext";

export function RegisterPage() {
  const navigate = useNavigate();
  const { register } = useAuth();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  return (
    <div className="auth-page-root">
      <div className="container-fluid p-0">
        <div className="row g-0 min-vh-100">
          <div className="col-12 col-lg-6 d-flex align-items-center justify-content-center p-4">
            <div className="surface-card auth-card w-100">
              <div className="mb-4">
                <div className="text-uppercase text-muted small">Create your account</div>
                <h2 className="fw-bold mb-0">Sign Up</h2>
              </div>

              {errorMessage && <div className="alert alert-danger py-2">{errorMessage}</div>}

              <form
                onSubmit={async (event) => {
                  event.preventDefault();

                  try {
                    setErrorMessage("");
                    await register(email, password);
                    navigate("/login?reason=unverified_sent");
                  } catch (error) {
                    if (error instanceof ApiError) {
                      setErrorMessage(error.message);
                      return;
                    }

                    setErrorMessage("Registration failed.");
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

                <div className="mb-4">
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
                  Create Account
                </button>
              </form>

              <div className="d-flex justify-content-between mt-4">
                <div className="text-muted">
                  Already have an account?{" "}
                  <Link className="link-primary" to="/login">
                    Sign in
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