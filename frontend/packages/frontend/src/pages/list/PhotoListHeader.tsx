import { memo } from 'react';
import {
  colDateLabel,
  colFlagsLabel,
  colIdLabel,
  colNameLabel,
  colPreviewLabel,
  colStorageLabel,
  colDetailsLabel,
} from '@photobank/shared/constants';

const PhotoListHeader = () => (
  <div className="grid grid-cols-12 gap-4 mb-4 px-4 py-2 bg-muted/50 rounded-lg font-medium text-sm">
    <div className="col-span-1">{colIdLabel}</div>
    <div className="col-span-2">{colPreviewLabel}</div>
    <div className="col-span-2">{colNameLabel}</div>
    <div className="col-span-1">{colDateLabel}</div>
    <div className="col-span-2">{colStorageLabel}</div>
    <div className="col-span-1">{colFlagsLabel}</div>
    <div className="col-span-3">{colDetailsLabel}</div>
  </div>
);

export default memo(PhotoListHeader);
