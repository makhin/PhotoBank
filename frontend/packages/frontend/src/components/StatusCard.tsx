import { AlertTriangle } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { Card, CardContent } from '@/shared/ui/card';
import { useAppSelector } from '@/app/hook';
import type { RootState } from '@/app/store';

export function StatusCard() {
  const { lastError } = useAppSelector((s: RootState) => s.bot);
  const { t } = useTranslation();
  return (
    <Card className="mx-auto mt-6 max-w-md">
      <CardContent>
        {lastError ? (
          <div className="flex items-center text-red-600">
            <AlertTriangle className="mr-2" /> {lastError}
          </div>
        ) : (
          <p className="text-green-700">{t('botRunningText')}</p>
        )}
      </CardContent>
    </Card>
  );
}
