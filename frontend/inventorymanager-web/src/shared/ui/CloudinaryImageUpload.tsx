import { useState } from "react";

type Props = {
  value: string;
  onChange: (value: string) => void;
};

export function CloudinaryImageUpload({ value, onChange }: Props) {
  const [isUploading, setIsUploading] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  async function handleFileChange(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    const cloudName = import.meta.env.VITE_CLOUDINARY_CLOUD_NAME;
    const uploadPreset = import.meta.env.VITE_CLOUDINARY_UPLOAD_PRESET;

    if (!cloudName || !uploadPreset) {
      setErrorMessage("Cloudinary env variables are missing.");
      return;
    }

    const formData = new FormData();
    formData.append("file", file);
    formData.append("upload_preset", uploadPreset);

    try {
      setIsUploading(true);
      setErrorMessage("");

      const response = await fetch(`https://api.cloudinary.com/v1_1/${cloudName}/image/upload`, {
        method: "POST",
        body: formData
      });

      if (!response.ok) {
        throw new Error("Upload failed.");
      }

      const data = await response.json();
      onChange(data.secure_url ?? "");
    } catch {
      setErrorMessage("Image upload failed.");
    } finally {
      setIsUploading(false);
    }
  }

  return (
    <div className="stack-sm">
      <input
        type="text"
        className="form-control"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder="Image URL"
      />

      <input
        type="file"
        className="form-control"
        accept="image/*"
        onChange={handleFileChange}
      />

      {isUploading && <div className="helper-text">Uploading...</div>}
      {errorMessage && <div className="text-danger">{errorMessage}</div>}
      {value && <img src={value} alt="Inventory" style={{ maxWidth: "240px", borderRadius: "12px" }} />}
    </div>
  );
}