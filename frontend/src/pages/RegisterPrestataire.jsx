import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import api from '../api/axios';
import './RegisterPrestataire.css';

const RegisterPrestataire = () => {
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [phone, setPhone] = useState('');
  const [address, setAddress] = useState('');
  const [description, setDescription] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setSuccess('');
    setLoading(true);

    try {
      const response = await api.post('/Auth/register-prestataire', {
        FullName: fullName,
        email,
        password,
        phone,
        address,
        description,
      });

      setSuccess('Demande de compte prestataire envoyée ! Votre compte est en attente de validation par un administrateur.');
      
      // Redirection vers la page de login après 3 secondes
      setTimeout(() => {
        navigate('/login');
      }, 3000);
    } catch (err) {
      if (err.response?.data?.errors) {
        setError(Object.values(err.response.data.errors).join(', '));
      } else {
        setError(err.response?.data?.message || 'Erreur lors de la création du compte prestataire');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="register-prestataire-page">
      <div className="register-prestataire-container">
        <div className="register-prestataire-header">
          <h1>Flower Market</h1>
          <h2>Compte Prestataire</h2>
          <p>Rejoignez notre réseau de professionnels</p>
        </div>

        {/* Message d'information */}
        <div style={{
          background: 'linear-gradient(135deg, #e0f2fe 0%, #b3e5fc 100%)',
          color: '#0277bd',
          padding: '15px',
          borderRadius: '12px',
          marginBottom: '20px',
          border: '1px solid rgba(2, 119, 189, 0.2)',
          fontSize: '14px',
          lineHeight: '1.5'
        }}>
          <strong>⚠️ Important :</strong> Votre compte prestataire nécessite une validation par un administrateur. 
          Vous pourrez vous connecter après approbation.
        </div>

        {error && (
          <div className="error-message">
            {error}
          </div>
        )}

        {success && (
          <div className="success-message">
            {success}
          </div>
        )}

        <form className="register-prestataire-form" onSubmit={handleSubmit}>
          <div className="form-row">
            <div className="input-group">
              <label htmlFor="fullName">Nom de l'entreprise</label>
              <input
                id="fullName"
                type="text"
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                placeholder="Nom de votre entreprise"
                required
              />
            </div>
          </div>
          <div className="input-group">
            <label htmlFor="email">Email professionnel</label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="contact@votre-entreprise.com"
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

          <div className="input-group">
            <label htmlFor="address">Adresse</label>
            <input
              id="address"
              type="text"
              value={address}
              onChange={(e) => setAddress(e.target.value)}
              placeholder="Adresse de votre entreprise"
            />
          </div>
          <button 
            type="submit" 
            className={`register-prestataire-btn ${loading ? 'loading' : ''}`}
            disabled={loading}
          >
            {loading ? 'Envoi en cours...' : 'Soumettre la demande'}
          </button>
        </form>

        <div className="register-prestataire-footer">
          <p>
            Client ? <Link to="/register">Créer un compte standard</Link>
          </p>
          <p>
            Déjà un compte ? <Link to="/login">Se connecter</Link>
          </p>
          <Link to="/" className="back-home">← Retour à l'accueil</Link>
        </div>
      </div>
    </div>
  );
};

export default RegisterPrestataire;