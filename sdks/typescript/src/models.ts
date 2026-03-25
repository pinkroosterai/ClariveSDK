export interface TemplateField {
  id: string;
  promptId: string;
  name: string;
  type: string;
  enumValues: string[] | null;
  defaultValue: string | null;
  min: number | null;
  max: number | null;
}

export interface Prompt {
  content: string;
  order: number;
  isTemplate: boolean;
  templateFields: TemplateField[] | null;
}

export interface TabSummary {
  id: string;
  name: string;
  isMainTab: boolean;
  forkedFromVersion: number | null;
}

export interface PromptEntry {
  id: string;
  title: string;
  systemMessage: string | null;
  version: number;
  prompts: Prompt[];
  tags: string[];
  updatedAt: string;
  publishedAt: string | null;
  tabs: TabSummary[];
  tabCount: number;
}

export interface GenerateRequest {
  fields?: Record<string, string>;
}

export interface RenderedPrompt {
  content: string;
  order: number;
}

export interface GenerateResponse {
  id: string;
  title: string;
  version: number;
  systemMessage: string | null;
  renderedPrompts: RenderedPrompt[];
}

export interface EntrySummary {
  id: string;
  title: string;
  version: number;
  hasSystemMessage: boolean;
  isTemplate: boolean;
  isChain: boolean;
  promptCount: number;
  firstPromptPreview: string | null;
  tags: string[];
  createdAt: string;
  updatedAt: string;
  tabs: TabSummary[];
  tabCount: number;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface TagInfo {
  name: string;
  entryCount: number;
}

export interface ListEntriesOptions {
  folderId?: string;
  tags?: string;
  tagMode?: string;
  page?: number;
  pageSize?: number;
  search?: string;
  sortBy?: string;
}
