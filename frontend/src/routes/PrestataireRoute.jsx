import { Navigate } from "react-router-dom";

const PrestataireRoute = ({ children }) => {
  const role = localStorage.getItem("role");
  const isApproved = localStorage.getItem("isApproved") === "true";

  if (role !== "Prestataire") {
    return <Navigate to="/login" />;
  }

  if (!isApproved) {
    return <Navigate to="/login" />;
  }

  return children;
};

export default PrestataireRoute;
