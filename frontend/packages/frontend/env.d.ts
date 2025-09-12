interface ImportMetaEnv {
  VITE_API_BASE_URL?: string;
  API_BASE_URL?: string;
  VITE_BOT_TOKEN?: string;
  BOT_TOKEN?: string;
  VITE_API_EMAIL?: string;
  API_EMAIL?: string;
  VITE_API_PASSWORD?: string;
  API_PASSWORD?: string;
  VITE_AZURE_OPENAI_ENDPOINT?: string;
  AZURE_OPENAI_ENDPOINT?: string;
  VITE_AZURE_OPENAI_KEY?: string;
  AZURE_OPENAI_KEY?: string;
  VITE_AZURE_OPENAI_DEPLOYMENT?: string;
  AZURE_OPENAI_DEPLOYMENT?: string;
  VITE_AZURE_OPENAI_API_VERSION?: string;
  AZURE_OPENAI_API_VERSION?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}