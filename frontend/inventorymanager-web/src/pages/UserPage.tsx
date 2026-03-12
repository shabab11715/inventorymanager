import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { apiRequest, ApiError } from "../shared/api/apiClient";
import { useAuth } from "../shared/auth/AuthContext";

type UserInventoryCardResponse = {
  id: string;
  title: string;
  description: string;
  imageUrl: string;
  categoryName: string;
  itemCount: number;
};

type UserProfileResponse = {
  id: string;
  name: string;
  email: string;
  ownedInventories: UserInventoryCardResponse[];
  writableInventories: UserInventoryCardResponse[];
};

type SortMode = "title" | "items";

export function UserPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { session } = useAuth();
  const [data, setData] = useState<UserProfileResponse | null>(null);
  const [errorMessage, setErrorMessage] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [ownedSortMode, setOwnedSortMode] = useState<SortMode>("title");
  const [writableSortMode, setWritableSortMode] = useState<SortMode>("title");
  const [ownedFilter, setOwnedFilter] = useState("");
  const [writableFilter, setWritableFilter] = useState("");
  const [deletingInventoryId, setDeletingInventoryId] = useState<string | null>(null);

  const isOwnProfile = !id;

  async function loadProfile() {
    try {
      setIsLoading(true);
      setErrorMessage("");

      const token = session?.token ?? null;
      const endpoint = id ? `/api/users/${id}/profile` : "/api/users/me/profile";

      const profile = await apiRequest<UserProfileResponse>(endpoint, { token });
      setData(profile);
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Failed to load profile.");
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadProfile();
  }, [id, session?.token]);

  const ownedInventories = useMemo(() => {
    if (!data) {
      return [];
    }

    const filtered = data.ownedInventories.filter((inventory) =>
      inventory.title.toLowerCase().includes(ownedFilter.toLowerCase()) ||
      inventory.description.toLowerCase().includes(ownedFilter.toLowerCase())
    );

    return [...filtered].sort((a, b) => {
      if (ownedSortMode === "items") {
        return b.itemCount - a.itemCount;
      }

      return a.title.localeCompare(b.title);
    });
  }, [data, ownedFilter, ownedSortMode]);

  const writableInventories = useMemo(() => {
    if (!data) {
      return [];
    }

    const filtered = data.writableInventories.filter((inventory) =>
      inventory.title.toLowerCase().includes(writableFilter.toLowerCase()) ||
      inventory.description.toLowerCase().includes(writableFilter.toLowerCase())
    );

    return [...filtered].sort((a, b) => {
      if (writableSortMode === "items") {
        return b.itemCount - a.itemCount;
      }

      return a.title.localeCompare(b.title);
    });
  }, [data, writableFilter, writableSortMode]);

  async function deleteInventory(inventoryId: string) {
    if (!session?.token) {
      setErrorMessage("Please log in first.");
      return;
    }

    const confirmed = window.confirm("Delete this inventory?");
    if (!confirmed) {
      return;
    }

    try {
      setDeletingInventoryId(inventoryId);
      setErrorMessage("");

      await apiRequest(`/api/inventories/${inventoryId}`, {
        method: "DELETE",
        token: session.token
      });

      await loadProfile();
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Failed to delete inventory.");
    } finally {
      setDeletingInventoryId(null);
    }
  }

  return (
    <div className="stack-lg">
      {isLoading ? (
        <div className="surface-card section-pad text-center">Loading...</div>
      ) : errorMessage ? (
        <div className="alert alert-danger">{errorMessage}</div>
      ) : !data ? (
        <div className="surface-card section-pad text-center">Profile not found.</div>
      ) : (
        <>
          <section className="surface-card-strong section-pad-lg">
            <h1 className="page-title">{data.name}</h1>
            <p className="page-subtitle">{data.email}</p>
          </section>

          <section className="surface-card section-pad">
            <div className="d-flex justify-content-between align-items-center mb-3 flex-wrap gap-2">
              <h2 className="h5 mb-0">Owned inventories</h2>
              <div className="toolbar">
                {isOwnProfile && (
                  <button type="button" className="btn btn-primary" onClick={() => navigate("/inventories/new")}>
                    Create inventory
                  </button>
                )}

                <input
                  className="form-control"
                  value={ownedFilter}
                  onChange={(event) => setOwnedFilter(event.target.value)}
                  placeholder="Filter"
                />
                <select
                  className="form-select"
                  value={ownedSortMode}
                  onChange={(event) => setOwnedSortMode(event.target.value as SortMode)}
                >
                  <option value="title">Sort by title</option>
                  <option value="items">Sort by items</option>
                </select>
              </div>
            </div>

            <div className="table-responsive">
              <table className="table table-clean table-hover">
                <thead>
                  <tr>
                    <th>Title</th>
                    <th>Description</th>
                    <th>Category</th>
                    <th>Items</th>
                    {isOwnProfile && <th style={{ width: 220 }}>Actions</th>}
                  </tr>
                </thead>
                <tbody>
                  {ownedInventories.length === 0 ? (
                    <tr>
                      <td colSpan={isOwnProfile ? 5 : 4}>No owned inventories.</td>
                    </tr>
                  ) : (
                    ownedInventories.map((inventory) => (
                      <tr key={inventory.id}>
                        <td>
                          <Link to={`/inventories/${inventory.id}`}>{inventory.title}</Link>
                        </td>
                        <td>{inventory.description || "-"}</td>
                        <td>{inventory.categoryName}</td>
                        <td>{inventory.itemCount}</td>
                        {isOwnProfile && (
                          <td>
                            <div className="toolbar">
                              <Link className="btn btn-sm btn-outline-secondary" to={`/inventories/${inventory.id}`}>
                                Open
                              </Link>
                              <Link className="btn btn-sm btn-soft" to={`/inventories/${inventory.id}?tab=settings`}>
                                Edit
                              </Link>
                              <button
                                type="button"
                                className="btn btn-sm btn-outline-danger"
                                disabled={deletingInventoryId === inventory.id}
                                onClick={() => void deleteInventory(inventory.id)}
                              >
                                {deletingInventoryId === inventory.id ? "Deleting..." : "Delete"}
                              </button>
                            </div>
                          </td>
                        )}
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </section>

          <section className="surface-card section-pad">
            <div className="d-flex justify-content-between align-items-center mb-3">
              <h2 className="h5 mb-0">Writable inventories</h2>
              <div className="toolbar">
                <input
                  className="form-control"
                  value={writableFilter}
                  onChange={(event) => setWritableFilter(event.target.value)}
                  placeholder="Filter"
                />
                <select
                  className="form-select"
                  value={writableSortMode}
                  onChange={(event) => setWritableSortMode(event.target.value as SortMode)}
                >
                  <option value="title">Sort by title</option>
                  <option value="items">Sort by items</option>
                </select>
              </div>
            </div>

            <div className="table-responsive">
              <table className="table table-clean table-hover">
                <thead>
                  <tr>
                    <th>Title</th>
                    <th>Description</th>
                    <th>Category</th>
                    <th>Items</th>
                  </tr>
                </thead>
                <tbody>
                  {writableInventories.length === 0 ? (
                    <tr>
                      <td colSpan={4}>No writable inventories.</td>
                    </tr>
                  ) : (
                    writableInventories.map((inventory) => (
                      <tr key={inventory.id}>
                        <td>
                          <Link to={`/inventories/${inventory.id}`}>{inventory.title}</Link>
                        </td>
                        <td>{inventory.description || "-"}</td>
                        <td>{inventory.categoryName}</td>
                        <td>{inventory.itemCount}</td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </section>
        </>
      )}
    </div>
  );
}