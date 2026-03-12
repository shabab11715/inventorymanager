import { createContext, useContext, useEffect, useMemo, useState } from "react";
import type { ReactNode } from "react";
import { translations, type UiLanguage } from "./translations";

type UiTheme = "light" | "dark";

type UiPreferencesContextValue = {
  language: UiLanguage;
  setLanguage: (value: UiLanguage) => void;
  theme: UiTheme;
  setTheme: (value: UiTheme) => void;
  t: (key: keyof typeof translations.en) => string;
};

const UiPreferencesContext = createContext<UiPreferencesContextValue | undefined>(undefined);

const LANGUAGE_KEY = "inventorymanager.language";
const THEME_KEY = "inventorymanager.theme";

export function UiPreferencesProvider({ children }: { children: ReactNode }) {
  const [language, setLanguageState] = useState<UiLanguage>(() => {
    const saved = window.localStorage.getItem(LANGUAGE_KEY);
    return saved === "bn" ? "bn" : "en";
  });

  const [theme, setThemeState] = useState<UiTheme>(() => {
    const saved = window.localStorage.getItem(THEME_KEY);
    return saved === "dark" ? "dark" : "light";
  });

  useEffect(() => {
    window.localStorage.setItem(LANGUAGE_KEY, language);
  }, [language]);

  useEffect(() => {
    window.localStorage.setItem(THEME_KEY, theme);
    document.documentElement.setAttribute("data-theme", theme);
    document.body.setAttribute("data-theme", theme);
  }, [theme]);

  const value = useMemo<UiPreferencesContextValue>(() => {
    return {
      language,
      setLanguage: setLanguageState,
      theme,
      setTheme: setThemeState,
      t: (key) => translations[language][key]
    };
  }, [language, theme]);

  return <UiPreferencesContext.Provider value={value}>{children}</UiPreferencesContext.Provider>;
}

export function useUiPreferences() {
  const context = useContext(UiPreferencesContext);

  if (!context) {
    throw new Error("useUiPreferences must be used inside UiPreferencesProvider.");
  }

  return context;
}