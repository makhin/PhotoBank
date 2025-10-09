import { AzureOpenAI } from 'openai';
import type { ChatCompletionMessageParam } from 'openai/resources';

import { FEW_SHOTS, SYSTEM_PROMPT } from './constants';
import type { PhotoFilter } from './filter';
import { PhotoFilterSchema, photoFilterSchemaForLLM } from './filter';
import { logger } from '../utils/logger';

type AzureOpenAIOptions = {
  endpoint: string;
  apiKey: string;
  deployment: string;
  apiVersion: string;
};

let client: AzureOpenAI | null = null;
let clientOptions: AzureOpenAIOptions | null = null;

export function configureAzureOpenAI(options: AzureOpenAIOptions): void {
  client = new AzureOpenAI(options);
  clientOptions = options;
}

function ensureConfigured(): { client: AzureOpenAI; deployment: string } {
  if (!client) {
    throw new Error('Azure OpenAI is not configured');
  }

  const deployment = clientOptions?.deployment;

  if (!deployment) {
    throw new Error('Azure OpenAI deployment is not configured');
  }

  return { client, deployment };
}

type AzureChatCompletionsCreateParams = Parameters<
  AzureOpenAI['chat']['completions']['create']
>[0];

type ChatCompletionCreateParamsWithoutModel = Omit<
  AzureChatCompletionsCreateParams,
  'model'
>;

type ChatCompletionCreateResult = Awaited<
  ReturnType<AzureOpenAI['chat']['completions']['create']>
>;

type NonStreamingChatCompletion = Extract<
  ChatCompletionCreateResult,
  { choices: unknown }
>;

async function runChatCompletion(
  params: ChatCompletionCreateParamsWithoutModel,
): Promise<NonStreamingChatCompletion> {
  const { client, deployment } = ensureConfigured();

  try {
    const response = await client.chat.completions.create({
      ...params,
      stream: false,
      model: deployment,
    });

    if ('choices' in response) {
      return response;
    }

    throw new Error('Streaming responses are not supported for this call');
  } catch (err) {
    logger.error(err);
    throw new Error('OpenAI request failed');
  }
}

export async function createChatCompletion(text: string): Promise<string> {
  const response = await runChatCompletion({
    messages: [{ role: 'user', content: text }],
    temperature: 1,
    top_p: 1,
    response_format: {
      type: 'text',
    },
  });

  return response.choices?.[0]?.message?.content ?? '';
}

export async function parseQueryWithOpenAI(text: string): Promise<PhotoFilter> {
  const messages: Array<ChatCompletionMessageParam> = [
    { role: 'system', content: SYSTEM_PROMPT },
    ...FEW_SHOTS,
    { role: 'user', content: text },
  ];

  const response = await runChatCompletion({
    messages: messages,
    temperature: 1,
    top_p: 1,
    response_format: {
      type: 'json_schema',
      json_schema: {
        name: 'PhotoFilter',
        schema: photoFilterSchemaForLLM,
        strict: true,
      },
    },
  });

  const content = response.choices?.[0]?.message?.content ?? '{}';

  return PhotoFilterSchema.parse(JSON.parse(content));
}
