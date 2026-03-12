export type UiLanguage = "en" | "bn";

export const translations = {
  en: {
    search: "Search",
    login: "Login",
    logout: "Logout",
    lightTheme: "Light",
    darkTheme: "Dark",
    language: "Language",
    myPage: "My page",
    browseInventories: "Browse inventories",
    createInventory: "Create inventory",
    latestInventories: "Latest inventories",
    topInventories: "Top 5 popular inventories",
    tagCloud: "Tag cloud",
    ownedInventories: "Owned inventories",
    writableInventories: "Writable inventories",
    creator: "Creator",
    items: "Items",
    description: "Description"
  },
  bn: {
    search: "সার্চ",
    login: "লগইন",
    logout: "লগআউট",
    lightTheme: "লাইট",
    darkTheme: "ডার্ক",
    language: "ভাষা",
    myPage: "আমার পেজ",
    browseInventories: "ইনভেন্টরি দেখুন",
    createInventory: "ইনভেন্টরি তৈরি করুন",
    latestInventories: "সর্বশেষ ইনভেন্টরি",
    topInventories: "জনপ্রিয় শীর্ষ ৫ ইনভেন্টরি",
    tagCloud: "ট্যাগ ক্লাউড",
    ownedInventories: "নিজের ইনভেন্টরি",
    writableInventories: "লিখতে পারা ইনভেন্টরি",
    creator: "তৈরি করেছেন",
    items: "আইটেম",
    description: "বিবরণ"
  }
} as const;