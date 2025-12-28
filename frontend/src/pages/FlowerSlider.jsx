import React, { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import './FlowerSlider.css';
const API_BASE = "https://localhost:44302";

const FlowerSlider = ({ products = [], onOrder, formatCurrency, getImageUrl }) => {
  const [hoveredIndex, setHoveredIndex] = useState(null);
  const [isAutoPlaying, setIsAutoPlaying] = useState(true);
  const [isDragging, setIsDragging] = useState(false);
  const [startX, setStartX] = useState(0);
  const [scrollLeft, setScrollLeft] = useState(0);
  const sliderRef = useRef(null);
  const navigate = useNavigate();

  // Configuration de l'auto-scroll horizontal
  useEffect(() => {
    let interval;
    if (isAutoPlaying && products.length > 0 && sliderRef.current) {
      interval = setInterval(() => {
        if (sliderRef.current && !isDragging) {
          const container = sliderRef.current;
          container.scrollLeft += 1; // Défilement lent
          
          // Si on arrive à la fin, revenir au début
          if (container.scrollLeft >= container.scrollWidth - container.clientWidth - 10) {
            container.scrollLeft = 0;
          }
        }
      }, 20);
    }
    return () => clearInterval(interval);
  }, [isAutoPlaying, products.length, isDragging]);

  // Gestion du drag pour le défilement manuel
  const handleMouseDown = (e) => {
    if (!sliderRef.current) return;
    setIsDragging(true);
    setStartX(e.pageX - sliderRef.current.offsetLeft);
    setScrollLeft(sliderRef.current.scrollLeft);
    setIsAutoPlaying(false);
  };

  const handleMouseLeave = () => {
    setIsDragging(false);
    if (!isDragging) setIsAutoPlaying(true);
  };

  const handleMouseUp = () => {
    setIsDragging(false);
    // Redémarrer l'auto-play après un délai
    setTimeout(() => setIsAutoPlaying(true), 2000);
  };

  const handleMouseMove = (e) => {
    if (!isDragging || !sliderRef.current) return;
    e.preventDefault();
    const x = e.pageX - sliderRef.current.offsetLeft;
    const walk = (x - startX) * 2; // Multiplicateur pour le défilement
    sliderRef.current.scrollLeft = scrollLeft - walk;
  };

  const handleTouchStart = (e) => {
    if (!sliderRef.current) return;
    setIsDragging(true);
    const touch = e.touches[0];
    setStartX(touch.pageX - sliderRef.current.offsetLeft);
    setScrollLeft(sliderRef.current.scrollLeft);
    setIsAutoPlaying(false);
  };

  const handleTouchMove = (e) => {
    if (!isDragging || !sliderRef.current) return;
    const touch = e.touches[0];
    const x = touch.pageX - sliderRef.current.offsetLeft;
    const walk = (x - startX) * 2;
    sliderRef.current.scrollLeft = scrollLeft - walk;
  };

  const handleTouchEnd = () => {
    setIsDragging(false);
    setTimeout(() => setIsAutoPlaying(true), 2000);
  };

  const handleArtisanClick = (artisanName, e) => {
    e.preventDefault();
    e.stopPropagation();
    navigate(`/artisan/${encodeURIComponent(artisanName)}`);
  };

  const scrollToCard = (direction) => {
    if (!sliderRef.current) return;
    const container = sliderRef.current;
    const cardWidth = 320; // Largeur approximative d'une carte
    const scrollAmount = direction === 'next' ? cardWidth : -cardWidth;
    
    container.scrollBy({
      left: scrollAmount,
      behavior: 'smooth'
    });
    
    setIsAutoPlaying(false);
    setTimeout(() => setIsAutoPlaying(true), 5000);
  };

  // Dupliquer les produits pour un effet infini
  const duplicatedProducts = [...products, ...products, ...products];

  if (!products || products.length === 0) {
    return (
      <div className="flower-slider-section">
        <div className="slider-header">
          <h2>Nos Créations Florales</h2>
          <p className="slider-subtitle">
            Aucun produit disponible pour le moment
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="flower-slider-section">
      <div className="slider-container">
        <button 
          className="slider-btn prev-btn" 
          onClick={() => scrollToCard('prev')}
          aria-label="Précédent"
        >
          ‹
        </button>

        <div 
          ref={sliderRef}
          className="horizontal-slider-wrapper"
          onMouseDown={handleMouseDown}
          onMouseLeave={handleMouseLeave}
          onMouseUp={handleMouseUp}
          onMouseMove={handleMouseMove}
          onTouchStart={handleTouchStart}
          onTouchMove={handleTouchMove}
          onTouchEnd={handleTouchEnd}
          style={{ cursor: isDragging ? 'grabbing' : 'grab' }}
        >
          <div className="horizontal-slider-track">
            {duplicatedProducts.map((product, index) => (
              <div
                key={`${product.id}-${index}`}
                className="horizontal-slide"
                onMouseEnter={() => setHoveredIndex(index)}
                onMouseLeave={() => setHoveredIndex(null)}
              >
                <div className="flower-card">
                  {/* Image et badges */}
                  <div className="flower-card">

                      <img
                        src={`https://localhost:44302${product.imageUrl}`}
                        alt={product.name}
                        className="flower-image"
                        onError={(e) => {
                          e.target.src = "/placeholder-flower.jpg";
                        }}
                      />
                    <div className="price-badge-large">
                      {formatCurrency ? formatCurrency(product.finalPrice || product.price) : (product.finalPrice || product.price)}
                    </div>
                    <div className="category-label">
                      {product.category || 'Bouquet'}
                    </div>
                  </div>

                  {/* Contenu principal */}
                  <div className="flower-info-simple">
                    <div className="product-header-simple">
                      <h3 className="product-title-clear">{product.name}</h3>
                      <div className="product-type-simple">
                        {product.type || 'Fleurs fraîches'}
                      </div>
                    </div>
                    <p className="product-description-simple">
                      {product.description || 'Bouquet artisanal de haute qualité'}
                    </p>
                    <div className="artisan-section">
                      <div className="artisan-label-simple">Artisan :</div>
                      <button
                                                className="artisan-name-simple"
                                                // CORRECTION ICI: Utilisation de product.storeName
                                                onClick={(e) => handleArtisanClick(product.storeName || 'Boutique', e)}
                                            >
                                                {product.storeName || 'Boutique Inconnue'}
                                            </button>
                    </div>
                  </div>

                  {/* Section actions */}
                  <div className="action-section-clear">
                    <button
                      className="cart-button-clear"
                      onClick={() => onOrder(product)}
                    >
                      <span className="cart-icon-clear">🛒</span>
                      Commander
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>

        <button 
          className="slider-btn next-btn" 
          onClick={() => scrollToCard('next')}
          aria-label="Suivant"
        >
          ›
        </button>
      </div>

      {/* Contrôles */}
      <div className="slider-controls">
        <div className="drag-hint">
          👆 Faites glisser pour défiler | Cliquez sur une carte pour commander
        </div>
      </div>
    </div>
  );
};

export default FlowerSlider;