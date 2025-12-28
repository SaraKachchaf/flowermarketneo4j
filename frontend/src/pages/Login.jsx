import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { Eye, EyeOff } from 'lucide-react';
import api from '../api/axios';
import './Login.css';
import alerts from '../utils/alerts';

const Login = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);

  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setSuccess('');
    setLoading(true);

    try {
      const res = await api.post("/Auth/login", {
        email,
        password,
      });

      console.log("Login response:", res.data);

      const { token, requires2FA, tempToken, user } = res.data;
      const { role, fullName, isApproved } = user || {};

      /* ===============================
         🔐 2FA REQUIRED
      =============================== */
      if (requires2FA) {
        localStorage.setItem("pending2FAEmail", email);
        localStorage.setItem("temp2FAToken", tempToken);

        navigate("/verify-2fa");
        return;
      }

      /* ===============================
         ✅ LOGIN NORMAL
      =============================== */
      localStorage.clear();
      localStorage.setItem("token", token);
      localStorage.setItem("role", role);
      localStorage.setItem("fullName", fullName);
      localStorage.setItem("isApproved", String(isApproved));

      if (role === "Admin") {
        navigate("/admin-dashboard");
        return;
      }

      if (role === "Prestataire") {
        if (!isApproved) {
          alerts.error(
            "Validation en attente",
            "Votre compte est en attente de validation par l’admin."
          );
          return;
        }
        navigate("/prestataire/dashboard");
        return;
      }

      navigate("/");

    } catch (err) {
      setError(
        err.response?.data?.error ||
        err.response?.data?.message ||
        "Email ou mot de passe incorrect"
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-page">
      <div className="login-container">
        <div className="login-header">
          <h1>Flower Market</h1>
          <h2>Connexion</h2>
          <p>Connectez-vous à votre compte</p>
        </div>

        {/* ❌ ERREUR */}
        {error && <div className="error-message">{error}</div>}

        {/* ✅ SUCCÈS */}
        {success && <div className="success-message">{success}</div>}

        <form className="login-form" onSubmit={handleSubmit}>
          <div className="input-group">
            <label htmlFor="email">Email</label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="votre@email.com"
              required
            />
          </div>

          <div className="input-group">
            <label htmlFor="password">Mot de passe</label>
            <div className="password-input-wrapper">
              <input
                id="password"
                type={showPassword ? "text" : "password"}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                required
              />
              <button
                type="button"
                className="password-toggle-btn"
                onClick={() => setShowPassword(!showPassword)}
                tabIndex="-1"
              >
                {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
              </button>
            </div>
          </div>

          <button
            type="submit"
            className={`login-btn ${loading ? 'loading' : ''}`}
            disabled={loading}
          >
            {loading ? 'Connexion...' : 'Se connecter'}
          </button>
        </form>

        <div className="login-footer">
          <p>
            Pas de compte ? <Link to="/register">Créer un compte</Link>
          </p>
          <p>
            Prestataire ?{' '}
            <Link to="/register-prestataire">
              S'inscrire comme prestataire
            </Link>
          </p>
          <Link to="/" className="back-home">
            ← Retour à l'accueil
          </Link>
        </div>
      </div>
    </div>
  );
};

export default Login;
