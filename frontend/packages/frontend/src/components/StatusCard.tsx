import { AlertTriangle } from 'lucide-react';
import { botRunningText } from '@photobank/shared/constants';

import { Card, CardContent } from '@/shared/ui/card';
import { useAppSelector } from '@/app/hook';
import type { RootState } from '@/app/store.ts';

export function StatusCard() {
  const { lastError } = useAppSelector((s: RootState) => s.bot);
  return (
    <Card className="mx-auto mt-6 max-w-md">
      <CardContent>
        {lastError ? (
          <div className="flex items-center text-red-600">
            <AlertTriangle className="mr-2" /> {lastError}
          </div>
        ) : (
          <p className="text-green-700">{botRunningText}</p>
        )}
      </CardContent>
    </Card>
  );
}
