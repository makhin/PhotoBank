import axios from 'axios';

export interface AzureOpenAIConfig {
  endpoint: string; // e.g. https://my-resource.openai.azure.com
  apiKey: string;
  deployment: string;
  apiVersion?: string;
}

const DEFAULT_API_VERSION = '2024-02-15-preview';

let config: AzureOpenAIConfig | null = null;

export function configureAzureOpenAI(cfg: AzureOpenAIConfig): void {
  config = { ...cfg, apiVersion: cfg.apiVersion ?? DEFAULT_API_VERSION };
}

export interface ChatMessage {
  role: 'system' | 'user' | 'assistant';
  content: string;
}

export interface ChatCompletionRequest {
  messages: ChatMessage[];
  temperature?: number;
  max_tokens?: number;
}

export interface ChatCompletionResponse {
  id: string;
  choices: Array<{ message: ChatMessage }>;
  [key: string]: unknown;
}

export async function createChatCompletion(
  request: ChatCompletionRequest,
): Promise<ChatCompletionResponse> {
  if (!config) {
    throw new Error('Azure OpenAI is not configured');
  }
  const { endpoint, deployment, apiKey, apiVersion = DEFAULT_API_VERSION } = config;
  const url = `${endpoint}/openai/deployments/${deployment}/chat/completions?api-version=${apiVersion}`;
  const res = await axios.post(url, request, {
    headers: {
      'api-key': apiKey,
    },
  });
  return res.data as ChatCompletionResponse;
}

