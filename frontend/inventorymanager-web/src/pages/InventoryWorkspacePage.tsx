import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams, useSearchParams } from "react-router-dom";
import { apiRequest, ApiError } from "../shared/api/apiClient";
import { useAuth } from "../shared/auth/AuthContext";
import { MarkdownBlock } from "../shared/ui/MarkdownBlock";
import { CloudinaryImageUpload } from "../shared/ui/CloudinaryImageUpload";
import { FieldDefinitionsEditor } from "../shared/ui/FieldDefinitionsEditor";
import type {
  CategoryResponse,
  CustomIdPreviewResponse,
  DiscussionResponse,
  InventoryPermissionsResponse,
  InventoryResponse,
  InventoryStatsResponse,
  ItemFieldDefinitionResponse,
  ItemResponse,
  TagAutocompleteResponse,
  UserAutocompleteResponse,
  WriterResponse
} from "../shared/api/types";

const allTabs = ["items", "discussion", "settings", "custom-id", "fields", "access", "stats"] as const;
type TabKey = (typeof allTabs)[number];

type ItemFormValue = {
  fieldDefinitionId: string;
  stringValue: string | null;
  textValue: string | null;
  numberValue: number | null;
  linkValue: string | null;
  booleanValue: boolean | null;
};

type LikeStatusResponse = {
  itemId: string;
  count: number;
  likedByUser: boolean;
};

type EditableField = {
  id: string;
  inventoryId: string;
  fieldType: "string" | "text" | "number" | "link" | "boolean";
  title: string;
  description: string;
  showInTable: boolean;
  displayOrder: number;
};

export function InventoryWorkspacePage() {
  const { inventoryId } = useParams();
  const navigate = useNavigate();
  const { session, isAuthenticated } = useAuth();
  const [searchParams, setSearchParams] = useSearchParams();

  const activeTab = (searchParams.get("tab") as TabKey) || "items";

  const [inventory, setInventory] = useState<InventoryResponse | null>(null);
  const [items, setItems] = useState<ItemResponse[]>([]);
  const [fields, setFields] = useState<ItemFieldDefinitionResponse[]>([]);
  const [editableFields, setEditableFields] = useState<EditableField[]>([]);
  const [discussions, setDiscussions] = useState<DiscussionResponse[]>([]);
  const [stats, setStats] = useState<InventoryStatsResponse | null>(null);
  const [writers, setWriters] = useState<WriterResponse[]>([]);
  const [categories, setCategories] = useState<CategoryResponse[]>([]);
  const [permissions, setPermissions] = useState<InventoryPermissionsResponse>({
    canManageInventory: false,
    canWriteItems: false
  });
  const [writerSortMode, setWriterSortMode] = useState<"name" | "email">("name");
  const [tagSuggestions, setTagSuggestions] = useState<TagAutocompleteResponse[]>([]);
  const [writerSuggestions, setWriterSuggestions] = useState<UserAutocompleteResponse[]>([]);

  const [errorMessage, setErrorMessage] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [autosaveState, setAutosaveState] = useState<"idle" | "saving" | "saved" | "error">("idle");

  const [settingsForm, setSettingsForm] = useState({
    title: "",
    description: "",
    imageUrl: "",
    categoryId: "",
    tagsText: ""
  });

  const [customIdFormat, setCustomIdFormat] = useState("");
  const [customIdPreview, setCustomIdPreview] = useState("");
  const [discussionText, setDiscussionText] = useState("");
  const [writerSearch, setWriterSearch] = useState("");
  const [tagInput, setTagInput] = useState("");
  const [isPublic, setIsPublic] = useState(false);

  const [selectedItemId, setSelectedItemId] = useState<string | null>(null);
  const [selectedWriterIds, setSelectedWriterIds] = useState<string[]>([]);
  const [selectedItemLikeState, setSelectedItemLikeState] = useState<LikeStatusResponse | null>(null);

  const [editingItemId, setEditingItemId] = useState<string | null>(null);
  const [itemForm, setItemForm] = useState({
    customId: "",
    name: "",
    version: 0,
    values: [] as ItemFormValue[]
  });

  const selectedItem = useMemo(
    () => items.find((item) => item.id === selectedItemId) ?? null,
    [items, selectedItemId]
  );

  const visibleFields = useMemo(
    () => fields.filter((field) => field.showInTable).sort((a, b) => a.displayOrder - b.displayOrder),
    [fields]
  );

  const visibleTabs = useMemo(() => {
    if (permissions.canManageInventory) {
      return allTabs;
    }

    if (permissions.canWriteItems) {
      return ["items", "discussion"] as const;
    }

    return ["items", "discussion", "stats"] as const;
  }, [permissions]);
  
  function setTab(tab: TabKey) {
    const next = new URLSearchParams(searchParams);
    next.set("tab", tab);
    setSearchParams(next);
  }

  async function loadDiscussionsOnly() {
    if (!inventoryId) {
      return;
    }

    const token = session?.token ?? null;

    const discussionData = await apiRequest<DiscussionResponse[]>(
      `/api/inventories/${inventoryId}/discussions?pageNumber=1&pageSize=50`,
      { token }
    );

    setDiscussions(discussionData);
  }

  async function loadAll() {
    if (!inventoryId) {
      return;
    }

    const token = session?.token ?? null;

    try {
      setIsLoading(true);
      setErrorMessage("");

      const [inventoryData, itemData, fieldData, discussionData, categoryData, statsData, permissionData] = await Promise.all([
        apiRequest<InventoryResponse>(`/api/inventories/${inventoryId}`, { token }),
        apiRequest<ItemResponse[]>(`/api/inventories/${inventoryId}/items?pageNumber=1&pageSize=100`, { token }),
        apiRequest<ItemFieldDefinitionResponse[]>(`/api/inventories/${inventoryId}/fields`, { token }),
        apiRequest<DiscussionResponse[]>(`/api/inventories/${inventoryId}/discussions?pageNumber=1&pageSize=50`, { token }),
        apiRequest<CategoryResponse[]>("/api/inventories/categories", { token }),
        apiRequest<InventoryStatsResponse>(`/api/inventories/${inventoryId}/stats`, { token }),
        apiRequest<InventoryPermissionsResponse>(`/api/inventories/${inventoryId}/permissions`, { token })
      ]);

      setInventory(inventoryData);
      setItems(itemData);
      setFields(fieldData);
      setDiscussions(discussionData);
      setCategories(categoryData);
      setStats(statsData);
      setPermissions(permissionData);

      setSettingsForm({
        title: inventoryData.title,
        description: inventoryData.description,
        imageUrl: inventoryData.imageUrl,
        categoryId: inventoryData.categoryId ?? "",
        tagsText: inventoryData.tags.join(", ")
      });

      setCustomIdFormat(inventoryData.customIdFormat);

      setEditableFields(
        fieldData
          .slice()
          .sort((a, b) => a.displayOrder - b.displayOrder)
          .map((field) => ({
            id: field.id,
            inventoryId: field.inventoryId,
            fieldType: field.fieldType,
            title: field.title,
            description: field.description,
            showInTable: field.showInTable,
            displayOrder: field.displayOrder
          }))
      );

      if (token && permissionData.canManageInventory) {
        try {
          const writerData = await apiRequest<WriterResponse[]>(
            `/api/inventories/${inventoryId}/writers?sortBy=${encodeURIComponent(writerSortMode)}`,
            { token }
          );
          setWriters(writerData);
        } catch {
          setWriters([]);
        }

        try {
          const visibility = await apiRequest<{ isPublic: boolean; version: number }>(
            `/api/inventories/${inventoryId}/visibility-state`,
            { token }
          );
          setIsPublic(visibility.isPublic);
        } catch {
          setIsPublic(false);
        }
      } else {
        setWriters([]);
        setIsPublic(false);
      }
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Failed to load inventory workspace.");
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    if (activeTab !== "discussion" || !inventoryId) {
      return;
    }

    const timer = window.setInterval(() => {
      void loadDiscussionsOnly();
    }, 3000);

    return () => {
      window.clearInterval(timer);
    };
  }, [activeTab, inventoryId, session?.token]);

  useEffect(() => {
    void loadAll();
  }, [inventoryId, session?.token]);

  useEffect(() => {
    if (!selectedItemId) {
      setSelectedItemLikeState(null);
      return;
    }

    let mounted = true;

    async function loadLikeStatus() {
      const token = session?.token;
      if (!selectedItemId || !token) {
        if (mounted) {
          setSelectedItemLikeState(null);
        }
        return;
      }

      try {
        const data = await apiRequest<LikeStatusResponse>(
          `/api/items/${selectedItemId}/likes/status`,
          {
            token
          }
        );

        if (mounted) {
          setSelectedItemLikeState({
            itemId: data.itemId,
            count: data.count,
            likedByUser: data.likedByUser
          });
        }
      } catch {
        if (mounted) {
          setSelectedItemLikeState(null);
        }
      }
    }

    void loadLikeStatus();

    return () => {
      mounted = false;
    };
  }, [selectedItemId, session?.token]);

  useEffect(() => {
    if (!inventoryId || !session?.token) return;
    if (!["settings", "custom-id"].includes(activeTab)) return;
    if (!inventory) return;

    const timer = window.setTimeout(async () => {
      try {
        setAutosaveState("saving");

        const tags = settingsForm.tagsText
          .split(",")
          .map((x) => x.trim().toLowerCase())
          .filter(Boolean);

        const updated = await apiRequest<InventoryResponse>(`/api/inventories/${inventoryId}/autosave`, {
          method: "PATCH",
          token: session.token,
          body: {
            title: settingsForm.title,
            description: settingsForm.description,
            imageUrl: settingsForm.imageUrl,
            categoryId: settingsForm.categoryId || null,
            tags,
            version: inventory.version
          }
        });

        setInventory(updated);
        setAutosaveState("saved");
      } catch {
        setAutosaveState("error");
      }
    }, 2500);

    return () => {
      window.clearTimeout(timer);
    };
  }, [settingsForm, inventoryId, session?.token, inventory, activeTab]);

  useEffect(() => {
    if (!session?.token || activeTab !== "settings") return;

    const term = tagInput.trim();
    if (!term) {
      setTagSuggestions([]);
      return;
    }

    const timer = window.setTimeout(async () => {
      try {
        const data = await apiRequest<TagAutocompleteResponse[]>(
          `/api/inventories/tag-autocomplete?term=${encodeURIComponent(term)}`,
          { token: session.token }
        );
        setTagSuggestions(data);
      } catch {
        setTagSuggestions([]);
      }
    }, 250);

    return () => window.clearTimeout(timer);
  }, [tagInput, session?.token, activeTab]);

  useEffect(() => {
    if (!session?.token || !inventoryId || activeTab !== "access") return;

    const value = writerSearch.trim();
    if (!value) {
      setWriterSuggestions([]);
      return;
    }

    const timer = window.setTimeout(async () => {
      try {
        const data = await apiRequest<UserAutocompleteResponse[]>(
          `/api/inventories/${inventoryId}/writer-autocomplete?term=${encodeURIComponent(value)}`,
          { token: session.token }
        );
        setWriterSuggestions(data);
      } catch {
        setWriterSuggestions([]);
      }
    }, 250);

    return () => window.clearTimeout(timer);
  }, [writerSearch, session?.token, inventoryId, activeTab]);

  useEffect(() => {
    const token = session?.token;

    if (!token || !inventoryId) return;
    if (activeTab !== "access") return;
    if (!permissions.canManageInventory) return;

    async function loadWritersOnly() {
      try {
        const data = await apiRequest<WriterResponse[]>(
          `/api/inventories/${inventoryId}/writers?sortBy=${encodeURIComponent(writerSortMode)}`,
          { token }
        );
        setWriters(data);
      } catch {
        setWriters([]);
      }
    }

    void loadWritersOnly();
  }, [session?.token, inventoryId, activeTab, permissions.canManageInventory, writerSortMode]);

  async function beginCreateItem() {
    let nextCustomId = "";

    if (inventoryId && session?.token) {
      try {
        const data = await apiRequest<CustomIdPreviewResponse>(
          `/api/inventories/${inventoryId}/custom-id-preview?customIdFormat=${encodeURIComponent(customIdFormat)}`,
          { token: session.token }
        );
        nextCustomId = data.customId;
      } catch {
        nextCustomId = "";
      }
    }

    setEditingItemId(null);
    setSelectedItemId(null);
    setItemForm({
      customId: nextCustomId,
      name: "",
      version: 0,
      values: fields.map((field) => ({
        fieldDefinitionId: field.id,
        stringValue: null,
        textValue: null,
        numberValue: null,
        linkValue: null,
        booleanValue: null
      }))
    });
  }

  async function beginEditItem(item: ItemResponse) {
    if (!inventoryId) {
      return;
    }

    try {
      const fullItem = await apiRequest<ItemResponse>(`/api/inventories/${inventoryId}/items/${item.id}`, {
        token: session?.token ?? null
      });

      setEditingItemId(fullItem.id);
      setSelectedItemId(fullItem.id);
      setItemForm({
        customId: fullItem.customId,
        name: fullItem.name,
        version: fullItem.version,
        values: fields.map((field) => {
          const existing = fullItem.customValues.find((x) => x.fieldDefinitionId === field.id);
          return {
            fieldDefinitionId: field.id,
            stringValue: existing?.stringValue ?? null,
            textValue: existing?.textValue ?? null,
            numberValue: existing?.numberValue ?? null,
            linkValue: existing?.linkValue ?? null,
            booleanValue: existing?.booleanValue ?? null
          };
        })
      });
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Failed to load item details.");
    }
  }

  function updateItemValue(fieldId: string, patch: Partial<ItemFormValue>) {
    setItemForm((current) => ({
      ...current,
      values: current.values.map((value) =>
        value.fieldDefinitionId === fieldId ? { ...value, ...patch } : value
      )
    }));
  }

  async function saveItem() {
    const token = session?.token;
    if (!inventoryId || !token) return;

    try {
      const body = {
        customId: itemForm.customId,
        name: itemForm.name,
        version: itemForm.version,
        customValues: itemForm.values
      };

      if (editingItemId) {
        await apiRequest(`/api/inventories/${inventoryId}/items/${editingItemId}`, {
          method: "PUT",
          token,
          body
        });
      } else {
        await apiRequest(`/api/inventories/${inventoryId}/items`, {
          method: "POST",
          token,
          body: {
            customId: itemForm.customId,
            name: itemForm.name,
            customValues: itemForm.values
          }
        });
      }

      beginCreateItem();
      await loadAll();
    } catch (error) {
      if (error instanceof ApiError) {
        if (error.message.toLowerCase().includes("customid") || error.message.toLowerCase().includes("custom id")) {
          setErrorMessage(
            "The item could not be saved because its Custom ID does not match the inventory's current ID format. Update the Custom ID and try again."
          );
        } else {
          setErrorMessage(error.message);
        }
      } else {
        setErrorMessage("Failed to save item.");
      }
    }
  }

  async function deleteSelectedItem() {
    const token = session?.token;
    if (!inventoryId || !token || !selectedItemId) return;

    try {
      await apiRequest(`/api/inventories/${inventoryId}/items/${selectedItemId}`, {
        method: "DELETE",
        token
      });

      setSelectedItemId(null);
      setEditingItemId(null);
      await loadAll();
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Failed to delete item.");
    }
  }

  async function toggleSelectedItemLike() {
    const token = session?.token;
    if (!selectedItemId || !token) return;

    try {
      const likedByUser = selectedItemLikeState?.likedByUser ?? false;

      await apiRequest(`/api/items/${selectedItemId}/likes`, {
        method: likedByUser ? "DELETE" : "POST",
        token
      });

      const data = await apiRequest<LikeStatusResponse>(
        `/api/items/${selectedItemId}/likes/status`,
        {
          token
        }
      );

      setSelectedItemLikeState(data);
      await loadAll();
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Failed to update like.");
    }
  }

  async function previewCustomId() {
    const token = session?.token;
    if (!inventoryId || !token) return;

    try {
      const data = await apiRequest<CustomIdPreviewResponse>(
        `/api/inventories/${inventoryId}/custom-id-preview?customIdFormat=${encodeURIComponent(customIdFormat)}`,
        { token }
      );
      setCustomIdPreview(data.customId);
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Failed to preview custom ID.");
    }
  }

  async function saveCustomId() {
    const token = session?.token;
    if (!inventoryId || !token || !inventory) return;

    try {
      const updated = await apiRequest<InventoryResponse>(`/api/inventories/${inventoryId}/custom-id-format`, {
        method: "PUT",
        token,
        body: {
          customIdFormat,
          version: inventory.version
        }
      });
      setInventory(updated);
      setCustomIdFormat(updated.customIdFormat);
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Failed to save custom ID format.");
    }
  }

  async function saveFields() {
    const token = session?.token;
    if (!inventoryId || !token) return;

    try {
      const updated = await apiRequest<ItemFieldDefinitionResponse[]>(`/api/inventories/${inventoryId}/fields`, {
        method: "PUT",
        token,
        body: {
          fields: editableFields.map((field) => ({
            fieldType: field.fieldType,
            title: field.title,
            description: field.description,
            showInTable: field.showInTable
          }))
        }
      });

      setFields(updated);
      setEditableFields(
        updated
          .slice()
          .sort((a, b) => a.displayOrder - b.displayOrder)
          .map((field) => ({
            id: field.id,
            inventoryId: field.inventoryId,
            fieldType: field.fieldType,
            title: field.title,
            description: field.description,
            showInTable: field.showInTable,
            displayOrder: field.displayOrder
          }))
      );
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Failed to save fields.");
    }
  }

  async function postDiscussion() {
    const token = session?.token;
    if (!inventoryId || !token || !discussionText.trim()) return;

    try {
      await apiRequest(`/api/inventories/${inventoryId}/discussions`, {
        method: "POST",
        token,
        body: {
          content: discussionText
        }
      });
      setDiscussionText("");
      await loadAll();
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Failed to post discussion.");
    }
  }

  async function addWriter(userId: string) {
    const token = session?.token;
    if (!inventoryId || !token) return;

    try {
      await apiRequest(`/api/inventories/${inventoryId}/writers`, {
        method: "POST",
        token,
        body: {
          userId
        }
      });
      setWriterSearch("");
      setWriterSuggestions([]);
      await loadAll();
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Failed to add writer.");
    }
  }

  async function removeSelectedWriters() {
    const token = session?.token;
    if (!inventoryId || !token || selectedWriterIds.length === 0) return;

    try {
      for (const writerUserId of selectedWriterIds) {
        await apiRequest(`/api/inventories/${inventoryId}/writers/${writerUserId}`, {
          method: "DELETE",
          token
        });
      }

      setSelectedWriterIds([]);
      await loadAll();
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Failed to remove selected writers.");
    }
  }

  async function saveVisibility() {
    const token = session?.token;
    if (!inventoryId || !token || !inventory) return;

    try {
      await apiRequest(`/api/inventories/${inventoryId}/visibility`, {
        method: "PUT",
        token,
        body: {
          isPublic,
          version: inventory.version
        }
      });
      await loadAll();
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Failed to save visibility.");
    }
  }

  async function deleteInventory() {
    const token = session?.token;
    if (!inventoryId || !token) return;

    try {
      await apiRequest(`/api/inventories/${inventoryId}`, {
        method: "DELETE",
        token
      });
      navigate("/inventories");
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Failed to delete inventory.");
    }
  }

  function toggleWriterSelection(writerId: string) {
    setSelectedWriterIds((current) =>
      current.includes(writerId)
        ? current.filter((id) => id !== writerId)
        : [...current, writerId]
    );
  }

  function addFieldRow() {
    if (!inventoryId) {
      return;
    }

    setEditableFields((current) => [
      ...current,
      {
        id: `new-${crypto.randomUUID()}`,
        inventoryId,
        fieldType: "string",
        title: "",
        description: "",
        showInTable: false,
        displayOrder: current.length + 1
      }
    ]);
  }

  if (!inventoryId) {
    return <div className="alert alert-danger">Missing inventory id.</div>;
  }

  if (isLoading) {
    return <div className="surface-card empty-state">Loading...</div>;
  }

  if (!inventory) {
    return <div className="alert alert-warning">Inventory not found.</div>;
  }

  return (
    <div className="stack-lg">
      <section className="surface-card-strong section-pad-lg">
        <div className="page-header">
          <div>
            <div className="toolbar mb-2">
              <h1 className="page-title">{inventory.title}</h1>
              {autosaveState === "saved" && <span className="soft-badge soft-badge-success">All changes saved</span>}
              {autosaveState === "saving" && <span className="soft-badge soft-badge-primary">Saving...</span>}
              {autosaveState === "error" && <span className="soft-badge soft-badge-danger">Autosave failed</span>}
            </div>
            <MarkdownBlock content={inventory.description || "No description"} />
          </div>

          <div className="toolbar">
            <button className="btn btn-outline-danger" onClick={deleteInventory} disabled={!session?.token}>
              Delete inventory
            </button>
          </div>
        </div>

        <div className="inventory-hero mt-4">
          <div className="stack-lg">
            <div className="inline-list">
              <span className="tag-pill">{inventory.categoryName ?? "No category"}</span>
              {inventory.tags.map((tag) => (
                <span key={tag} className="tag-pill">
                  {tag}
                </span>
              ))}
            </div>

            <div className="meta-grid">
              <div className="meta-item">
                <div className="meta-label">Items</div>
                <div className="meta-value">{items.length}</div>
              </div>
              <div className="meta-item">
                <div className="meta-label">Fields</div>
                <div className="meta-value">{fields.length}</div>
              </div>
              <div className="meta-item">
                <div className="meta-label">Writers</div>
                <div className="meta-value">{writers.length}</div>
              </div>
              <div className="meta-item">
                <div className="meta-label">Version</div>
                <div className="meta-value">{inventory.version}</div>
              </div>
            </div>
          </div>

          {inventory.imageUrl ? <img src={inventory.imageUrl} alt={inventory.title} /> : null}
        </div>
      </section>

      {errorMessage && <div className="alert alert-danger">{errorMessage}</div>}

      <ul className="nav nav-tabs professional-tabs">
        {visibleTabs.map((tab) => (
          <li className="nav-item" key={tab}>
            <button
              type="button"
              className={`nav-link ${activeTab === tab ? "active" : ""}`}
              onClick={() => setTab(tab)}
            >
              {tab}
            </button>
          </li>
        ))}
      </ul>

      <section className="surface-card section-pad-lg">
        {activeTab === "items" && (
          <div className="split-layout">
            <div className="stack-lg">
              <div className="surface-card section-pad">
                <div className="toolbar">
                  <button className="btn btn-primary" onClick={() => void beginCreateItem()} disabled={!permissions.canWriteItems}>
                    New item
                  </button>

                  <button
                    className="btn btn-soft"
                    onClick={() => selectedItem && void beginEditItem(selectedItem)}
                    disabled={!selectedItem}
                  >
                    Edit selected
                  </button>

                  <button
                    className="btn btn-outline-danger"
                    onClick={deleteSelectedItem}
                    disabled={!selectedItem || !permissions.canWriteItems}
                  >
                    Delete selected
                  </button>

                  <button
                    className="btn btn-outline-secondary"
                    onClick={toggleSelectedItemLike}
                    disabled={!selectedItem || !isAuthenticated}
                  >
                    {selectedItemLikeState?.likedByUser ? "Unlike selected" : "Like selected"}
                  </button>

                  <span className="helper-text ms-auto">
                    {selectedItem
                      ? `Selected: ${selectedItem.name}${selectedItemLikeState ? ` • likes ${selectedItemLikeState.count}` : ""}`
                      : "Select an item from the table"}
                  </span>
                </div>
              </div>

              <div className="surface-card">
                <div className="table-responsive">
                  <table className="table table-clean table-hover">
                    <thead>
                      <tr>
                        <th style={{ width: 52 }}></th>
                        <th>Custom ID</th>
                        <th>Name</th>
                        <th>Likes</th>
                        {visibleFields.map((field) => (
                          <th key={field.id}>{field.title}</th>
                        ))}
                      </tr>
                    </thead>
                    <tbody>
                      {items.length === 0 ? (
                        <tr>
                          <td colSpan={4 + visibleFields.length}>No items found.</td>
                        </tr>
                      ) : (
                        items.map((item) => {
                          const isSelected = item.id === selectedItemId;

                          return (
                            <tr
                              key={item.id}
                              className={isSelected ? "table-active" : ""}
                              onClick={() => setSelectedItemId(item.id)}
                            >
                              <td onClick={(event) => event.stopPropagation()}>
                                <input
                                  type="radio"
                                  className="form-check-input"
                                  checked={isSelected}
                                  onChange={() => setSelectedItemId(item.id)}
                                />
                              </td>
                              <td className="mono fw-semibold">{item.customId}</td>
                              <td>{item.name}</td>
                              <td>{item.likeCount}</td>
                              {visibleFields.map((field) => {
                                const value = item.customValues.find((x) => x.fieldDefinitionId === field.id);

                                let display = "-";

                                if (value) {
                                  if (value.fieldType === "string") display = value.stringValue || "-";
                                  if (value.fieldType === "text") display = value.textValue || "-";
                                  if (value.fieldType === "number") display = value.numberValue?.toString() || "-";
                                  if (value.fieldType === "link") display = value.linkValue || "-";
                                  if (value.fieldType === "boolean") display = value.booleanValue ? "True" : "False";
                                }

                                return <td key={field.id}>{display}</td>;
                              })}
                            </tr>
                          );
                        })
                      )}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>

            <div className="surface-card sticky-side section-pad">
              <div className="toolbar mb-3">
                <h2 className="h5 mb-0">{editingItemId ? "Edit item" : "Create item"}</h2>
              </div>

              <form
                className="stack-lg"
                onSubmit={(event) => {
                  event.preventDefault();
                  void saveItem();
                }}
              >
                <div>
                  <label className="form-label">Custom ID</label>
                  <input
                    className="form-control mono"
                    value={itemForm.customId}
                    onChange={(event) => setItemForm((current) => ({ ...current, customId: event.target.value }))}
                    disabled={!permissions.canWriteItems}
                  />
                  <div className="helper-text mt-2">Custom ID must match the current inventory format.</div>
                  <div className="helper-text mt-1">Current inventory format:</div>
                  <div className="mono small mt-1">{customIdFormat || "-"}</div>
                </div>

                <div>
                  <label className="form-label">Name</label>
                  <input
                    className="form-control"
                    value={itemForm.name}
                    onChange={(event) => setItemForm((current) => ({ ...current, name: event.target.value }))}
                    required
                  />
                </div>

                {fields.map((field) => {
                  const value = itemForm.values.find((x) => x.fieldDefinitionId === field.id);
                  if (!value) return null;

                  return (
                    <div key={field.id}>
                      <label className="form-label">{field.title}</label>

                      {field.fieldType === "string" && (
                        <input
                          className="form-control"
                          value={value.stringValue ?? ""}
                          onChange={(event) => updateItemValue(field.id, { stringValue: event.target.value })}
                        />
                      )}

                      {field.fieldType === "text" && (
                        <textarea
                          className="form-control"
                          rows={3}
                          value={value.textValue ?? ""}
                          onChange={(event) => updateItemValue(field.id, { textValue: event.target.value })}
                        />
                      )}

                      {field.fieldType === "number" && (
                        <input
                          type="number"
                          className="form-control"
                          value={value.numberValue ?? ""}
                          onChange={(event) =>
                            updateItemValue(field.id, {
                              numberValue: event.target.value === "" ? null : Number(event.target.value)
                            })
                          }
                        />
                      )}

                      {field.fieldType === "link" && (
                        <input
                          className="form-control"
                          value={value.linkValue ?? ""}
                          onChange={(event) => updateItemValue(field.id, { linkValue: event.target.value })}
                        />
                      )}

                      {field.fieldType === "boolean" && (
                        <div className="form-check">
                          <input
                            type="checkbox"
                            className="form-check-input"
                            checked={value.booleanValue ?? false}
                            onChange={(event) => updateItemValue(field.id, { booleanValue: event.target.checked })}
                          />
                        </div>
                      )}

                      {field.description && <div className="helper-text mt-1">{field.description}</div>}
                    </div>
                  );
                })}

                <button type="submit" className="btn btn-primary" disabled={!permissions.canWriteItems}>
                  {editingItemId ? "Save item" : "Create item"}
                </button>
              </form>
            </div>
          </div>
        )}

        {activeTab === "discussion" && (
          <div className="stack-lg">
            {isAuthenticated && (
              <form
                className="stack-lg"
                onSubmit={(event) => {
                  event.preventDefault();
                  void postDiscussion();
                }}
              >
                <textarea
                  className="form-control"
                  rows={4}
                  value={discussionText}
                  onChange={(event) => setDiscussionText(event.target.value)}
                  placeholder="Write a discussion message"
                />
                <div>
                  <button type="submit" className="btn btn-primary">
                    Post message
                  </button>
                </div>
              </form>
            )}

            {discussions.length === 0 ? (
              <div className="empty-state">No discussion yet.</div>
            ) : (
              discussions.map((discussion) => (
                <div key={discussion.id} className="surface-card section-pad">
                  <div className="helper-text mb-2">
                    <Link to={`/users/${discussion.userId}`}>{discussion.userName}</Link> •{" "}
                    {new Date(discussion.createdAt).toLocaleString()}
                  </div>
                  <MarkdownBlock content={discussion.content} />
                </div>
              ))
            )}
          </div>
        )}

        {activeTab === "settings" && permissions.canManageInventory && (
          <div className="split-layout">
            <div className="surface-card section-pad">
              <div className="stack-lg">
                <div>
                  <label className="form-label">Title</label>
                  <input
                    className="form-control"
                    value={settingsForm.title}
                    onChange={(event) => setSettingsForm((current) => ({ ...current, title: event.target.value }))}
                  />
                </div>

                <div>
                  <label className="form-label">Description</label>
                  <textarea
                    className="form-control"
                    rows={6}
                    value={settingsForm.description}
                    onChange={(event) => setSettingsForm((current) => ({ ...current, description: event.target.value }))}
                  />
                  <div className="helper-text mt-2">Markdown is supported.</div>
                </div>

                <div>
                  <label className="form-label">Image</label>
                  <CloudinaryImageUpload
                    value={settingsForm.imageUrl}
                    onChange={(value) => setSettingsForm((current) => ({ ...current, imageUrl: value }))}
                  />
                </div>

                <div>
                  <label className="form-label">Category</label>
                  <select
                    className="form-select"
                    value={settingsForm.categoryId}
                    onChange={(event) => setSettingsForm((current) => ({ ...current, categoryId: event.target.value }))}
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
                  <input
                    className="form-control"
                    value={settingsForm.tagsText}
                    onChange={(event) => setSettingsForm((current) => ({ ...current, tagsText: event.target.value }))}
                    placeholder="tag1, tag2, tag3"
                  />
                </div>
              </div>
            </div>

            <div className="surface-card section-pad sticky-side">
              <h2 className="h6 mb-3">Tag autocomplete</h2>

              <input
                className="form-control"
                value={tagInput}
                onChange={(event) => setTagInput(event.target.value)}
                placeholder="Search existing tags"
              />

              {tagSuggestions.length > 0 && (
                <div className="list-group professional-list mt-3">
                  {tagSuggestions.map((tag) => (
                    <button
                      key={tag.id}
                      type="button"
                      className="list-group-item list-group-item-action"
                      onClick={() =>
                        setSettingsForm((current) => {
                          const parts = current.tagsText
                            .split(",")
                            .map((x) => x.trim())
                            .filter(Boolean);

                          if (parts.includes(tag.name)) return current;

                          return {
                            ...current,
                            tagsText: [...parts, tag.name].join(", ")
                          };
                        })
                      }
                    >
                      {tag.name}
                    </button>
                  ))}
                </div>
              )}

              <div className="helper-text mt-3">This tab autosaves after a short pause.</div>
            </div>
          </div>
        )}

        {activeTab === "custom-id" && permissions.canManageInventory && (
          <div className="split-layout">
            <div className="surface-card section-pad">
              <div className="stack-lg">
                <div>
                  <label className="form-label">Custom ID format</label>
                  <textarea
                    className="form-control mono"
                    rows={10}
                    value={customIdFormat}
                    onChange={(event) => setCustomIdFormat(event.target.value)}
                  />
                </div>

                <div className="toolbar">
                  <button className="btn btn-soft" onClick={previewCustomId} disabled={!session?.token}>
                    Preview
                  </button>
                  <button className="btn btn-primary" onClick={saveCustomId} disabled={!session?.token}>
                    Save format
                  </button>
                </div>
              </div>
            </div>

            <div className="surface-card section-pad sticky-side">
              <h2 className="h6 mb-2">Preview</h2>
              <div className="mono fs-5 fw-semibold">{customIdPreview || "-"}</div>
              <div className="helper-text mt-3">
                Preview the next generated Custom ID before saving the current format.
              </div>
            </div>
          </div>
        )}

        {activeTab === "fields" && permissions.canManageInventory && (
          <div className="stack-lg">
            <FieldDefinitionsEditor value={editableFields} onChange={setEditableFields} />

            <div className="toolbar">
              <button type="button" className="btn btn-soft" onClick={addFieldRow}>
                Add field
              </button>

              <button className="btn btn-primary" onClick={saveFields} disabled={!session?.token}>
                Save fields
              </button>
            </div>
          </div>
        )}

        {activeTab === "access" && permissions.canManageInventory && (
          <div className="split-layout">
            <div className="stack-lg">
              <div className="surface-card section-pad">
                <h2 className="h5 mb-3">Inventory visibility</h2>
                <div className="form-check mb-3">
                  <input
                    type="checkbox"
                    className="form-check-input"
                    checked={isPublic}
                    onChange={(event) => setIsPublic(event.target.checked)}
                  />
                  <label className="form-check-label">Public write access for all authenticated users</label>
                </div>

                <div className="mb-3">
                  <label className="form-label">Writer sort mode</label>
                  <select
                    className="form-select"
                    value={writerSortMode}
                    onChange={(event) => setWriterSortMode(event.target.value as "name" | "email")}
                  >
                    <option value="name">Sort by name</option>
                    <option value="email">Sort by email</option>
                  </select>
                </div>

                <button className="btn btn-primary" onClick={saveVisibility} disabled={!session?.token}>
                  Save access
                </button>
              </div>

              <div className="surface-card section-pad">
                <div className="toolbar">
                  <button
                    className="btn btn-outline-danger"
                    onClick={removeSelectedWriters}
                    disabled={!session?.token || selectedWriterIds.length === 0}
                  >
                    Remove selected writers
                  </button>

                  <span className="helper-text ms-auto">Selected: {selectedWriterIds.length}</span>
                </div>
              </div>

              <div className="surface-card">
                <div className="table-responsive">
                  <table className="table table-clean table-hover">
                    <thead>
                      <tr>
                        <th style={{ width: 52 }}></th>
                        <th>Name</th>
                        <th>Email</th>
                      </tr>
                    </thead>
                    <tbody>
                      {writers.length === 0 ? (
                        <tr>
                          <td colSpan={3}>No writers found.</td>
                        </tr>
                      ) : (
                        writers.map((writer) => {
                          const isSelected = selectedWriterIds.includes(writer.userId);

                          return (
                            <tr
                              key={writer.userId}
                              className={isSelected ? "table-active" : ""}
                              onClick={() => toggleWriterSelection(writer.userId)}
                            >
                              <td onClick={(event) => event.stopPropagation()}>
                                <input
                                  type="checkbox"
                                  className="form-check-input"
                                  checked={isSelected}
                                  onChange={() => toggleWriterSelection(writer.userId)}
                                />
                              </td>
                              <td>{writer.name}</td>
                              <td>{writer.email}</td>
                            </tr>
                          );
                        })
                      )}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>

            <div className="surface-card section-pad sticky-side">
              <h2 className="h6 mb-3">Add writer</h2>
              <input
                className="form-control"
                value={writerSearch}
                onChange={(event) => setWriterSearch(event.target.value)}
                placeholder="Search by name or email"
              />

              {writerSuggestions.length > 0 && (
                <div className="list-group professional-list mt-3">
                  {writerSuggestions.map((user) => (
                    <button
                      key={user.id}
                      type="button"
                      className="list-group-item list-group-item-action"
                      onClick={() => addWriter(user.id)}
                    >
                      {user.name} ({user.email})
                    </button>
                  ))}
                </div>
              )}
            </div>
          </div>
        )}

        {activeTab === "stats" && (
          <div className="stack-lg">
            <div className="meta-grid">
              <div className="meta-item">
                <div className="meta-label">Items</div>
                <div className="meta-value">{stats?.itemCount ?? 0}</div>
              </div>
              <div className="meta-item">
                <div className="meta-label">Total likes</div>
                <div className="meta-value">{stats?.totalLikes ?? 0}</div>
              </div>
            </div>

            <div className="surface-card section-pad">
              <h2 className="h5 mb-3">Numeric fields</h2>

              {!stats || stats.numericFields.length === 0 ? (
                <div className="empty-state">No numeric field stats yet.</div>
              ) : (
                <div className="table-responsive">
                  <table className="table table-clean table-hover">
                    <thead>
                      <tr>
                        <th>Field</th>
                        <th>Count</th>
                        <th>Min</th>
                        <th>Max</th>
                        <th>Average</th>
                      </tr>
                    </thead>
                    <tbody>
                      {stats.numericFields.map((field) => (
                        <tr key={field.fieldDefinitionId}>
                          <td>{field.title}</td>
                          <td>{field.populatedCount}</td>
                          <td>{field.min ?? "-"}</td>
                          <td>{field.max ?? "-"}</td>
                          <td>{field.average ?? "-"}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>

            <div className="surface-card section-pad">
              <h2 className="h5 mb-3">Most frequent text values</h2>

              {!stats || stats.textFields.length === 0 ? (
                <div className="empty-state">No text field stats yet.</div>
              ) : (
                <div className="stack-lg">
                  {stats.textFields.map((field) => (
                    <div key={field.fieldDefinitionId}>
                      <h3 className="h6 mb-2">{field.title}</h3>

                      {field.topValues.length === 0 ? (
                        <div className="helper-text">No values yet.</div>
                      ) : (
                        <div className="table-responsive">
                          <table className="table table-clean table-hover">
                            <thead>
                              <tr>
                                <th>Value</th>
                                <th>Count</th>
                              </tr>
                            </thead>
                            <tbody>
                              {field.topValues.map((value) => (
                                <tr key={`${field.fieldDefinitionId}-${value.value}`}>
                                  <td>{value.value}</td>
                                  <td>{value.count}</td>
                                </tr>
                              ))}
                            </tbody>
                          </table>
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        )}
      </section>
    </div>
  );
}