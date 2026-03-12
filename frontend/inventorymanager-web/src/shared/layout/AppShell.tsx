import { useMemo, useState, type FormEvent, type ReactNode } from "react";
import { Link, NavLink, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

export function AppShell({ children }: { children: ReactNode }) {
  const { session, isAuthenticated, isAdmin, logout } = useAuth();
  const navigate = useNavigate();
  const [searchText, setSearchText] = useState("");

  function handleLogout() {
    logout();
    navigate("/");
  }

  function handleSearchSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const q = searchText.trim();
    if (!q) return;
    navigate(`/search?q=${encodeURIComponent(q)}`);
  }

  const userLabel = useMemo(() => {
    if (!session) return "";
    return `${session.user.name} (${session.user.email})`;
  }, [session]);

  return (
    <div className="app-shell d-flex flex-column">
      <nav className="navbar navbar-expand-lg navbar-dark app-topbar">
        <div className="container py-1">
          <Link className="navbar-brand app-brand" to="/">
            InventoryManager
          </Link>

          <button
            className="navbar-toggler"
            type="button"
            data-bs-toggle="collapse"
            data-bs-target="#mainNavbar"
            aria-controls="mainNavbar"
            aria-expanded="false"
            aria-label="Toggle navigation"
          >
            <span className="navbar-toggler-icon" />
          </button>

          <div className="collapse navbar-collapse gap-3" id="mainNavbar">
            <div className="navbar-nav">
              <NavLink className="nav-link" to="/">
                Home
              </NavLink>
              <NavLink className="nav-link" to="/inventories">
                Inventories
              </NavLink>
              {isAuthenticated && (
                <NavLink className="nav-link" to="/inventories/new">
                  New Inventory
                </NavLink>
              )}
              {isAdmin && (
                <NavLink className="nav-link" to="/admin">
                  Admin
                </NavLink>
              )}
            </div>

            <form className="ms-lg-3 flex-grow-1" onSubmit={handleSearchSubmit}>
              <input
                className="form-control form-control-sm"
                type="search"
                placeholder="Search inventories or items"
                value={searchText}
                onChange={(event) => setSearchText(event.target.value)}
              />
            </form>

            <div className="d-flex align-items-center gap-2 flex-wrap">
              {isAuthenticated && session ? (
                <>
                  <span className="text-light small">{userLabel}</span>
                  {isAdmin && <span className="soft-badge soft-badge-primary">admin</span>}
                  <button type="button" className="btn btn-outline-light btn-sm" onClick={handleLogout}>
                    Logout
                  </button>
                </>
              ) : (
                <NavLink className="btn btn-outline-light btn-sm" to="/login">
                  Dev Login
                </NavLink>
              )}
            </div>
          </div>
        </div>
      </nav>

      <main className="flex-grow-1 py-4">
        <div className="container">{children}</div>
      </main>
    </div>
  );
}