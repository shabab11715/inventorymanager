import { Link, Route, Routes } from "react-router-dom";
import { HomePage } from "./pages/HomePage";
import { LoginPage } from "./pages/LoginPage";
import { RegisterPage } from "./pages/RegisterPage";
import { ResendVerificationPage } from "./pages/ResendVerificationPage";
import { AuthCallbackPage } from "./pages/AuthCallbackPage";
import { AdminPage } from "./pages/AdminPage";
import { InventoriesPage } from "./pages/InventoriesPage";
import { CreateInventoryPage } from "./pages/CreateInventoryPage";
import { InventoryWorkspacePage } from "./pages/InventoryWorkspacePage";
import { SearchPage } from "./pages/SearchPage";
import { UserPage } from "./pages/UserPage";
import { useAuth } from "./shared/auth/AuthContext";
import { useUiPreferences } from "./shared/ui/UiPreferencesContext";

export default function App() {
  const { session, isAdmin, logout } = useAuth();
  const { language, setLanguage, theme, setTheme, t } = useUiPreferences();

  return (
    <div className="app-shell">
      <header className="border-bottom app-topbar">
        <div className="container py-3 d-flex justify-content-between align-items-center flex-wrap gap-3">
          <div className="d-flex align-items-center gap-3 flex-wrap">
            <Link to="/" className="text-decoration-none fw-bold text-white">
              InventoryManager
            </Link>

            <Link to="/inventories" className="text-decoration-none text-white">
              {t("browseInventories")}
            </Link>

            {session && (
              <Link to="/me" className="text-decoration-none text-white">
                {t("myPage")}
              </Link>
            )}

            {isAdmin && (
              <Link to="/admin" className="text-decoration-none text-white">
                Admin
              </Link>
            )}
          </div>

          <div className="d-flex align-items-center gap-2 flex-wrap">
            <select
              className="form-select topbar-select"
              value={language}
              onChange={(event) => setLanguage(event.target.value as "en" | "bn")}
            >
              <option value="en">English</option>
              <option value="bn">বাংলা</option>
            </select>

            <select
              className="form-select topbar-select"
              value={theme}
              onChange={(event) => setTheme(event.target.value as "light" | "dark")}
            >
              <option value="light">{t("lightTheme")}</option>
              <option value="dark">{t("darkTheme")}</option>
            </select>

            {session ? (
              <button type="button" className="btn btn-outline-light" onClick={logout}>
                {t("logout")}
              </button>
            ) : (
              <Link to="/login" className="btn btn-light">
                {t("login")}
              </Link>
            )}
          </div>
        </div>
      </header>

      <main className="container py-4">
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/resend-verification" element={<ResendVerificationPage />} />
          <Route path="/auth/callback" element={<AuthCallbackPage />} />
          <Route path="/admin" element={<AdminPage />} />
          <Route path="/inventories" element={<InventoriesPage />} />
          <Route path="/inventories/new" element={<CreateInventoryPage />} />
          <Route path="/inventories/:inventoryId" element={<InventoryWorkspacePage />} />
          <Route path="/search" element={<SearchPage />} />
          <Route path="/me" element={<UserPage />} />
          <Route path="/users/:id" element={<UserPage />} />
        </Routes>
      </main>
    </div>
  );
}