import React, { useState, useEffect } from 'react';
import axios from '../api/axios';
import { useNavigate } from 'react-router-dom';
import { useCart } from '../context/CartContext';
import alerts from '../utils/alerts';
import { Plus, Minus, ShoppingCart, X, CreditCard, CheckCircle, Store, Calendar, ArrowRight, Trash2, History, Package, Clock } from 'lucide-react';
import './CreditCardForm.css'; // Assuming this exists from previous steps
import './MyOrders.css';
import CreditCardForm from './CreditCardForm'; // Assumes component is in same directory
import Receipt from './Receipt'; // Import du composant Receipt
const BASE_URL = "https://localhost:44302"; // Port backend Client HTTPS
const InteractivePayment = () => {
    const navigate = useNavigate();
    const { clearCart, refreshCount } = useCart();
    // Data States
    const [orders, setOrders] = useState([]);
    const [allRawOrders, setAllRawOrders] = useState([]);
    const [cart, setCart] = useState([]); // Stores orders selected for payment
    const [activeTab, setActiveTab] = useState('to-pay'); // 'to-pay' or 'history'
    const [loading, setLoading] = useState(true);
    // Modal States
    const [showPaymentModal, setShowPaymentModal] = useState(false);
    const [paymentMethod, setPaymentMethod] = useState(null); // 'card' or 'delivery'
    const [isSuccess, setIsSuccess] = useState(false);
    const [showReceipt, setShowReceipt] = useState(false); // État pour afficher le reçu
    const [paidCart, setPaidCart] = useState([]); // Store paid items for receipt
    useEffect(() => {
        fetchOrders();
    }, []);
    const fetchOrders = async () => {
        try {
            const token = localStorage.getItem('token');

            // 🌸 Auto-fix: if fullName is missing, fetch it
            if (!localStorage.getItem('fullName')) {
                const meRes = await axios.get('/auth/me', {
                    headers: { Authorization: `Bearer ${token}` }
                });
                if (meRes.data?.fullName) {
                    localStorage.setItem('fullName', meRes.data.fullName);
                }
            }

            const response = await axios.get('/market/my-orders', {
                headers: { Authorization: `Bearer ${token}` }
            });
            // Filter only pending or confirmed orders that are unpaid?
            // User requirement: "Client doesn't wait for confirmation".
            // So we show all 'pending' and 'confirmed' orders that are NOT paid.
            // Assuming status 'processing', 'shipped', 'delivered' are paid or handled.
            // Adjust filtering as needed.
            const allReturned = response.data?.data || [];
            setAllRawOrders(allReturned);
            console.log("🌸 RAW ORDERS FROM API:", allReturned);

            // For "To Pay" list
            setOrders(allReturned.filter(o => ['pending', 'confirmed'].includes(o.status)));

            // Sync Navbar count
            refreshCount();
        } catch (error) {
            console.error("Erreur chargement commandes:", error);
        } finally {
            setLoading(false);
        }
    };
    // Helper to get property value case-insensitively
    const getProp = (obj, key) => {
        if (!obj) return undefined;
        // Try exact match
        if (obj[key] !== undefined) return obj[key];
        // Try normalized match
        const lowerKey = key.toLowerCase();
        const foundKey = Object.keys(obj).find(k => k.toLowerCase() === lowerKey);
        return foundKey ? obj[foundKey] : undefined;
    };

    const getName = (o) => getProp(o, 'productName');
    const getId = (o) => getProp(o, 'productId');

    // Cart Logic (Grouped by Product Name)
    const getGroupedCart = () => {
        const groups = {};
        cart.forEach(order => {
            const pName = getName(order) || "Inconnu";
            if (!groups[pName]) {
                groups[pName] = {
                    productName: pName,
                    totalPrice: 0,
                    orders: []
                };
            }
            groups[pName].orders.push(order);
            groups[pName].totalPrice += (order.totalAmount || order.totalPrice || 0);
        });
        return Object.values(groups);
    };
    const addToCart = (order) => {
        // Can add the same "order" object multiple times if it represents a NEW order
        // But for existing orders, we should only add once.
        if (order.id && !order.isNew && cart.find(c => c.id === order.id)) {
            return; // Existing order already in cart
        }
        setCart([...cart, order]);
    };
    const removeFromCart = (productName, removeNewFirst = true) => {
        // Remove one instance of this product. 
        // Strategy: Remove "isNew" items first to avoid removing real pending orders if possible?
        // Or remove the last added?
        let targetIndex = -1;
        // Find all items of this product
        const items = cart.map((item, idx) => ({ ...item, originalIndex: idx }))
            .filter(item => getName(item) === productName);

        if (items.length === 0) return;
        if (removeNewFirst) {
            // Try to find a 'new' one first
            const newOrder = items.find(i => i.isNew);
            if (newOrder) targetIndex = newOrder.originalIndex;
            else targetIndex = items[items.length - 1].originalIndex; // Remove last
        } else {
            targetIndex = items[items.length - 1].originalIndex;
        }
        if (targetIndex !== -1) {
            const newCart = [...cart];
            newCart.splice(targetIndex, 1);
            setCart(newCart);
        }
    };
    const countInCart = (productName) => {
        return cart.filter(o => getName(o) === productName).length;
    };
    // Increments product quantity in cart
    const handleIncrement = (productName) => {
        // 1. Try to find an existing pending order for this product NOT in cart
        const existingOrder = orders.find(o => getName(o) === productName && !cart.find(c => c.id === o.id));
        if (existingOrder) {
            addToCart(existingOrder);
        } else {
            // 2. If no existing order, create a "Virtual" new order
            // We need product details. Find any order of this product to clone details.
            const template = orders.find(o => getName(o) === productName);
            if (template) {
                const newOrder = {
                    ...template,
                    id: `new-${Date.now()}-${Math.random()}`, // Temp ID
                    isNew: true,
                    status: 'new' // Indicator
                };
                addToCart(newOrder);
            }
        }
    };
    const handleDecrement = (productName) => {
        removeFromCart(productName);
    };
    const groupedCart = getGroupedCart();
    const totalAmount = cart.reduce((sum, item) => sum + (item.totalAmount || item.totalPrice || 0), 0);
    // Payment Logic
    const handleCheckout = () => {
        if (cart.length === 0) return;
        setShowPaymentModal(true);
    };
    const handlePaymentSuccess = async () => {
        const token = localStorage.getItem('token');
        try {
            for (const order of cart) {
                if (order.isNew) {
                    // 1. Create Order
                    const pid = getId(order) || (order.product && getId(order.product));

                    if (!pid) {
                        console.error("Impossible de trouver l'ID du produit pour l'ordre:", order);
                        // Tentative de secours ultime : si on a l'ID brut dans l'objet
                        const backupId = order.productId || order.ProductId || order.productid;
                        if (!backupId) throw new Error("L'identifiant du produit est manquant. Veuillez rafraîchir la page.");
                    }

                    console.log("Creating virtual order for product ID:", pid || order.productId || order.ProductId || order.productid);

                    const createRes = await axios.post('/market/orders', {
                        productId: pid || order.productId || order.ProductId || order.productid,
                        quantity: 1
                    }, { headers: { Authorization: `Bearer ${token}` } });
                    const newOrderId = createRes.data?.orderId;
                    // 2. Pay it
                    if (newOrderId) {
                        await axios.post(`/market/orders/${newOrderId}/pay`, {}, {
                            headers: { Authorization: `Bearer ${token}` }
                        });
                    }
                } else {
                    // Pay existing
                    await axios.post(`/market/orders/${order.id}/pay`, {}, {
                        headers: { Authorization: `Bearer ${token}` }
                    });
                }
            }
            setPaidCart(cart); // Save for receipt
            setIsSuccess(true);
            setCart([]);
            fetchOrders();
        } catch (error) {
            console.error("Erreur paiement:", error);
            // On affiche le message réel pour comprendre l'erreur (ex: Produit introuvable vs Stock)
            const backendMsg = error.response?.data?.message || (typeof error.response?.data === 'string' ? error.response.data : null);
            alerts.error("Paiement échoué", backendMsg || "Une erreur est survenue lors du paiement.");
        }
    };

    const handleDeleteOrder = async (productName) => {
        const confirmed = await alerts.confirm(
            "Annuler les commandes ?",
            `Voulez-vous vraiment supprimer toutes les commandes en attente pour "${productName}" ?`,
            "Supprimer"
        );
        if (!confirmed) return;

        const token = localStorage.getItem('token');
        const ordersToDelete = orders.filter(o => getName(o) === productName);

        try {
            for (const order of ordersToDelete) {
                await axios.delete(`/market/orders/${order.id}`, {
                    headers: { Authorization: `Bearer ${token}` }
                });
            }
            alerts.success("Supprimé", "Les commandes ont été annulées.");
            // Refresh list
            fetchOrders();
        } catch (error) {
            console.error("Erreur suppression:", error);
            alerts.error("Erreur", "Impossible de supprimer la commande.");
        }
    };
    // Helper for formatting
    const formatCurrency = (amount) => {
        return new Intl.NumberFormat('fr-MA', { style: 'currency', currency: 'MAD' }).format(amount);
    };
    const getStatusLabel = (status) => {
        switch (status) {
            case 'pending': return 'En attente';
            case 'confirmed': return 'Confirmée';
            case 'new': return 'Nouveau';
            default: return status;
        }
    };
    // Aggregation Logic for "To Pay" cards
    const getAggregatedPending = () => {
        const groups = {};
        orders.forEach(o => {
            const name = getName(o) || "Inconnu";
            if (!groups[name]) {
                const first = { ...o };
                groups[name] = {
                    ...first,
                    totalQty: 0,
                    aggregatePrice: 0,
                    orderIds: []
                };
            }
            groups[name].totalQty += (o.quantity || 1);
            groups[name].aggregatePrice += (o.totalPrice || o.totalAmount || 0);
            groups[name].orderIds.push(o.id);
        });
        return Object.values(groups);
    };

    const pendingOrders = allRawOrders.filter(o => ['pending', 'confirmed', 'new'].includes(o.status));
    const historyOrders = allRawOrders.filter(o => !['pending', 'confirmed', 'new'].includes(o.status));
    const aggregatedPending = getAggregatedPending();
    return (
        <div className="checkout-container">
            <div className="checkout-header-tabs">
                <button
                    className={`tab-btn ${activeTab === 'to-pay' ? 'active' : ''}`}
                    onClick={() => setActiveTab('to-pay')}
                >
                    <ShoppingCart size={18} /> À régler ({pendingOrders.length})
                </button>
                <button
                    className={`tab-btn ${activeTab === 'history' ? 'active' : ''}`}
                    onClick={() => setActiveTab('history')}
                >
                    <History size={18} /> Historique ({historyOrders.length})
                </button>
            </div>

            <div className="checkout-layout">
                {activeTab === 'to-pay' ? (
                    <>
                        {/* LEFT COLUMN: Available Products (Agregrated) */}
                        <div className="products-list">
                            {loading ? <p>Chargement...</p> : aggregatedPending.length === 0 ? (
                                <div style={{ textAlign: 'center', padding: '3rem', color: '#666', background: 'rgba(255,255,255,0.8)', borderRadius: '15px' }}>
                                    <div style={{ fontSize: '3rem', marginBottom: '1rem' }}>🛍️</div>
                                    <h3 style={{ fontSize: '1.25rem', fontWeight: '600', color: '#163d2d' }}>Votre panier est vide</h3>
                                    <p style={{ marginTop: '0.5rem', marginBottom: '1.5rem' }}>Vous n'avez aucune commande en attente de paiement.</p>
                                    <button className="btn-primary" onClick={() => navigate("/")} style={{ margin: '0 auto' }}>
                                        Retourner à la boutique
                                    </button>
                                </div>
                            ) : (
                                aggregatedPending.map(product => {
                                    const params = product;
                                    const qtyInCart = countInCart(params.productName);
                                    return (
                                        <div key={params.productName} className="product-card">
                                            <div className="product-info-wrapper">
                                                <div className="product-image-container">
                                                    {getProp(params, 'productImage') ? (
                                                        <img src={`${BASE_URL}${getProp(params, 'productImage')}`} alt={getName(params)} className="product-image" />
                                                    ) : (
                                                        <div style={{ width: '100%', height: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#ccc' }}>🌸</div>
                                                    )}
                                                    {params.totalQty > 1 && <span className="qty-badge-card">x{params.totalQty}</span>}
                                                </div>
                                                <div>
                                                    <div className="product-details">
                                                        <h3>{getName(params)}</h3>
                                                    </div>
                                                    <div className="product-meta">
                                                        <Store size={14} /> {getProp(params, 'storeName') || "Boutique"}
                                                    </div>
                                                    <div className="product-meta" style={{ color: '#18181b', fontWeight: '600', marginTop: '4px' }}>
                                                        {formatCurrency(getProp(params, 'totalPrice'))} <span style={{ fontSize: '0.7rem', color: '#666' }}>unité</span>
                                                    </div>
                                                </div>
                                            </div>
                                            <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                                                <button
                                                    className="btn-delete-small"
                                                    onClick={() => handleDeleteOrder(params.productName)}
                                                    title="Annuler tout"
                                                >
                                                    <Trash2 size={16} />
                                                </button>

                                                {qtyInCart > 0 ? (
                                                    <div style={{ display: 'flex', alignItems: 'center', gap: '5px', background: '#f4f4f5', borderRadius: '6px', padding: '2px' }}>
                                                        <button className="btn-icon" onClick={() => handleDecrement(params.productName)}>
                                                            <Minus size={16} />
                                                        </button>
                                                        <span style={{ fontWeight: '600', minWidth: '24px', textAlign: 'center' }}>{qtyInCart}</span>
                                                        <button className="btn-icon" onClick={() => handleIncrement(params.productName)}>
                                                            <Plus size={16} />
                                                        </button>
                                                    </div>
                                                ) : (
                                                    <button className="btn-outline" onClick={() => handleIncrement(params.productName)}>
                                                        <Plus size={16} /> Ajouter
                                                    </button>
                                                )}
                                            </div>
                                        </div>
                                    );
                                })
                            )}
                        </div>

                        {/* RIGHT COLUMN: Payment Summary (Cart) */}
                        <div className="cart-summary">
                            <div className="cart-header">
                                <ShoppingCart size={18} className="text-zinc-500" />
                                <h2>Votre Sélection ({cart.length})</h2>
                            </div>
                            <div className="cart-items-container">
                                {groupedCart.length === 0 ? (
                                    <p style={{ fontSize: '0.875rem', color: '#a1a1aa', textAlign: 'center', marginTop: '1rem' }}>
                                        Sélectionnez des articles pour payer.
                                    </p>
                                ) : (
                                    groupedCart.map((group, index) => (
                                        <div key={index} className="cart-item">
                                            <div className="cart-item-details">
                                                <div className="cart-item-row">
                                                    <span className="cart-item-name">{group.productName}</span>
                                                </div>
                                                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginTop: '5px' }}>
                                                    <div style={{ display: 'flex', alignItems: 'center', gap: '5px' }}>
                                                        <button className="btn-icon" onClick={() => handleDecrement(group.productName)} style={{ background: '#e4e4e7' }}>
                                                            <Minus size={12} />
                                                        </button>
                                                        <span style={{ fontSize: '0.875rem', fontWeight: '600', minWidth: '20px', textAlign: 'center' }}>
                                                            {group.orders.length}
                                                        </span>
                                                        <button className="btn-icon" onClick={() => handleIncrement(group.productName)} style={{ background: '#e4e4e7' }}>
                                                            <Plus size={12} />
                                                        </button>
                                                    </div>
                                                    <div className="cart-item-price">
                                                        {formatCurrency(group.totalPrice)}
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    ))
                                )}
                            </div>
                            <div className="cart-footer">
                                <div className="total-row">
                                    <span className="total-label">Total</span>
                                    <span className="total-value">{formatCurrency(totalAmount)}</span>
                                </div>
                                <button
                                    className="btn-primary"
                                    disabled={cart.length === 0}
                                    onClick={handleCheckout}
                                >
                                    <CreditCard size={16} />
                                    Payer {formatCurrency(totalAmount)}
                                </button>
                            </div>
                        </div>
                    </>
                ) : (
                    /* HISTORY TAB */
                    <div className="history-section" style={{ width: '100%', background: 'white', borderRadius: '1rem', padding: '1.5rem', boxShadow: '0 4px 6px rgba(0,0,0,0.05)' }}>
                        <h2 style={{ fontSize: '1.25rem', fontWeight: '600', marginBottom: '1.5rem', display: 'flex', alignItems: 'center', gap: '10px' }}>
                            <Package className="text-emerald-600" /> Historique de vos commandes
                        </h2>
                        {historyOrders.length === 0 ? (
                            <div style={{ textAlign: 'center', padding: '3rem', color: '#999' }}>
                                <Clock size={48} style={{ margin: '0 auto 1rem auto', opacity: 0.3 }} />
                                <p>Aucune commande passée pour le moment.</p>
                            </div>
                        ) : (
                            <div className="history-table-wrapper">
                                <table className="history-table">
                                    <thead>
                                        <tr>
                                            <th>Date</th>
                                            <th>Produit</th>
                                            <th>Magasin</th>
                                            <th>Status</th>
                                            <th>Total</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {historyOrders.map(o => (
                                            <tr key={o.id}>
                                                <td>{new Date(o.createdAt).toLocaleDateString()}</td>
                                                <td>{getName(o)}</td>
                                                <td>{getProp(o, 'storeName') || "Boutique"}</td>
                                                <td>
                                                    <span className={`status-badge ${o.status}`}>
                                                        {o.status === 'processing' ? 'Payé / En préparation' : o.status}
                                                    </span>
                                                </td>
                                                <td style={{ fontWeight: '600' }}>{formatCurrency(o.totalPrice)}</td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        )}
                    </div>
                )}
            </div>
            {/* PAYMENT METHOD MODAL */}
            {showPaymentModal && (
                <div className="modal-overlay">
                    <div className="modal-content" style={{ maxWidth: paymentMethod === 'card' ? '1000px' : '500px' }}>
                        <button className="close-modal-btn" onClick={() => { setShowPaymentModal(false); setPaymentMethod(null); }}>×</button>
                        {!paymentMethod ? (
                            <>
                                <h2 className="modal-title">Mode de Paiement</h2>
                                <p style={{ marginBottom: '20px', color: '#666' }}>Comment souhaitez-vous régler vos {cart.length} commande(s) ?</p>
                                <div className="payment-options">
                                    <button className="payment-option-btn" onClick={() => setPaymentMethod('card')}>
                                        <CreditCard size={20} />
                                        Carte Bancaire
                                    </button>
                                    <button className="payment-option-btn" onClick={() => { setPaymentMethod('delivery'); handlePaymentSuccess(); }}>
                                        <Store size={20} />
                                        Paiement à la livraison
                                    </button>
                                </div>
                            </>
                        ) : paymentMethod === 'card' ? (
                            <div className="card-payment-form">
                                <h3 style={{ marginBottom: '1rem', fontWeight: 'bold' }}>Paiement Sécurisé par Carte</h3>
                                <CreditCardForm onSubmit={handlePaymentSuccess} amount={totalAmount} />
                                <button
                                    style={{ marginTop: '10px', background: 'none', border: 'none', color: '#666', cursor: 'pointer', fontSize: '0.875rem' }}
                                    onClick={() => setPaymentMethod(null)}
                                >
                                    &larr; Retour au choix
                                </button>
                            </div>
                        ) : (
                            <div>Traitement...</div>
                        )}
                    </div>
                </div>
            )}
            {/* SUCCESS MODAL */}
            {isSuccess && (
                <div className="modal-overlay">
                    <div className="modal-content">
                        <div style={{ textAlign: 'center' }}>
                            <CheckCircle size={64} className="success-icon" style={{ margin: '0 auto 1rem auto' }} />
                            <h2 className="success-title">Paiement Validé !</h2>
                            <p className="success-text">
                                Vos {paidCart.length} commande(s) ont été payées avec succès.
                                <br />Merci pour votre confiance.
                            </p>
                            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px', marginTop: '24px' }}>
                                <button
                                    style={{
                                        padding: '12px 24px',
                                        borderRadius: '8px',
                                        border: 'none',
                                        background: '#f3f4f6',
                                        color: '#374151',
                                        fontWeight: '500',
                                        cursor: 'pointer',
                                        fontSize: '1rem'
                                    }}
                                    onClick={() => setShowReceipt(true)}
                                >
                                    Télécharger le reçu
                                </button>
                                <button className="btn-primary" onClick={() => {
                                    setIsSuccess(false);
                                    setShowPaymentModal(false);
                                    setPaymentMethod(null);
                                    clearCart();
                                    navigate("/");
                                }}>
                                    Continuer les achats
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            )}
            {/* RECEIPT MODAL */}
            {showReceipt && paidCart.length > 0 && (
                <Receipt
                    orderData={{
                        // Données du prestataire (depuis la première commande du panier)
                        shopName: paidCart[0].storeName || "FlowerMarket",
                        shopAddress: paidCart[0].storeAddress || "Adresse non disponible",
                        shopCity: paidCart[0].storeCity || "Ville",
                        shopZip: paidCart[0].storeZip || "",
                        shopEmail: paidCart[0].storeEmail || "admin@flowermarket.ma",
                        shopPhone: paidCart[0].storePhone || "",
                        // Informations de commande
                        orderNumber: paidCart.map(c => c.id).join('-'),
                        date: new Date().toLocaleDateString('fr-FR'),
                        time: new Date().toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' }),
                        customerName: localStorage.getItem('fullName') || "Client",
                        paymentMethod: paymentMethod === 'card' ? 'Carte Bancaire' : 'Paiement à la livraison',
                        // Articles du panier
                        items: paidCart.map(order => ({
                            name: order.productName,
                            quantity: order.quantity || 1,
                            price: order.totalAmount || order.totalPrice || 0
                        })),
                        // Totaux
                        subtotal: paidCart.reduce((sum, item) => sum + (item.totalAmount || item.totalPrice || 0), 0),
                        tax: paidCart.reduce((sum, item) => sum + (item.totalAmount || item.totalPrice || 0), 0) * 0.13, // Assuming tax calculation logic is consistent
                        total: paidCart.reduce((sum, item) => sum + (item.totalAmount || item.totalPrice || 0), 0) * 1.13
                    }}
                    onClose={() => setShowReceipt(false)}
                />
            )}
        </div>
    );
};
export default InteractivePayment;