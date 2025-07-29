import AzureOpenAI from 'openai';
import type { ChatCompletionMessageParam } from 'openai/resources/chat/completions/completions.mjs';

import { FEW_SHOTS, SYSTEM_PROMPT } from '@photobank/shared/ai/constants';

import type { PhotoFilter } from './filter';
import { PhotoFilterSchema, photoFilterSchemaForLLM } from './filter';

let client: AzureOpenAI | null = null;

export function configureAzureOpenAI(options: { endpoint:string , apiKey:string, deployment:string, apiVersion:string }): void {
  client = new AzureOpenAI({...options, dangerouslyAllowBrowser: true});
}

export async function createChatCompletion(text: string): Promise<string> {
  if (!client) {
      throw new Error('Azure OpenAI is not configured');
  }

  const response = await client.chat.completions.create({
    messages: [{role: 'user', content: text }],
    temperature: 1,
    top_p: 1,
    model: 'gpt-4o',
    response_format: {
      type: 'text',
    },
  });

  return  response.choices[0].message.content ?? '';
}

export async function parseQueryWithOpenAI(text: string): Promise<PhotoFilter> {
  if (!client) {
      throw new Error('Azure OpenAI is not configured');
  }

  const messages: Array<ChatCompletionMessageParam> = [
    { role: 'system', content: SYSTEM_PROMPT },
    ...FEW_SHOTS,
    { role: 'user', content: text },
  ];

  const response = await client.chat.completions.create({
    messages: messages,
    temperature: 1,
    top_p: 1,
    model: 'gpt-4o',
    response_format: {
      type: 'json_schema',
      json_schema: {
        name: 'PhotoFilter',
        schema: photoFilterSchemaForLLM,
        strict: true,
      },
    },
  });

  const content = response.choices[0].message.content ?? '{}';

  console.log(content);

  return PhotoFilterSchema.parse(JSON.parse(content));
}
