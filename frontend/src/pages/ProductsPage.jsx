import { useEffect, useState } from "react";
import api from "../api/axios"; // Use your existing API configuration
import LoadingSpinner from "../components/LoadingSpinner";
import "../styles/page.css";
import plantImage from "../assets/plant.png";

export default function ProductsPage() {
    const [products, setProducts] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [searchQuery, setSearchQuery] = useState("");
    const [filteredProducts, setFilteredProducts] = useState([]);

    // Fetch products on component mount
    useEffect(() => {
        fetchProducts();
    }, []);

    // Filter products based on search query
    useEffect(() => {
        if (searchQuery.trim() === "") {
            setFilteredProducts(products);
        } else {
            const filtered = products.filter(product =>
                product.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
                (product.description && product.description.toLowerCase().includes(searchQuery.toLowerCase()))
            );
            setFilteredProducts(filtered);
        }
    }, [products, searchQuery]);

    const fetchProducts = async () => {
        try {
            setLoading(true);
            setError(null);
            
            // Use your existing API configuration
            const response = await api.get('/prestataire/products/');
            setProducts(response.data);
        } catch (err) {
            console.error("Error fetching products:", err);
            setError(err.response?.data?.message || err.message || "Erreur lors du chargement des produits");
        } finally {
            setLoading(false);
        }
    };

    const handleSearch = async (query) => {
        setSearchQuery(query);
        
        if (query.trim() === "") {
            return;
        }

        // You can implement backend search later like this:
        // try {
        //     const response = await api.get(`/prestataire/products/search?q=${encodeURIComponent(query)}`);
        //     setProducts(response.data);
        // } catch (err) {
        //     console.error("Error searching products:", err);
        //     setError(err.response?.data?.message || err.message);
        // }
    };

    const handleRefresh = () => {
        fetchProducts();
    };

    if (loading) {
        return (
            <div className="page-container">
                <div className="loading-container">
                    <LoadingSpinner />
                    <p>Chargement des produits...</p>
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="page-container">
                <div className="error-container">
                    <h2>Erreur de chargement</h2>
                    <p>{error}</p>
                    <button onClick={handleRefresh} className="retry-btn">
                        Réessayer
                    </button>
                </div>
            </div>
        );
    }

    return (
        <div className="page-container">
            <img src={plantImage} className="plant-bg" />

            <div className="page-header">
                <h1 className="page-title">Produits</h1>
                
                {/* Search Bar */}
                <div className="search-container">
                    <input
                        type="text"
                        placeholder="Rechercher des produits..."
                        value={searchQuery}
                        onChange={(e) => handleSearch(e.target.value)}
                        className="search-input"
                    />
                    <button onClick={handleRefresh} className="refresh-btn">
                        🔄 Actualiser
                    </button>
                </div>

                {/* Products Count */}
                <div className="products-info">
                    <p>{filteredProducts.length} produit(s) trouvé(s)</p>
                </div>
            </div>

            {/* Products Grid */}
            <div className="page-grid">
                {filteredProducts.length === 0 ? (
                    <div className="no-products">
                        <p>Aucun produit trouvé</p>
                        {searchQuery && (
                            <button onClick={() => setSearchQuery("")} className="clear-search-btn">
                                Effacer la recherche
                            </button>
                        )}
                    </div>
                ) : (
                    filteredProducts.map((product) => (
                        <div className="page-card product-card" key={product.id}>
                            <div className="product-image-container">
                                <img 
                                    src={product.imageUrl || plantImage} 
                                    alt={product.name}
                                    className="product-image"
                                    onError={(e) => {
                                        e.target.src = plantImage; // Fallback image
                                    }}
                                />
                            </div>
                            <div className="product-info">
                                <h3 className="product-name">{product.name}</h3>
                                {product.description && (
                                    <p className="product-description">{product.description}</p>
                                )}
                                <div className="product-price">
                                    <span className="price-amount">{product.price} MAD</span>
                                </div>
                                <div className="product-actions">
                                    <button className="btn-primary">Voir détails</button>
                                    <button className="btn-secondary">Modifier</button>
                                </div>
                            </div>
                        </div>
                    ))
                )}
            </div>


        </div>
    );
}
