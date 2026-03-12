import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ApiError } from "../shared/api/apiClient";
import { useAuth } from "../shared/auth/AuthContext";

export function ResendVerificationPage() {
  const navigate = useNavigate();
  const { resendVerification } = useAuth();

  const [email, setEmail] = useState("");
  const [errorMessage, setErrorMessage] = useState("");

  return (
    <div className="auth-page-root d-flex justify-content-center align-items-center min-vh-100 p-4">
      <div className="surface-card w-100 auth-simple-card">
        <h4 className="fw-semibold mb-4">Resend Verification Email</h4>

        {errorMessage && <div className="alert alert-danger py-2">{errorMessage}</div>}

        <form
          onSubmit={async (event) => {
            event.preventDefault();

            try {
              setErrorMessage("");
              await resendVerification(email);
              navigate("/login?reason=resent");
            } catch (error) {
              if (error instanceof ApiError) {
                setErrorMessage(error.message);
                return;
              }

              setErrorMessage("Request failed.");
            }
          }}
        >
          <div className="mb-4">
            <label className="form-label">Email</label>
            <input
              type="email"
              className="form-control"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              required
            />
          </div>

          <button className="btn btn-primary w-100">Send Email</button>
        </form>

        <div className="text-center mt-3">
          <Link to="/login">Back to login</Link>
        </div>
      </div>
    </div>
  );
}