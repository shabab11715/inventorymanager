import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import type { ReactNode } from "react";
import { apiRequest } from "../api/apiClient";

type SessionUser = {
  id: string;
  email: string;
  name: string;
  role: string;
  isBlocked: boolean;
  isEmailVerified: boolean;
};

type SessionState = {
  token: string;
  user: SessionUser;
};

type AuthContextValue = {
  session: SessionState | null;
  isAuthenticated: boolean;
  isAdmin: boolean;
  setSessionFromToken: (token: string) => Promise<void>;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string) => Promise<void>;
  resendVerification: (email: string) => Promise<void>;
  devLogin: (email: string, name: string, isAdmin: boolean) => Promise<void>;
  logout: () => void;
};

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

const STORAGE_KEY = "inventorymanager.session";

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<SessionState | null>(() => {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as SessionState;
    } catch {
      return null;
    }
  });

  useEffect(() => {
    if (!session) {
      window.localStorage.removeItem(STORAGE_KEY);
      return;
    }

    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
  }, [session]);

  const setSessionFromToken = useCallback(async (token: string) => {
    const me = await apiRequest<SessionUser>("/api/auth/me", { token });
    setSession({
      token,
      user: me
    });
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const response = await apiRequest<{ token: string; user: SessionUser }>("/api/auth/login", {
      method: "POST",
      body: {
        email,
        password
      }
    });

    setSession({
      token: response.token,
      user: response.user
    });
  }, []);

  const register = useCallback(async (email: string, password: string) => {
    await apiRequest<{ message: string }>("/api/auth/register", {
      method: "POST",
      body: {
        email,
        password
      }
    });
  }, []);

  const resendVerification = useCallback(async (email: string) => {
    await apiRequest<{ message: string }>("/api/auth/resend-verification", {
      method: "POST",
      body: {
        email
      }
    });
  }, []);

  const devLogin = useCallback(async (email: string, name: string, isAdmin: boolean) => {
    const response = await apiRequest<{ token: string; user: SessionUser }>("/api/auth/dev-login", {
      method: "POST",
      body: {
        email,
        name,
        isAdmin
      }
    });

    setSession({
      token: response.token,
      user: response.user
    });
  }, []);

  const logout = useCallback(() => {
    setSession(null);
  }, []);

  const value = useMemo<AuthContextValue>(() => {
    return {
      session,
      isAuthenticated: !!session,
      isAdmin: session?.user.role === "admin",
      setSessionFromToken,
      login,
      register,
      resendVerification,
      devLogin,
      logout
    };
  }, [session, setSessionFromToken, login, register, resendVerification, devLogin, logout]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error("useAuth must be used inside AuthProvider.");
  }

  return context;
}