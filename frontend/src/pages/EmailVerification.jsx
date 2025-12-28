import { useState, useEffect } from "react";
import { useLocation, useNavigate, Link } from "react-router-dom";
import axios from "../api/axios";
import './EmailVerification.css';
import alerts from "../utils/alerts";

export default function EmailVerification() {
  const location = useLocation();
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [code, setCode] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);
  const [resendLoading, setResendLoading] = useState(false);
  const [countdown, setCountdown] = useState(0);

  // Récupérer l'email depuis l'URL ou localStorage
  useEffect(() => {
    const searchParams = new URLSearchParams(location.search);
    const emailFromUrl = searchParams.get("email");
    const storedEmail = localStorage.getItem("pendingVerificationEmail");

    const finalEmail = emailFromUrl || storedEmail || "";
    setEmail(finalEmail);

    // Démarrer un compte à rebours pour le renvoi
    if (localStorage.getItem("lastResendTime")) {
      const lastTime = parseInt(localStorage.getItem("lastResendTime"));
      const diff = Math.floor((Date.now() - lastTime) / 1000);
      if (diff < 60) setCountdown(60 - diff);
    }
  }, [location]);

  // Compte à rebours
  useEffect(() => {
    if (countdown > 0) {
      const timer = setTimeout(() => setCountdown(countdown - 1), 1000);
      return () => clearTimeout(timer);
    }
  }, [countdown]);

  const handleVerify = async (e) => {
    e.preventDefault();
    if (!code || code.length !== 6) {
      setError("Veuillez entrer un code à 6 chiffres");
      return;
    }

    try {
      setLoading(true);
      setError("");

      const res = await axios.post("/Auth/verify-email", {
        email,
        code,
      });

      setSuccess(true);
      localStorage.removeItem("pendingVerificationEmail");
      localStorage.removeItem("lastResendTime");

      // Rediriger après 2 secondes
      setTimeout(() => {
        navigate("/login", {
          state: { message: "Email vérifié avec succès ! Vous pouvez maintenant vous connecter." }
        });
      }, 2000);

    } catch (err) {
      setError(err.response?.data?.message || "Code invalide ou expiré");
    } finally {
      setLoading(false);
    }
  };

  const handleResendCode = async () => {
    if (countdown > 0) return;

    try {
      setResendLoading(true);
      setError("");

      await axios.post("/Auth/send-verification", { email });

      localStorage.setItem("lastResendTime", Date.now().toString());
      setCountdown(60);

      alerts.success("Code envoyé !", "Un nouveau code a été envoyé à votre email.");
    } catch (err) {
      setError(err.response?.data?.message || "Erreur lors de l'envoi du code");
    } finally {
      setResendLoading(false);
    }
  };

  return (
    <div className="email-verification-page">
      <div className="email-verification-container">
        <div className="email-verification-header">
          <h1>Flower Market</h1>
          <h2>Vérification d'email</h2>
          <p>Nous avons envoyé un code à 6 chiffres à <strong>{email}</strong></p>
        </div>

        {success ? (
          <div className="success-message">
            ✅ Email vérifié avec succès ! Redirection vers la page de connexion...
          </div>
        ) : (
          <>
            {error && (
              <div className="error-message">
                {error}
              </div>
            )}

            <form className="email-verification-form" onSubmit={handleVerify}>
              <div className="input-group">
                <label htmlFor="verification-code">Code de vérification</label>
                <input
                  id="verification-code"
                  type="text"
                  value={code}
                  onChange={(e) => {
                    const value = e.target.value.replace(/\D/g, '');
                    if (value.length <= 6) setCode(value);
                  }}
                  required
                  placeholder="xxxxxx"
                  maxLength={6}
                  className="code-input"
                />
                <div className="code-hint">
                  Entrez le code à 6 chiffres reçu par email
                </div>
              </div>

              <button
                type="submit"
                className={`verify-btn ${loading ? 'loading' : ''}`}
                disabled={loading || !code || code.length !== 6}
              >
                {loading ? 'Vérification...' : 'Vérifier le code'}
              </button>
            </form>

            <div className="resend-container">
              <button
                onClick={handleResendCode}
                disabled={resendLoading || countdown > 0}
                className="resend-btn"
              >
                {resendLoading ? 'Envoi...' :
                  countdown > 0 ? `Renvoyer (${countdown}s)` :
                    "Je n'ai pas reçu de code"}
              </button>
            </div>
          </>
        )}

        <div className="email-verification-footer">
          <Link to="/login" className="back-login">
            ← Retour à la connexion
          </Link>
        </div>
      </div>
    </div>
  );
}