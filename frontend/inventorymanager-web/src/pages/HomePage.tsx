import { useEffect, useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { apiRequest, ApiError } from "../shared/api/apiClient";
import { useAuth } from "../shared/auth/AuthContext";

type DashboardInventoryCardResponse = {
  id: string;
  title: string;
  description: string;
  imageUrl: string;
  ownerName: string;
  itemCount: number;
};

type DashboardTagResponse = {
  name: string;
  inventoryCount: number;
};

type DashboardResponse = {
  latestInventories: DashboardInventoryCardResponse[];
  topInventories: DashboardInventoryCardResponse[];
  tagCloud: DashboardTagResponse[];
};

export function HomePage() {
  const navigate = useNavigate();
  const { session } = useAuth();
  const [data, setData] = useState<DashboardResponse | null>(null);
  const [errorMessage, setErrorMessage] = useState("");
  const [searchText, setSearchText] = useState("");

  useEffect(() => {
    let mounted = true;

    async function load() {
      try {
        const response = await apiRequest<DashboardResponse>("/api/dashboard", {
          token: session?.token ?? null
        });

        if (mounted) {
          setData(response);
        }
      } catch (error) {
        if (mounted) {
          setErrorMessage(error instanceof ApiError ? error.message : "Failed to load dashboard.");
        }
      }
    }

    void load();

    return () => {
      mounted = false;
    };
  }, [session?.token]);

  function handleSearchSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    navigate(`/search?q=${encodeURIComponent(searchText.trim())}`);
  }

  return (
    <div className="stack-lg">
      <section className="surface-card-strong section-pad-lg home-hero">
        <div className="home-hero-main">
          <h1 className="page-title">Explore inventories</h1>
          <p className="page-subtitle">
            Browse inventories, open popular collections, and search by tags.
          </p>

          <form className="home-search-bar mt-4" onSubmit={handleSearchSubmit}>
            <input
              className="form-control"
              placeholder="Search inventories and items"
              value={searchText}
              onChange={(event) => setSearchText(event.target.value)}
            />
            <button type="submit" className="btn btn-primary">
              Search
            </button>
          </form>
        </div>

        <div className="home-hero-actions">
          <Link className="btn btn-outline-secondary" to="/inventories">
            Browse inventories
          </Link>

          {session && (
            <Link className="btn btn-primary" to="/inventories/new">
              Create inventory
            </Link>
          )}
        </div>
      </section>

      {errorMessage && <div className="alert alert-danger">{errorMessage}</div>}

      <section className="surface-card section-pad">
        <h2 className="h3 mb-4">Latest inventories</h2>

        <div className="table-responsive">
          <table className="table table-clean table-hover">
            <thead>
              <tr>
                <th>Title</th>
                <th>Description</th>
                <th>Creator</th>
                <th>Items</th>
              </tr>
            </thead>
            <tbody>
              {!data || data.latestInventories.length === 0 ? (
                <tr>
                  <td colSpan={4}>No inventories yet.</td>
                </tr>
              ) : (
                data.latestInventories.map((inventory) => (
                  <tr
                    key={inventory.id}
                    className="table-row-link"
                    onClick={() => navigate(`/inventories/${inventory.id}`)}
                    style={{ cursor: "pointer" }}
                  >
                    <td>{inventory.title}</td>
                    <td>{inventory.description || "-"}</td>
                    <td>{inventory.ownerName}</td>
                    <td>
                      <span className="tag-pill">{inventory.itemCount}</span>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </section>

      <section className="surface-card section-pad">
        <h2 className="h3 mb-4">Top 5 popular inventories</h2>

        <div className="table-responsive">
          <table className="table table-clean table-hover">
            <thead>
              <tr>
                <th>Title</th>
                <th>Description</th>
                <th>Creator</th>
                <th>Items</th>
              </tr>
            </thead>
            <tbody>
              {!data || data.topInventories.length === 0 ? (
                <tr>
                  <td colSpan={4}>No popular inventories yet.</td>
                </tr>
              ) : (
                data.topInventories.map((inventory) => (
                  <tr
                    key={inventory.id}
                    className="table-row-link"
                    onClick={() => navigate(`/inventories/${inventory.id}`)}
                    style={{ cursor: "pointer" }}
                  >
                    <td>{inventory.title}</td>
                    <td>{inventory.description || "-"}</td>
                    <td>{inventory.ownerName}</td>
                    <td>
                      <span className="tag-pill">{inventory.itemCount}</span>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </section>

      <section className="surface-card section-pad">
        <h2 className="h3 mb-4">Tag cloud</h2>

        {!data || data.tagCloud.length === 0 ? (
          <div className="empty-state">No tags yet.</div>
        ) : (
          <div className="inline-list">
            {data.tagCloud.map((tag) => (
              <button
                key={tag.name}
                type="button"
                className="tag-pill tag-cloud-pill"
                onClick={() => navigate(`/search?q=${encodeURIComponent(tag.name)}`)}
              >
                {tag.name} ({tag.inventoryCount})
              </button>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}