import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import "bootstrap/dist/css/bootstrap.min.css";
import App from "./App";
import { AuthProvider } from "./shared/auth/AuthContext";
import { UiPreferencesProvider } from "./shared/ui/UiPreferencesContext";
import "./index.css";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <BrowserRouter>
      <AuthProvider>
        <UiPreferencesProvider>
          <App />
        </UiPreferencesProvider>
      </AuthProvider>
    </BrowserRouter>
  </React.StrictMode>
);