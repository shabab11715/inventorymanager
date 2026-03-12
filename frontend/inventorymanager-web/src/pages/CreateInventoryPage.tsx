import { useEffect, useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { apiRequest, ApiError } from "../shared/api/apiClient";
import { useAuth } from "../shared/auth/AuthContext";
import { CloudinaryImageUpload } from "../shared/ui/CloudinaryImageUpload";
import type { CategoryResponse, InventoryResponse, TagAutocompleteResponse } from "../shared/api/types";

export function CreateInventoryPage() {
  const navigate = useNavigate();
  const { session, isAuthenticated } = useAuth();

  const [categories, setCategories] = useState<CategoryResponse[]>([]);
  const [tagSuggestions, setTagSuggestions] = useState<TagAutocompleteResponse[]>([]);
  const [tagInput, setTagInput] = useState("");
  const [form, setForm] = useState({
    title: "",
    description: "",
    imageUrl: "",
    categoryId: "",
    tags: [] as string[]
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    let mounted = true;

    async function load() {
      try {
        const data = await apiRequest<CategoryResponse[]>("/api/inventories/categories", {
          token: session?.token ?? null
        });

        if (mounted) {
          setCategories(data);
        }
      } catch {
      }
    }

    void load();

    return () => {
      mounted = false;
    };
  }, [session?.token]);

  useEffect(() => {
    let mounted = true;

    const term = tagInput.trim();
    if (!term) {
      setTagSuggestions([]);
      return;
    }

    const timer = window.setTimeout(async () => {
      try {
        const data = await apiRequest<TagAutocompleteResponse[]>(
          `/api/inventories/tag-autocomplete?term=${encodeURIComponent(term)}`,
          { token: session?.token ?? null }
        );

        if (mounted) {
          setTagSuggestions(data);
        }
      } catch {
      }
    }, 250);

    return () => {
      mounted = false;
      window.clearTimeout(timer);
    };
  }, [tagInput, session?.token]);

  function addTag(value: string) {
    const normalized = value.trim().toLowerCase();

    if (!normalized || form.tags.includes(normalized)) {
      setTagInput("");
      return;
    }

    setForm((current) => ({
      ...current,
      tags: [...current.tags, normalized]
    }));

    setTagInput("");
    setTagSuggestions([]);
  }

  function removeTag(tag: string) {
    setForm((current) => ({
      ...current,
      tags: current.tags.filter((x) => x !== tag)
    }));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!session?.token) {
      setErrorMessage("Please log in first.");
      return;
    }

    try {
      setIsSubmitting(true);
      setErrorMessage("");

      const created = await apiRequest<InventoryResponse>("/api/inventories", {
        method: "POST",
        token: session.token,
        body: {
          title: form.title,
          description: form.description,
          imageUrl: form.imageUrl,
          categoryId: form.categoryId || null,
          tags: form.tags
        }
      });

      navigate(`/inventories/${created.id}`);
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Failed to create inventory.");
    } finally {
      setIsSubmitting(false);
    }
  }

  if (!isAuthenticated) {
    return <div className="alert alert-warning">Please log in to create an inventory.</div>;
  }

  return (
    <div className="stack-lg">
      <section className="page-header">
        <div>
          <h1 className="page-title">Create inventory</h1>
          <p className="page-subtitle">Start with the core metadata, then configure fields, access, and custom IDs.</p>
        </div>
      </section>

      <section className="surface-card-strong section-pad-lg">
        {errorMessage && <div className="alert alert-danger">{errorMessage}</div>}

        <form className="stack-lg" onSubmit={handleSubmit}>
          <div>
            <label className="form-label">Title</label>
            <input
              className="form-control"
              value={form.title}
              onChange={(event) => setForm((current) => ({ ...current, title: event.target.value }))}
              required
            />
          </div>

          <div>
            <label className="form-label">Description</label>
            <textarea
              className="form-control"
              rows={5}
              value={form.description}
              onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))}
            />
          </div>

          <div>
            <label className="form-label">Image</label>
            <CloudinaryImageUpload
              value={form.imageUrl}
              onChange={(value) => setForm((current) => ({ ...current, imageUrl: value }))}
            />
          </div>

          <div>
            <label className="form-label">Category</label>
            <select
              className="form-select"
              value={form.categoryId}
              onChange={(event) => setForm((current) => ({ ...current, categoryId: event.target.value }))}
            >
              <option value="">Select category</option>
              {categories.map((category) => (
                <option value={category.id} key={category.id}>
                  {category.name}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="form-label">Tags</label>

            <div className="toolbar">
              <input
                className="form-control"
                value={tagInput}
                onChange={(event) => setTagInput(event.target.value)}
                placeholder="Type a tag"
              />
              <button type="button" className="btn btn-soft" onClick={() => addTag(tagInput)}>
                Add tag
              </button>
            </div>

            {tagSuggestions.length > 0 && (
              <div className="list-group professional-list mt-2">
                {tagSuggestions.map((tag) => (
                  <button
                    key={tag.id}
                    type="button"
                    className="list-group-item list-group-item-action"
                    onClick={() => addTag(tag.name)}
                  >
                    {tag.name}
                  </button>
                ))}
              </div>
            )}

            {form.tags.length > 0 && (
              <div className="inline-list mt-3">
                {form.tags.map((tag) => (
                  <span key={tag} className="tag-pill">
                    {tag}
                    <button type="button" className="btn btn-sm border-0 p-0" onClick={() => removeTag(tag)}>
                      ×
                    </button>
                  </span>
                ))}
              </div>
            )}
          </div>

          <div className="toolbar">
            <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
              {isSubmitting ? "Creating..." : "Create inventory"}
            </button>
            <button type="button" className="btn btn-outline-secondary" onClick={() => navigate("/inventories")}>
              Cancel
            </button>
          </div>
        </form>
      </section>
    </div>
  );
}