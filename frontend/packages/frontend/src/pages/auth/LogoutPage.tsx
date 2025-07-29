import {useEffect} from 'react';
import {useNavigate} from 'react-router-dom';
import { clearAuthToken } from '@photobank/shared/auth';
import { loggingOutMsg } from '@photobank/shared/constants';

export default function LogoutPage() {
  const navigate = useNavigate();
  useEffect(() => {
    clearAuthToken();
    navigate('/login');
  }, [navigate]);

  return <p className="p-4">{loggingOutMsg}</p>;
}
