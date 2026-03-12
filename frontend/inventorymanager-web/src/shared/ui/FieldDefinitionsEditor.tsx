import type { ItemFieldDefinitionResponse } from "../api/types";

type EditableField = ItemFieldDefinitionResponse;

type Props = {
  value: EditableField[];
  onChange: (value: EditableField[]) => void;
};

export function FieldDefinitionsEditor({ value, onChange }: Props) {
  function moveField(fromIndex: number, toIndex: number) {
    const next = [...value];
    const [moved] = next.splice(fromIndex, 1);
    next.splice(toIndex, 0, moved);

    onChange(
      next.map((field, index) => ({
        ...field,
        displayOrder: index
      }))
    );
  }

  function handleDragStart(event: React.DragEvent<HTMLDivElement>, index: number) {
    event.dataTransfer.setData("text/plain", String(index));
  }

  function handleDrop(event: React.DragEvent<HTMLDivElement>, index: number) {
    event.preventDefault();
    const fromIndex = Number(event.dataTransfer.getData("text/plain"));

    if (Number.isNaN(fromIndex) || fromIndex === index) {
      return;
    }

    moveField(fromIndex, index);
  }

  function updateField(index: number, patch: Partial<EditableField>) {
    const next = [...value];
    next[index] = { ...next[index], ...patch };
    onChange(next);
  }

  return (
    <div className="stack-md">
      <div className="helper-text">Drag fields to reorder them.</div>

      {value.map((field, index) => (
        <div
          key={field.id}
          className="surface-card section-pad"
          draggable
          onDragStart={(event) => handleDragStart(event, index)}
          onDragOver={(event) => event.preventDefault()}
          onDrop={(event) => handleDrop(event, index)}
        >
          <div className="row g-3">
            <div className="col-md-4">
              <input
                className="form-control"
                value={field.title}
                onChange={(event) => updateField(index, { title: event.target.value })}
                placeholder="Title"
              />
            </div>

            <div className="col-md-4">
              <input
                className="form-control"
                value={field.description}
                onChange={(event) => updateField(index, { description: event.target.value })}
                placeholder="Description"
              />
            </div>

            <div className="col-md-2">
              <select
                className="form-select"
                value={field.fieldType}
                onChange={(event) => updateField(index, { fieldType: event.target.value as EditableField["fieldType"] })}
              >
                <option value="string">String</option>
                <option value="text">Text</option>
                <option value="number">Number</option>
                <option value="link">Link</option>
                <option value="boolean">Boolean</option>
              </select>
            </div>

            <div className="col-md-2 d-flex align-items-center">
              <div className="form-check">
                <input
                  className="form-check-input"
                  type="checkbox"
                  checked={field.showInTable}
                  onChange={(event) => updateField(index, { showInTable: event.target.checked })}
                />
                <label className="form-check-label">Show in table</label>
              </div>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}