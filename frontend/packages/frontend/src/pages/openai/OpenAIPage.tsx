import { useEffect, useState } from 'react';
import { configureAzureOpenAI, parseQueryWithOpenAI } from '@photobank/shared/ai/openai';
import { useTranslation } from 'react-i18next';

import {
  AZURE_OPENAI_ENDPOINT,
  AZURE_OPENAI_KEY,
  AZURE_OPENAI_DEPLOYMENT,
  AZURE_OPENAI_API_VERSION,
} from '@/config';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card';
import { Textarea } from '@/shared/ui/textarea';
import { Button } from '@/shared/ui/button';

type ChatMessage = {
  role: 'user' | 'assistant';
  content: string;
};

export default function OpenAIPage() {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { t } = useTranslation();

  useEffect(() => {
    configureAzureOpenAI({
      endpoint: AZURE_OPENAI_ENDPOINT,
      apiKey: AZURE_OPENAI_KEY,
      deployment: AZURE_OPENAI_DEPLOYMENT,
      apiVersion: AZURE_OPENAI_API_VERSION,
    });
  }, []);

  const sendMessage = async () => {
    if (!input.trim()) return;
    const userMsg: ChatMessage = { role: 'user', content: input };
    const newMessages = [...messages, userMsg];
    setMessages(newMessages);
    setInput('');
    setLoading(true);
    setError(null);
    try {
      const reply = await parseQueryWithOpenAI(input);
      const aiMsg: ChatMessage = { role: 'assistant', content: JSON.stringify(reply, null, 2) };
      setMessages(() => [...newMessages, aiMsg]);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Unknown error';
      setError(message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="p-4 space-y-4">
      <Card className="w-full max-w-2xl mx-auto space-y-4">
        <CardHeader>
          <CardTitle>{t('openAiPageTitle')}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            {messages.map((m, i) => (
              <div key={i} className="whitespace-pre-wrap">
                <span className="font-bold mr-1">
                  {m.role === 'user' ? t('openAiUserPrefix') : t('openAiAssistantPrefix')}
                </span>
                {m.content}
              </div>
            ))}
            {error && <p className="text-destructive text-sm">{error}</p>}
          </div>
          <div className="flex items-start gap-2">
            <Textarea
              value={input}
              onChange={(e) => { setInput(e.target.value); }}
              placeholder={t('openAiPromptPlaceholder')}
              className="flex-1"
            />
            <Button onClick={() => void sendMessage()} disabled={loading}>
              {loading ? '...' : t('openAiSendButton')}
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
