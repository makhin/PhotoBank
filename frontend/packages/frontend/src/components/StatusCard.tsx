import { Card, CardContent } from '@/components/ui/card';
import { AlertTriangle } from 'lucide-react';
import { useAppSelector } from '@/app/hook';
import { botRunningText } from '@photobank/shared/constants';

export function StatusCard() {
  const { lastError } = useAppSelector((s) => (s as any).bot);
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
