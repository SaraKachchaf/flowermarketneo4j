import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import axios from '../api/axios';
import './Register.css';

const Register = () => {
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const response = await axios.post('/Auth/register', {
        fullName: fullName,
        email,
        password,
      });

      if (response.data.requiresEmailVerification === false) {
        navigate('/login');
      } else {
        localStorage.setItem('pendingVerificationEmail', email);
        navigate(`/verify-email?email=${encodeURIComponent(email)}`);
      }
    } catch (err) {
      if (err.response?.data?.errors) {
        setError(Object.values(err.response.data.errors).join(', '));
      } else {
        setError(err.response?.data?.message || 'Une erreur est survenue');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="register-page">
      <div className="register-container">
        <div className="register-header">
          <h1>Flower Market</h1>
          <h2>Créer un compte</h2>
          <p>Rejoignez notre communauté florale</p>
          <p className="verification-note">
            Un code de vérification sera envoyé à votre email après l'inscription
          </p>
        </div>

        {error && (
          <div className="error-message">
            {error}
          </div>
        )}

        <form className="register-form" onSubmit={handleSubmit}>
          <div className="input-group">
            <label htmlFor="fullName">Nom complet</label>
            <input
              id="fullName"
              type="text"
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              placeholder="Votre nom complet"
              required
            />
          </div>

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
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              required
            />
          </div>

          <button
            type="submit"
            className={`register-btn ${loading ? 'loading' : ''}`}
            disabled={loading}
          >
            {loading ? 'Inscription...' : 'S\'inscrire'}
          </button>
        </form>

        <div className="register-footer">
          <p>
            Déjà un compte ? <Link to="/login">Se connecter</Link>
          </p>
          <p>
            Prestataire ? <Link to="/register-prestataire">S'inscrire comme prestataire</Link>
          </p>
          <Link to="/" className="back-home">← Retour à l'accueil</Link>
        </div>
      </div>
    </div>
  );
};

export default Register;