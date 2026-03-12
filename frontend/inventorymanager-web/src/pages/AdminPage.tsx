import { useEffect, useMemo, useState } from "react";
import { apiRequest, ApiError } from "../shared/api/apiClient";
import { useAuth } from "../shared/auth/AuthContext";
import type { AdminUserResponse } from "../shared/api/types";

export function AdminPage() {
  const { session, isAdmin } = useAuth();
  const [users, setUsers] = useState<AdminUserResponse[]>([]);
  const [selectedUserIds, setSelectedUserIds] = useState<string[]>([]);
  const [errorMessage, setErrorMessage] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isBusy, setIsBusy] = useState(false);

  async function loadUsers() {
    const token = session?.token;
    if (!token) {
      return;
    }

    try {
      setIsLoading(true);
      setErrorMessage("");

      const data = await apiRequest<AdminUserResponse[]>("/api/admin/users", {
        token
      });

      setUsers(data);
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Failed to load users.");
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadUsers();
  }, [session?.token]);

  const selectedUsers = useMemo(
    () => users.filter((user) => selectedUserIds.includes(user.id)),
    [users, selectedUserIds]
  );

  const allSelected = users.length > 0 && selectedUserIds.length === users.length;
  const canRunSingleOnly = selectedUsers.length === 1;

  function toggleSelection(userId: string) {
    setSelectedUserIds((current) =>
      current.includes(userId)
        ? current.filter((id) => id !== userId)
        : [...current, userId]
    );
  }

  function toggleSelectAll() {
    setSelectedUserIds(allSelected ? [] : users.map((user) => user.id));
  }

  async function runBulkAction(action: "block" | "unblock" | "make-admin" | "remove-admin" | "delete") {
    const token = session?.token;
    if (!token || selectedUserIds.length === 0) {
      return;
    }

    try {
      setIsBusy(true);
      setErrorMessage("");

      for (const userId of selectedUserIds) {
        if (action === "delete") {
          await apiRequest(`/api/admin/users/${userId}`, {
            method: "DELETE",
            token
          });
        } else {
          await apiRequest(`/api/admin/users/${userId}/${action}`, {
            method: "POST",
            token
          });
        }
      }

      setSelectedUserIds([]);
      await loadUsers();
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Action failed.");
    } finally {
      setIsBusy(false);
    }
  }

  if (!isAdmin) {
    return <div className="alert alert-warning">Admin access required.</div>;
  }

  return (
    <div className="stack-lg">
      <section className="page-header">
        <div>
          <h1 className="page-title">Admin</h1>
          <p className="page-subtitle">
            Select one or more users, then use the centralized action bar.
          </p>
        </div>
      </section>

      {errorMessage && <div className="alert alert-danger">{errorMessage}</div>}

      <section className="surface-card section-pad">
        <div className="toolbar">
          <button
            className="btn btn-outline-secondary"
            disabled={selectedUserIds.length === 0 || isBusy}
            onClick={() => void runBulkAction("block")}
          >
            Block selected
          </button>

          <button
            className="btn btn-outline-secondary"
            disabled={selectedUserIds.length === 0 || isBusy}
            onClick={() => void runBulkAction("unblock")}
          >
            Unblock selected
          </button>

          <button
            className="btn btn-soft"
            disabled={selectedUserIds.length === 0 || isBusy}
            onClick={() => void runBulkAction("make-admin")}
          >
            Make admin
          </button>

          <button
            className="btn btn-outline-secondary"
            disabled={selectedUserIds.length === 0 || isBusy}
            onClick={() => void runBulkAction("remove-admin")}
          >
            Remove admin
          </button>

          <button
            className="btn btn-outline-danger"
            disabled={selectedUserIds.length === 0 || isBusy}
            onClick={() => void runBulkAction("delete")}
          >
            Delete selected
          </button>

          <span className="helper-text ms-auto">
            Selected: {selectedUserIds.length}
            {canRunSingleOnly ? " user" : " users"}
          </span>
        </div>
      </section>

      <section className="surface-card">
        <div className="table-responsive">
          {isLoading ? (
            <div className="empty-state">Loading...</div>
          ) : (
            <table className="table table-clean table-hover">
              <thead>
                <tr>
                  <th style={{ width: 52 }}>
                    <input
                      type="checkbox"
                      className="form-check-input"
                      checked={allSelected}
                      onChange={toggleSelectAll}
                    />
                  </th>
                  <th>Name</th>
                  <th>Email</th>
                  <th>Role</th>
                  <th>Blocked</th>
                  <th>Created</th>
                </tr>
              </thead>
              <tbody>
                {users.map((user) => {
                  const isSelected = selectedUserIds.includes(user.id);

                  return (
                    <tr
                      key={user.id}
                      className={isSelected ? "table-active" : ""}
                      onClick={() => toggleSelection(user.id)}
                    >
                      <td onClick={(event) => event.stopPropagation()}>
                        <input
                          type="checkbox"
                          className="form-check-input"
                          checked={isSelected}
                          onChange={() => toggleSelection(user.id)}
                        />
                      </td>
                      <td className="fw-semibold">{user.name}</td>
                      <td>{user.email}</td>
                      <td>
                        <span className={`soft-badge ${user.role === "admin" ? "soft-badge-primary" : "soft-badge-muted"}`}>
                          {user.role}
                        </span>
                      </td>
                      <td>
                        <span className={`soft-badge ${user.isBlocked ? "soft-badge-danger" : "soft-badge-success"}`}>
                          {user.isBlocked ? "blocked" : "active"}
                        </span>
                      </td>
                      <td>{new Date(user.createdAt).toLocaleString()}</td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          )}
        </div>
      </section>
    </div>
  );
}