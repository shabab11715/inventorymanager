export type AuthUser = {
  id: string;
  email: string;
  name: string;
  role?: string;
};

export type AuthSession = {
  token: string;
  user: AuthUser;
};

const STORAGE_KEY = "inventorymanager.auth";

export function getStoredSession(): AuthSession | null {
  const raw = localStorage.getItem(STORAGE_KEY);

  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as AuthSession;
  } catch {
    localStorage.removeItem(STORAGE_KEY);
    return null;
  }
}

export function setStoredSession(session: AuthSession): void {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
}

export function clearStoredSession(): void {
  localStorage.removeItem(STORAGE_KEY);
}