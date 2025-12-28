// components/LoadingSpinner.jsx
import React from 'react';
import './LoadingSpinner.css';

const LoadingSpinner = ({ size = 'medium', fullPage = false }) => {
  const sizeClass = `spinner-${size}`;
  const containerClass = fullPage ? 'full-page-loading' : 'loading-container';
  
  return (
    <div className={containerClass}>
      <div className={`loading-spinner ${sizeClass}`}>
        <div className="spinner"></div>
        {fullPage && <p className="loading-text">Chargement...</p>}
      </div>
    </div>
  );
};

export default LoadingSpinner;