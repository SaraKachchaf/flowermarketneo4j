import React from "react";
import "./Navbar.css";
import { useLocation, useNavigate } from "react-router-dom";
import { useCart } from "../context/CartContext";
import { LogOut, User, ShoppingCart } from "lucide-react";

const Navbar = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const { cartCount } = useCart();
  const token = localStorage.getItem("token");
  const role = localStorage.getItem("role");

  const handleLogout = () => {
    localStorage.clear();
    navigate("/login");
  };

  // ❌ pas de navbar dans l’admin
  if (location.pathname.startsWith("/admin")) {
    return null;
  }

  // ✅ scroll intelligent (home + sections)
  const scrollToSection = (id) => {
    if (location.pathname !== "/") {
      navigate("/");
      setTimeout(() => {
        document.getElementById(id)?.scrollIntoView({
          behavior: "smooth",
          block: "start",
        });
      }, 300);
    } else {
      document.getElementById(id)?.scrollIntoView({
        behavior: "smooth",
        block: "start",
      });
    }
  };

  return (
    <div className="navbar">
      {/* Logo / Accueil */}
      <div
        className="navbar-logo"
        onClick={() => navigate("/")}
        style={{ cursor: "pointer" }}
      >
        Accueil
      </div>

      {/* Menu */}
      <div className="navbar-menu">
        <span onClick={() => scrollToSection("produits")}>
          Produits
        </span>
        <span onClick={() => scrollToSection("promotions")}>
          Promotions
        </span>
      </div>

      {/* Actions */}
      <div className="navbar-actions">
        <div
          className="navbar-cart"
          onClick={() => navigate("/my-orders")}
          style={{ cursor: "pointer" }}
        >
          <ShoppingCart size={20} />
          {cartCount > 0 && <span className="cart-badge">{cartCount}</span>}
        </div>

        {token ? (
          <div className="navbar-user-actions">
            <button
              className="navbar-logout"
              onClick={handleLogout}
              title="Déconnexion"
            >
              <LogOut size={20} />
              <span>Quitter</span>
            </button>
          </div>
        ) : (
          <button
            className="navbar-login"
            onClick={() => navigate("/login")}
          >
            Connexion
          </button>
        )}
      </div>
    </div>
  );
};

export default Navbar;
