import {useEffect} from 'react';
import {useNavigate} from 'react-router-dom';
import {logout} from '@photobank/shared/api';

export default function LogoutPage() {
  const navigate = useNavigate();
  useEffect(() => {
    logout();
    navigate('/login');
  }, [navigate]);

  return <p className="p-4">Logging out...</p>;
}
