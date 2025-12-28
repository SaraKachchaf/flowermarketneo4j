import { BrowserRouter as Router, Routes, Route, Navigate, useLocation } from 'react-router-dom';
import Navbar from './components/Navbar';

import HomePage from './pages/HomePage';
import Login from './pages/Login';
import Register from './pages/Register';
import RegisterPrestataire from './pages/RegisterPrestataire';
import AdminDashboard from './pages/AdminDashboard';
import './App.css';
import PrestataireRoute from "./routes/PrestataireRoute";
import PrestataireDashboard from "./pages/PrestataireDashboard";
import MyOrders from './pages/MyOrders';
import EmailVerification from './pages/EmailVerification';



const hideNavbarRoutes = [
  '/login',
  '/register',
  '/register-prestataire',
  '/admin-dashboard',
  '/prestataire/dashboard'
  , '/verify-email'
];

function AppContent() {
  const location = useLocation();
  const shouldHideNavbar = hideNavbarRoutes.includes(location.pathname);

  return (
    <>
      {!shouldHideNavbar && <Navbar />}
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
        <Route path="/register-prestataire" element={<RegisterPrestataire />} />
        <Route path="/admin-dashboard" element={<AdminDashboard />} />
        <Route path="/verify-email" element={<EmailVerification />} />
        <Route path="*" element={<Navigate to="/" />} />
        <Route path="/my-orders" element={<MyOrders />} />
        <Route path="/prestataire/dashboard" element={<PrestataireDashboard />} />
        <Route
          path="/prestataire/dashboard"
          element={
            <PrestataireRoute>
              <PrestataireDashboard />
            </PrestataireRoute>
          }
        />

      </Routes>
    </>
  );
}

import { CartProvider } from './context/CartContext';

function App() {
  return (
    <Router>
      <CartProvider>
        <AppContent />
      </CartProvider>
    </Router>
  );
}

export default App;
