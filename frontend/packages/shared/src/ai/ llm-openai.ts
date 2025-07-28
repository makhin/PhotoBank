import AzureOpenAI from 'openai';
import zodToJsonSchema from 'zod-to-json-schema';
import { ChatCompletionMessageParam } from 'openai/src/resources/chat/completions/completions';

import { FEW_SHOTS, SYSTEM_PROMPT } from '@photobank/shared/ai/constants';

import { PhotoFilter, PhotoFilterSchema } from './filter';

const endpoint = 'https://photobankopenai.openai.azure.com/';
const modelName = 'gpt-4o';
const deployment = 'gpt-4o';
const apiKey = '<your-api-key>';
const apiVersion = '2024-04-01-preview';

const options = { endpoint, apiKey, deployment, apiVersion };

const client = new AzureOpenAI(options);

export async function parseQueryWithOpenAI(text: string): Promise<PhotoFilter> {
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
    model: modelName,
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
  console.log(content);

  return PhotoFilterSchema.parse(JSON.parse(content));
}
