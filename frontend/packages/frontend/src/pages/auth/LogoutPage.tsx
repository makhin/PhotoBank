import {useEffect} from 'react';
import {useNavigate} from 'react-router-dom';
import { setAuthToken } from '@photobank/shared/auth';
import { loggingOutMsg } from '@photobank/shared/constants';

export default function LogoutPage() {
  const navigate = useNavigate();
  useEffect(() => {
    setAuthToken(null, true);
    navigate('/login');
  }, [navigate]);

  return <p className="p-4">{loggingOutMsg}</p>;
}
