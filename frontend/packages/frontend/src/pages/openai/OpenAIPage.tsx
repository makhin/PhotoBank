import { useState } from 'react';
import {
  openAiPageTitle,
  openAiSendButton,
  openAiPromptPlaceholder,
} from '@photobank/shared/constants';
import type { ChatMessage } from '@photobank/shared/ai/openai';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Textarea } from '@/components/ui/textarea';
import { Button } from '@/components/ui/button';

export default function OpenAIPage() {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const sendMessage = () => {
    if (!input.trim()) return;
    const userMsg: ChatMessage = { role: 'user', content: input };
    const newMessages = [...messages, userMsg];
    setMessages(newMessages);
    setInput('');
    setLoading(true);
    setError(null);
    setLoading(false);
  };

  return (
    <div className="p-4 space-y-4">
      <Card className="w-full max-w-2xl mx-auto space-y-4">
        <CardHeader>
          <CardTitle>{openAiPageTitle}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            {messages.map((m, i) => (
              <div key={i} className="whitespace-pre-wrap">
                <span className="font-bold mr-1">
                  {m.role === 'user' ? 'You:' : 'AI:'}
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
              placeholder={openAiPromptPlaceholder}
              className="flex-1"
            />
            <Button onClick={sendMessage} disabled={loading}>
              {loading ? '...' : openAiSendButton}
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}