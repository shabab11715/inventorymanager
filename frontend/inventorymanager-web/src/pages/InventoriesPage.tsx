import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { apiRequest, ApiError } from "../shared/api/apiClient";
import { useAuth } from "../shared/auth/AuthContext";
import type { InventoryResponse } from "../shared/api/types";

export function InventoriesPage() {
  const navigate = useNavigate();
  const { session, isAuthenticated } = useAuth();
  const [inventories, setInventories] = useState<InventoryResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    let mounted = true;

    async function load() {
      try {
        setIsLoading(true);
        setErrorMessage("");

        const data = await apiRequest<InventoryResponse[]>("/api/inventories?pageNumber=1&pageSize=30", {
          token: session?.token ?? null
        });

        if (mounted) setInventories(data);
      } catch (error) {
        if (!mounted) return;
        setErrorMessage(error instanceof ApiError ? error.message : "Failed to load inventories.");
      } finally {
        if (mounted) setIsLoading(false);
      }
    }

    load();

    return () => {
      mounted = false;
    };
  }, [session?.token]);

  return (
    <div className="stack-lg">
      <section className="page-header">
        <div>
          <h1 className="page-title">Inventories</h1>
          <p className="page-subtitle">Open an inventory to manage items, fields, access, and settings.</p>
        </div>

        {isAuthenticated && (
          <button className="btn btn-primary" onClick={() => navigate("/inventories/new")}>
            Create inventory
          </button>
        )}
      </section>

      {errorMessage && <div className="alert alert-danger">{errorMessage}</div>}

      <section className="surface-card">
        <div className="table-responsive">
          {isLoading ? (
            <div className="empty-state">Loading...</div>
          ) : inventories.length === 0 ? (
            <div className="empty-state">No inventories found.</div>
          ) : (
            <table className="table table-clean table-hover">
              <thead>
                <tr>
                  <th>Title</th>
                  <th>Category</th>
                  <th>Description</th>
                  <th>Tags</th>
                </tr>
              </thead>
              <tbody>
                {inventories.map((inventory) => (
                  <tr
                    key={inventory.id}
                    className="table-row-link cursor-pointer"
                    onClick={() => navigate(`/inventories/${inventory.id}`)}
                  >
                    <td className="fw-semibold">{inventory.title}</td>
                    <td>{inventory.categoryName ?? "-"}</td>
                    <td>{inventory.description || "-"}</td>
                    <td>
                      <div className="inline-list">
                        {inventory.tags.length > 0 ? (
                          inventory.tags.map((tag) => <span key={tag} className="tag-pill">{tag}</span>)
                        ) : (
                          <span className="text-muted">-</span>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </section>
    </div>
  );
}