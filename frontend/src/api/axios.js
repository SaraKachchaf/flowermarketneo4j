// src/api/axios.js
import axios from 'axios';



// Créer l'instance Axios avec la baseURL
const api = axios.create({
  baseURL: 'https://localhost:44302/api', // Your backend URL
  timeout: 10000, // 10 secondes
  headers: {
  },
});


// Ajouter automatiquement le token dans les headers
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Gérer les erreurs globales
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response) {
      // Erreurs HTTP
      switch (error.response.status) {
        case 401:
          // Si le flag _noRedirect est présent (ex: sync panier arrière-plan), on ne redirige pas
          if (error.config?._noRedirect) {
            return Promise.reject(error);
          }

          if (window.location.pathname !== "/login") {
            localStorage.removeItem("token");
            localStorage.removeItem("role");
            window.location.href = "/login";
          }
          break;

          break;
        case 403:
          console.warn('Accès refusé');
          break;
        case 404:
          console.warn('Ressource non trouvée');
          break;
        case 500:
          console.error('Erreur serveur');
          break;
        default:
          console.error('Erreur:', error.response.status);
      }
    } else if (error.request) {
      // Pas de réponse du serveur
      console.error('Pas de réponse du serveur');
    } else {
      // Erreur de configuration
      console.error('Erreur de configuration:', error.message);
    }

    return Promise.reject(error);
  }
);
export default api;