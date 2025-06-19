import { useParams } from "react-router-dom";

export function ProfilePage() {
  const { userId } = useParams();
  return <h1 className="text-xl">Profile of user {userId}</h1>;
}
