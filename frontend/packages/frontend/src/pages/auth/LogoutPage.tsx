import {useEffect} from 'react';
import {useNavigate} from 'react-router-dom';
import {logout} from '@photobank/shared/api';
import { loggingOutMsg } from '@photobank/shared/constants';

export default function LogoutPage() {
  const navigate = useNavigate();
  useEffect(() => {
    logout();
    navigate('/login');
  }, [navigate]);

  return <p className="p-4">{loggingOutMsg}</p>;
}
