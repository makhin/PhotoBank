import type { PhotoItemDto } from '@photobank/shared/api/photobank';
import { formatDate } from '@photobank/shared/format';

import i18n from '@/shared/config/i18n';
import FlagIcon from '@/components/formatters/FlagIcon';

import PreviewCell from './PreviewCell';
import RowActions from './RowActions';

export interface Column<T> {
  id: string;
  header: string;
  width?: string;
  render: (row: T) => React.ReactNode;
  sortAccessor?: (row: T) => string | number;
  hide?: boolean;
}

export const photoColumns: Column<PhotoItemDto>[] = [
  {
    id: 'id',
    header: i18n.t('colIdLabel'),
    width: 'col-span-1',
    render: (r) => r.id,
  },
  {
    id: 'preview',
    header: i18n.t('colPreviewLabel'),
    width: 'col-span-2',
    render: (r) => (
      <PreviewCell thumbnailUrl={r.thumbnailUrl ?? ''} alt={r.name} className="w-16 h-16" />
    ),
  },
  {
    id: 'name',
    header: i18n.t('colNameLabel'),
    width: 'col-span-2',
    render: (r) => <span className="truncate">{r.name}</span>,
  },
  {
    id: 'date',
    header: i18n.t('colDateLabel'),
    width: 'col-span-1',
    render: (r) => formatDate(r.takenDate ?? ''),
  },
  {
    id: 'storage',
    header: i18n.t('colStorageLabel'),
    width: 'col-span-2',
    render: (r) => r.storageName,
  },
  {
    id: 'flags',
    header: i18n.t('colFlagsLabel'),
    width: 'col-span-1',
    render: (r) => <FlagIcon value={r.isBW} />,
  },
  {
    id: 'details',
    header: i18n.t('colDetailsLabel'),
    width: 'col-span-3',
    render: (r) => <RowActions id={r.id} />,
  },
];
