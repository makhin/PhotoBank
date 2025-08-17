import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { setAuthToken } from '@photobank/shared/auth';
import { useTranslation } from 'react-i18next';

export default function LogoutPage() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  useEffect(() => {
    setAuthToken(null, true);
    navigate('/login');
  }, [navigate]);

  return <p className="p-4">{t('loggingOutMsg')}</p>;
}
