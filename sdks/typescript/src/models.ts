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

export interface PromptEntry {
  id: string;
  title: string;
  systemMessage: string | null;
  version: number;
  prompts: Prompt[];
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
