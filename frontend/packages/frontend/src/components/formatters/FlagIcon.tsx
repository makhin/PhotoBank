import { Check, X } from 'lucide-react';
import { memo } from 'react';

interface FlagIconProps {
  value: boolean | null | undefined;
}

const FlagIcon = ({ value }: FlagIconProps) => {
  const Icon = value ? Check : X;
  const label = value ? 'Yes' : 'No';
  return <Icon aria-label={label} className="w-4 h-4" />;
};

export default memo(FlagIcon);
