import { useTranslation } from 'react-i18next';

import { API_BASE_URL } from '@/config';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card';
import { StatusCard } from '@/components/StatusCard';

export default function ServiceInfoPage() {
  const { t } = useTranslation();
  const info = {
    mode: import.meta.env.MODE,
    apiBaseUrl: API_BASE_URL,
    userAgent: typeof navigator !== 'undefined' ? navigator.userAgent : 'N/A',
  } as const;

  return (
    <div className="p-4 space-y-4">
      <Card className="w-full max-w-md mx-auto">
        <CardHeader>
          <CardTitle>{t('serviceInfoTitle')}</CardTitle>
        </CardHeader>
        <CardContent>
          <ul className="list-disc list-inside space-y-1">
            {Object.entries(info).map(([key, value]) => (
              <li key={key}>
                <span className="font-medium">{key}: </span>
                <span className="break-all">{String(value)}</span>
              </li>
            ))}
          </ul>
        </CardContent>
      </Card>
      <StatusCard />
    </div>
  );
}
