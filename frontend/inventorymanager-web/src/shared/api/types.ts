export type InventoryResponse = {
  id: string;
  title: string;
  description: string;
  imageUrl: string;
  categoryId: string | null;
  categoryName: string | null;
  tags: string[];
  customIdFormat: string;
  version: number;
};

export type InventoryPermissionsResponse = {
  canManageInventory: boolean;
  canWriteItems: boolean;
};

export type CategoryResponse = {
  id: string;
  name: string;
};

export type TagAutocompleteResponse = {
  id: string;
  name: string;
};

export type ItemFieldDefinitionResponse = {
  id: string;
  inventoryId: string;
  fieldType: "string" | "text" | "number" | "link" | "boolean";
  title: string;
  description: string;
  showInTable: boolean;
  displayOrder: number;
};

export type ItemFieldValueResponse = {
  fieldDefinitionId: string;
  fieldType: string;
  title: string;
  stringValue: string | null;
  textValue: string | null;
  numberValue: number | null;
  linkValue: string | null;
  booleanValue: boolean | null;
};

export type ItemResponse = {
  id: string;
  inventoryId: string;
  customId: string;
  name: string;
  version: number;
  likeCount: number;
  customValues: ItemFieldValueResponse[];
};

export type DiscussionResponse = {
  id: string;
  inventoryId: string;
  userId: string;
  userName: string;
  content: string;
  createdAt: string;
};

export type WriterResponse = {
  userId: string;
  name: string;
  email: string;
};

export type UserAutocompleteResponse = {
  id: string;
  name: string;
  email: string;
};

export type CustomIdPreviewResponse = {
  customId: string;
  sequenceNumber: number;
};

export type SearchInventoryHit = {
  id: string;
  title: string;
  description: string;
  imageUrl: string;
};

export type SearchItemHit = {
  id: string;
  inventoryId: string;
  customId: string;
  name: string;
};

export type SearchResultResponse = {
  inventories: SearchInventoryHit[];
  items: SearchItemHit[];
};

export type AdminUserResponse = {
  id: string;
  email: string;
  name: string;
  role: string;
  isBlocked: boolean;
  createdAt: string;
};

export type InventoryTopValueResponse = {
  value: string;
  count: number;
};

export type InventoryNumericFieldStatsResponse = {
  fieldDefinitionId: string;
  title: string;
  populatedCount: number;
  min: number | null;
  max: number | null;
  average: number | null;
};

export type InventoryTextFieldStatsResponse = {
  fieldDefinitionId: string;
  title: string;
  topValues: InventoryTopValueResponse[];
};

export type InventoryStatsResponse = {
  itemCount: number;
  totalLikes: number;
  numericFields: InventoryNumericFieldStatsResponse[];
  textFields: InventoryTextFieldStatsResponse[];
};

export type DashboardInventoryCardResponse = {
  id: string;
  title: string;
  description: string;
  imageUrl: string;
  creatorName: string;
  itemCount: number;
};

export type DashboardTagResponse = {
  name: string;
  inventoryCount: number;
};

export type DashboardResponse = {
  latestInventories: DashboardInventoryCardResponse[];
  topInventories: DashboardInventoryCardResponse[];
  tagCloud: DashboardTagResponse[];
};

export type UserInventoryCardResponse = {
  id: string;
  title: string;
  description: string;
  imageUrl: string;
  categoryName: string;
  itemCount: number;
};

export type UserProfileResponse = {
  id: string;
  name: string;
  email: string;
  ownedInventories: UserInventoryCardResponse[];
  writableInventories: UserInventoryCardResponse[];
};