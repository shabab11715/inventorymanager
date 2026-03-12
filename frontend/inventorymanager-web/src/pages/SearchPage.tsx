import { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { apiRequest, ApiError } from "../shared/api/apiClient";
import { useAuth } from "../shared/auth/AuthContext";
import type { SearchResultResponse } from "../shared/api/types";

export function SearchPage() {
  const navigate = useNavigate();
  const { session } = useAuth();
  const [searchParams] = useSearchParams();
  const q = searchParams.get("q") ?? "";

  const [result, setResult] = useState<SearchResultResponse>({ inventories: [], items: [] });
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    let mounted = true;

    async function load() {
      try {
        setIsLoading(true);
        setErrorMessage("");

        const data = await apiRequest<SearchResultResponse>(
          `/api/search?q=${encodeURIComponent(q)}&pageNumber=1&pageSize=20`,
          { token: session?.token ?? null }
        );

        if (mounted) setResult(data);
      } catch (error) {
        if (!mounted) return;
        setErrorMessage(error instanceof ApiError ? error.message : "Search failed.");
      } finally {
        if (mounted) setIsLoading(false);
      }
    }

    load();

    return () => {
      mounted = false;
    };
  }, [q, session?.token]);

  return (
    <div className="stack-lg">
      <section className="page-header">
        <div>
          <h1 className="page-title">Search</h1>
          <p className="page-subtitle">Results for: <span className="mono">{q || "-"}</span></p>
        </div>
      </section>

      {errorMessage && <div className="alert alert-danger">{errorMessage}</div>}

      {isLoading ? (
        <div className="surface-card empty-state">Loading...</div>
      ) : (
        <>
          <section className="surface-card">
            <div className="section-pad border-bottom">
              <h2 className="h5 mb-0">Inventories</h2>
            </div>
            <div className="table-responsive">
              {result.inventories.length === 0 ? (
                <div className="empty-state">No inventories found.</div>
              ) : (
                <table className="table table-clean table-hover">
                  <thead>
                    <tr>
                      <th>Title</th>
                      <th>Description</th>
                    </tr>
                  </thead>
                  <tbody>
                    {result.inventories.map((inventory) => (
                      <tr
                        key={inventory.id}
                        className="table-row-link cursor-pointer"
                        onClick={() => navigate(`/inventories/${inventory.id}`)}
                      >
                        <td className="fw-semibold">{inventory.title}</td>
                        <td>{inventory.description || "-"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          </section>

          <section className="surface-card">
            <div className="section-pad border-bottom">
              <h2 className="h5 mb-0">Items</h2>
            </div>
            <div className="table-responsive">
              {result.items.length === 0 ? (
                <div className="empty-state">No items found.</div>
              ) : (
                <table className="table table-clean table-hover">
                  <thead>
                    <tr>
                      <th>Custom ID</th>
                      <th>Name</th>
                    </tr>
                  </thead>
                  <tbody>
                    {result.items.map((item) => (
                      <tr
                        key={item.id}
                        className="table-row-link cursor-pointer"
                        onClick={() => navigate(`/inventories/${item.inventoryId}?tab=items&itemId=${item.id}`)}
                      >
                        <td className="fw-semibold mono">{item.customId}</td>
                        <td>{item.name}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          </section>
        </>
      )}
    </div>
  );
}