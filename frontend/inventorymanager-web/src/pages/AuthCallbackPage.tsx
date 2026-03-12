import { useEffect } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../shared/auth/AuthContext";

export function AuthCallbackPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const { setSessionFromToken } = useAuth();

  useEffect(() => {
    const searchParams = new URLSearchParams(location.search);
    const token = searchParams.get("token");
    const returnUrl = searchParams.get("returnUrl");

    if (!token) {
      navigate("/login");
      return;
    }

    void setSessionFromToken(token).then(() => {
      navigate(returnUrl || "/");
    });
  }, [location.search, navigate, setSessionFromToken]);

  return (
    <div className="surface-card section-pad text-center">
      Completing login...
    </div>
  );
}