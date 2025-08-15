import type { PhotoItemDto } from '@photobank/shared/api/photobank';
import { formatDate } from '@photobank/shared/format';
import {
  colDateLabel,
  colFlagsLabel,
  colIdLabel,
  colNameLabel,
  colPreviewLabel,
  colStorageLabel,
  colDetailsLabel,
} from '@photobank/shared/constants';
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
    header: colIdLabel,
    width: 'col-span-1',
    render: (r) => r.id,
  },
  {
    id: 'preview',
    header: colPreviewLabel,
    width: 'col-span-2',
    render: (r) => (
      <PreviewCell thumbnail={r.thumbnail} alt={r.name} className="w-16 h-16" />
    ),
  },
  {
    id: 'name',
    header: colNameLabel,
    width: 'col-span-2',
    render: (r) => <span className="truncate">{r.name}</span>,
  },
  {
    id: 'date',
    header: colDateLabel,
    width: 'col-span-1',
    render: (r) => formatDate(r.takenDate),
  },
  {
    id: 'storage',
    header: colStorageLabel,
    width: 'col-span-2',
    render: (r) => r.storageName,
  },
  {
    id: 'flags',
    header: colFlagsLabel,
    width: 'col-span-1',
    render: (r) => <FlagIcon value={r.isBW} />,
  },
  {
    id: 'details',
    header: colDetailsLabel,
    width: 'col-span-3',
    render: (r) => <RowActions id={r.id} />,
  },
];
