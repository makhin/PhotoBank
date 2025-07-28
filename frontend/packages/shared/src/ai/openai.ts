import AzureOpenAI from 'openai';
import axios from 'axios';
import zodToJsonSchema from 'zod-to-json-schema';
import { ChatCompletionMessageParam } from 'openai/src/resources/chat/completions/completions';

import { FEW_SHOTS, SYSTEM_PROMPT } from '@photobank/shared/ai/constants';

import { PhotoFilter, PhotoFilterSchema } from './filter';

export interface AzureOpenAIConfig {
  endpoint: string; // e.g. https://my-resource.openai.azure.com
  apiKey: string;
  deployment: string;
  apiVersion?: string;
}

const DEFAULT_API_VERSION = '2024-04-01-preview';

let config: AzureOpenAIConfig | null = null;
let client: AzureOpenAI | null = null;

export function configureAzureOpenAI(cfg: AzureOpenAIConfig): void {
  config = { ...cfg, apiVersion: cfg.apiVersion ?? DEFAULT_API_VERSION };
  client = new AzureOpenAI({
    endpoint: config.endpoint,
    apiKey: config.apiKey,
    deployment: config.deployment,
    apiVersion: config.apiVersion,
  });
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

export async function parseQueryWithOpenAI(text: string): Promise<PhotoFilter> {
  if (!client) {
    if (!config) {
      throw new Error('Azure OpenAI is not configured');
    }
    client = new AzureOpenAI({
      endpoint: config.endpoint,
      apiKey: config.apiKey,
      deployment: config.deployment,
      apiVersion: config.apiVersion,
    });
  }

  const fullSchema = zodToJsonSchema(PhotoFilterSchema, 'PhotoFilter');

  const messages: Array<ChatCompletionMessageParam> = [
    { role: 'system', content: SYSTEM_PROMPT },
    ...FEW_SHOTS,
    { role: 'user', content: text },
  ];

  const response = await client.chat.completions.create({
    messages: messages,
    temperature: 1,
    top_p: 1,
    model: config?.deployment || 'gpt-4o',
    response_format: {
      type: 'json_schema',
      json_schema: {
        name: 'PhotoFilter',
        schema: fullSchema.definitions?.PhotoFilter || fullSchema,
        strict: true,
      },
    },
  });

  const content = response.choices[0].message.content ?? '{}';

  return PhotoFilterSchema.parse(JSON.parse(content));
}
