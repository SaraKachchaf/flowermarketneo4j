
import { Link, useNavigate } from "react-router-dom";
import "../styles/Sidebar.css";

export default function Sidebar() {
  const navigate = useNavigate();

  const handleLogout = () => {
    localStorage.removeItem("token");
    localStorage.removeItem("role");
    localStorage.removeItem("userEmail");
    navigate("/login");
  };

  return (
    <div className="sidebar">
      <h1 className="logo">FlowerMarket</h1>
      
      <div className="sidebar-menu">
        <Link to="/dashboard" className="side-btn">
          <span className="icon"></span> Dashboard
        </Link>
        
        <Link to="/produits" className="side-btn">
          <span className="icon"></span> Produits
        </Link>
        
        <Link to="/promotions" className="side-btn">
          <span className="icon"></span> Promotions
        </Link>
        
        <Link to="/commandes" className="side-btn">
          <span className="icon"></span> Commandes
        </Link>
        
      </div>
      
      <div className="sidebar-footer">
        <button onClick={handleLogout} className="logout-btn">
          <span className="icon"></span> Déconnexion
        </button>
      </div>
    </div>
  );
}