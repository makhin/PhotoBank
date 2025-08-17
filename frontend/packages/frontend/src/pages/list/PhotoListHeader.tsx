import { memo } from 'react';
import { useTranslation } from 'react-i18next';

const PhotoListHeader = () => {
  const { t } = useTranslation();
  return (
    <div className="grid grid-cols-12 gap-4 mb-4 px-4 py-2 bg-muted/50 rounded-lg font-medium text-sm">
      <div className="col-span-1">{t('colIdLabel')}</div>
      <div className="col-span-2">{t('colPreviewLabel')}</div>
      <div className="col-span-2">{t('colNameLabel')}</div>
      <div className="col-span-1">{t('colDateLabel')}</div>
      <div className="col-span-2">{t('colStorageLabel')}</div>
      <div className="col-span-1">{t('colFlagsLabel')}</div>
      <div className="col-span-3">{t('colDetailsLabel')}</div>
    </div>
  );
};

export default memo(PhotoListHeader);
