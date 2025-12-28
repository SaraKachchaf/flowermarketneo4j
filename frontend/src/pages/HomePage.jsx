import React, { useEffect, useState } from 'react';
import './HomePage.css';
import BlurText from './BlurText';
import axios from '../api/axios';
import { useNavigate } from 'react-router-dom';
import FlowerSlider from './FlowerSlider';
import alerts from '../utils/alerts';
import { useCart } from '../context/CartContext';
const API_BASE = "https://localhost:44302";

// Utility for formatting currency
const formatCurrency = (amount) => {
    return new Intl.NumberFormat('fr-FR', {
        style: 'currency', currency: 'MAD', minimumFractionDigits: 0
    }).format(amount || 0);
};

const HomePage = () => {
    const navigate = useNavigate();
    const { addToCart, refreshCount } = useCart();
    const [promotedProducts, setPromotedProducts] = useState([]);
    const [products, setProducts] = useState([]);
    const [loading, setLoading] = useState(true);

    // Fetch Data from Backend
    useEffect(() => {
        const fetchMarketData = async () => {
            try {
                const [promoRes, productsRes] = await Promise.all([
                    axios.get('/market/promoted'),
                    axios.get('/market/products')
                ]);
                setPromotedProducts(promoRes.data?.data || []);
                setProducts(productsRes.data?.data || []);
            } catch (error) {
                console.error("Erreur lors du chargement des données :", error);
            } finally {
                setLoading(false);
            }
        };
        fetchMarketData();
    }, []);

    // Handle Order Logic
    const handleOrder = async (product) => {
        const token = localStorage.getItem("token");
        if (!token) {
            alerts.error("Connexion requise", "Veuillez vous connecter pour commander des fleurs.");
            navigate("/login");
            return;
        }

        const confirmed = await alerts.confirm(
            "Confirmer la commande",
            `Voulez-vous commander "${product.name}" pour ${formatCurrency(product.finalPrice || product.price)} ?`,
            "Commander"
        );

        if (!confirmed) return;

        try {
            await axios.post('/market/orders', {
                productId: product.id,
                quantity: 1
            });

            addToCart();
            await alerts.success("Commande envoyée !", `Votre commande pour "${product.name}" a été enregistrée.`);
            refreshCount();
        } catch (error) {
            console.error("Erreur commande:", error);
            if (error.response?.status !== 401) {
                alerts.error("Oups !", "Erreur lors de la commande. Veuillez réessayer.");
            }
        }
    };

    // Fonction pour obtenir l'URL complète de l'image
    const getImageUrl = (url) => {
        const BASE_URL = "https://localhost:44302"; // Fix: 44382 -> 44302
        if (!url) return 'https://via.placeholder.com/300';
        if (url.startsWith('http')) return url;
        return `${BASE_URL}${url}`;
    };

    // Smooth scroll effect
    useEffect(() => {
        const handleSmoothScroll = (e) => {
            if (e.target.hash) {
                e.preventDefault();
                const element = document.querySelector(e.target.hash);
                if (element) {
                    window.scrollTo({
                        top: element.offsetTop - 80,
                        behavior: 'smooth'
                    });
                }
            }
        };
        document.querySelectorAll('a[href^="#"]').forEach(anchor => {
            anchor.addEventListener('click', handleSmoothScroll);
        });
        return () => {
            document.querySelectorAll('a[href^="#"]').forEach(anchor => {
                anchor.removeEventListener('click', handleSmoothScroll);
            });
        };
    }, []);

    return (
        <div className="homepage-container">
            {/* Hero Section */}
            <div className="hero-section">
                <BlurText
                    text="Flower Market"
                    className="homepage-title"
                    delay={200}
                    animateBy="words"
                    direction="top"
                    stepDuration={0.5}
                    animationFrom={{ filter: 'blur(15px)', opacity: 0, y: -50 }}
                    animationTo={[
                        { filter: 'blur(5px)', opacity: 0.5, y: 5 },
                        { filter: 'blur(0px)', opacity: 1, y: 0 },
                    ]}
                />
                <p className="homepage-subtitle">L'art floral à portée de main, la nature chez vous</p>
                <div className="scroll-indicator">Découvrir ↓</div>
            </div>

            {/* Promotions Section */}
            <section id="promotions" className="promotions-section">
                {promotedProducts.length > 0 ? (
                    <>
                        <div className="section-header">
                            <h2>Promotions du Moment</h2>
                            <p className="section-subtitle">
                                Profitez de nos offres spéciales limitées dans le temps
                            </p>
                        </div>
                        <div className="promotions-grid">
                            {promotedProducts.map((product) => (
                                <div key={product.id} className="promotion-card">
                                    <div className="promotion-badge">-{product.discount}%</div>
                                    <div className="promotion-image-container">
                                        <img
                                            src={getImageUrl(product.imageUrl)}
                                            alt={product.name}
                                            className="card-image"
                                        />
                                    </div>
                                    <div className="promotion-content">
                                        <h3>{product.name}</h3>
                                        <p className="promotion-provider">Par {product.store?.name || "Boutique"}</p>
                                        <p className="promotion-description">{product.promotionTitle || "Offre spéciale"}</p>
                                        <div className="promotion-prices">
                                            <span className="original-price">{formatCurrency(product.originalPrice)}</span>
                                            <span className="current-price">{formatCurrency(product.finalPrice)}</span>
                                        </div>
                                        <button className="promotion-button" onClick={() => handleOrder(product)}>
                                            Profiter de l'offre
                                        </button>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </>
                ) : (
                    !loading && (
                        <div className="section-header">
                            <h2>Promotions du Moment</h2>
                            <p className="section-subtitle">Aucune promotion en cours. Revenez bientôt !</p>
                        </div>
                    )
                )}
            </section>

            {/* Products Section avec FlowerSlider */}
            <section id="produits">
                <div className="floating-flower"></div>
                <div className="floating-flower"></div>
                <div className="products-section-content">
                    <div className="products-intro">
                        <h2>Nos Créations Florales</h2>
                        <p>Chaque pétale raconte une histoire, chaque bouquet est une émotion.<br />
                            Découvrez les créations exclusives de nos prestataires.</p>
                    </div>
                    <div className="decorative-line"></div>

                    {/* Remplacez la grille par le FlowerSlider */}
                    {loading ? (
                        <p style={{ textAlign: 'center', padding: '60px', fontSize: '1.2rem' }}>
                            Chargement de nos créations florales...
                        </p>
                    ) : (
                        <FlowerSlider
                            products={products}
                            onOrder={handleOrder}
                            formatCurrency={formatCurrency}
                            getImageUrl={getImageUrl}
                        />
                    )}
                </div>
            </section>

            {/* Feature Section */}
            <div className="feature-section">
                <div className="feature-content">
                    <h3>🌿 Livraison Express & Fraîcheur Garantie</h3>
                    <p>Recevez la fraîcheur du jardin directement à votre porte.
                        Nos fleurs sont cueillies le matin même et livrées avec soin.</p>
                </div>
            </div>

            {/* Footer */}
            <footer className="footer">
                <div className="footer-content">
                    <div className="footer-section">
                        <h4>Flower Market</h4>
                        <p>Depuis 2010, nous apportons la beauté de la nature dans votre quotidien.</p>
                    </div>
                    <div className="footer-section">
                        <h4>Contact</h4>
                        <p>contact@flowermarket.fr</p>
                        <p>+212 623 45 67 89</p>
                    </div>
                    <div className="footer-section">
                        <h4>Suivez-nous</h4>
                        <div className="social-icons">
                            <span>📷</span>
                            <span>📘</span>
                            <span>📌</span>
                        </div>
                    </div>
                </div>
                <p className="copyright">© 2025 Flower Market. Tous droits réservés.</p>
            </footer>
        </div>
    );
};

export default HomePage;