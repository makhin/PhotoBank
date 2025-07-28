import axios from 'axios';

export interface AzureOpenAIConfig {
  endpoint: string;
  apiKey: string;
  deployment: string;
  apiVersion: string;
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

let config: AzureOpenAIConfig | null = null;

export function configureAzureOpenAI(cfg: AzureOpenAIConfig): void {
  config = { ...cfg };
}

export async function createChatCompletion(
  request: ChatCompletionRequest,
): Promise<ChatCompletionResponse> {
  if (!config) {
    throw new Error('Azure OpenAI is not configured');
  }
  const { endpoint, deployment, apiKey, apiVersion } = config;
  const url = `${endpoint}/openai/deployments/${deployment}/chat/completions?api-version=${apiVersion}`;

  const res = await axios.post(url, request, {
    headers: {
      'api-key': apiKey,
    },
  });
  return res.data as ChatCompletionResponse;
}

