import { Navigate } from "react-router-dom";

const ProtectedRoute = ({ children, requiredRole }) => {
  const token = localStorage.getItem("token");
  const userRole = localStorage.getItem("role");

  // Si pas de token, rediriger vers login
  if (!token) {
    return <Navigate to="/login" replace />;
  }

  // Si rôle requis mais pas le bon
  if (requiredRole && userRole !== requiredRole) {
    // Rediriger selon le rôle
    if (userRole === "Prestataire") {
      return <Navigate to="/dashboard" replace />;
    } else if (userRole === "Admin") {
      return <Navigate to="/admin" replace />;
    } else {
      return <Navigate to="/" replace />;
    }
  }

  return children;
};

export default ProtectedRoute;