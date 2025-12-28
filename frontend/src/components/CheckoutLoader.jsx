import React from 'react';
import './CheckoutLoader.css';

const CheckoutLoader = ({ message = "Traitement en cours..." }) => {
  return (
    <div className="checkout-loader-overlay">
      <div className="checkout-loader-container">
        <div className="loader-animation">
          <div className="flower-loader">
            <div className="petal petal-1">🌸</div>
            <div className="petal petal-2">🌸</div>
            <div className="petal petal-3">🌸</div>
            <div className="petal petal-4">🌸</div>
            <div className="petal petal-5">🌸</div>
            <div className="center">🌼</div>
          </div>
        </div>
        <div className="loader-message">{message}</div>
        <div className="loader-submessage">Veuillez patienter quelques instants</div>
      </div>
    </div>
  );
};

export default CheckoutLoader;