import { useTranslation } from 'react-i18next';

import { Button } from '@/shared/ui/button';

export default function LanguageSwitcher() {
  const { i18n } = useTranslation();

  const toggle = () => {
    i18n.changeLanguage(i18n.language === 'en' ? 'ru' : 'en');
  };

  return (
    <Button variant="link" className="text-sm" onClick={toggle}>
      {i18n.language.toUpperCase()}
    </Button>
  );
}

