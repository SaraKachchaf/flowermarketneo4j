import React, { useState, useEffect } from 'react';
import axios from '../api/axios';
import { useNavigate } from "react-router-dom";
import {
  BarChart3, Package, ShoppingBag, Star, Tag,
  Filter, Search, Bell, Calendar, TrendingUp,
  DollarSign, Clock, Edit, Trash2, Plus,
  MessageSquare, Settings, LogOut,
  CheckCircle, XCircle, Users, Eye, ArrowUpDown
} from 'lucide-react';
import './PrestataireDashboard.css';
import alerts from '../utils/alerts';

const PrestataireDashboard = () => {
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState('dashboard');
  const [stats, setStats] = useState({
    totalProducts: 0,
    totalOrders: 0,
    pendingOrders: 0,
    totalReviews: 0,
    averageRating: 0,
    totalRevenue: 0
  });

  const [products, setProducts] = useState([]);
  const [orders, setOrders] = useState([]);
  const [reviews, setReviews] = useState([]);
  const [promotions, setPromotions] = useState([]);
  const [loading, setLoading] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [showAddProduct, setShowAddProduct] = useState(false);
  const [showAddPromotion, setShowAddPromotion] = useState(false);
  const [showViewOrder, setShowViewOrder] = useState(false);
  const [selectedOrder, setSelectedOrder] = useState(null);
  const [showEditOrder, setShowEditOrder] = useState(false);
  const [orderFilter, setOrderFilter] = useState("all");
  const [newProduct, setNewProduct] = useState({
    name: '',
    price: '',
    category: '',
    stock: '',
    description: '',
    image: null
  });
  // ===== PROMOTION MODAL =====
  const [showEditPromotion, setShowEditPromotion] = useState(false);
  const [editPromotion, setEditPromotion] = useState({
    id: null,
    discount: "",
    startDate: "",
    endDate: ""
  });

  // ===== VIEW / EDIT PRODUCT =====
  const [showViewProduct, setShowViewProduct] = useState(false);
  const [showEditProduct, setShowEditProduct] = useState(false);
  const [selectedProduct, setSelectedProduct] = useState(null);

  const [editProduct, setEditProduct] = useState({
    id: null,
    name: "",
    price: "",
    stock: "",
    category: "",
    description: "",
    imageUrl: "",
    isActive: true,
  });

  const [newPromotion, setNewPromotion] = useState({
    productId: '',
    discount: '',
    startDate: '',
    endDate: ''
  });

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        switch (activeTab) {
          case 'dashboard':
            const statsRes = await axios.get('/prestataire/stats');
            setStats(statsRes.data?.data || stats);
            break;
          case 'products':
            const productsRes = await axios.get('/prestataire/products');
            setProducts(productsRes.data?.data || []);
            break;
          case 'orders':
            const ordersRes = await axios.get('/prestataire/orders');
            setOrders(ordersRes.data?.data || []);
            break;
          case 'reviews':
            const reviewsRes = await axios.get('/prestataire/reviews');
            setReviews(reviewsRes.data?.data || []);
            break;
          case 'promotions':
            const promotionsRes = await axios.get('/prestataire/promotions');
            setPromotions(promotionsRes.data?.data || []);
            break;
        }
      } catch (err) {
        console.error('Erreur:', err);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [activeTab]);

  const handleLogout = () => {
    localStorage.removeItem("token");
    localStorage.removeItem("role");
    localStorage.removeItem("user");
    navigate("/login");
  };

  const handleAddProduct = async (e) => {
    e.preventDefault();

    try {
      const formData = new FormData();
      formData.append("name", newProduct.name);
      formData.append("price", newProduct.price);
      formData.append("stock", newProduct.stock);
      formData.append("category", newProduct.category);
      formData.append("description", newProduct.description);

      if (newProduct.image) {
        formData.append("image", newProduct.image);
      }

      await axios.post("/prestataire/products", formData);

      setShowAddProduct(false);
      setNewProduct({
        name: '',
        price: '',
        category: '',
        stock: '',
        description: '',
        image: null
      });

      const productsRes = await axios.get('/prestataire/products');
      setProducts(productsRes.data?.data || []);
      alerts.success("Produit créé", "Le produit a été ajouté à votre catalogue.");

    } catch (err) {
      console.error("Erreur création produit :", err);
      alerts.error("Oups !", "Erreur lors de la création du produit.");
    }
  };



  const handleDeleteProduct = async (id) => {
    const confirmed = await alerts.confirm(
      "Supprimer le produit",
      "Voulez-vous vraiment supprimer ce produit ?",
      "Supprimer"
    );
    if (confirmed) {
      try {
        await axios.delete(`/prestataire/products/${id}`);
        setProducts(products.filter(p => p.id !== id));
        alerts.success("Produit supprimé");
      } catch (err) {
        console.error('Erreur:', err);
        alerts.error("Erreur", "Impossible de supprimer le produit.");
      }
    }
  };

  const handleUpdateOrderStatus = async (orderId, newStatus) => {
    try {
      await axios.put(`/prestataire/orders/${orderId}`, { status: newStatus });
      setOrders(orders.map(order =>
        order.id === orderId ? { ...order, status: newStatus } : order
      ));
    } catch (err) {
      console.error('Erreur:', err);
    }
  };
  // ===== OPEN VIEW ORDER =====
  const openViewOrderModal = (order) => {
    setSelectedOrder(order);
    setShowViewOrder(true);
  };

  // ===== OPEN EDIT ORDER =====
  const openEditOrderModal = (order) => {
    setSelectedOrder(order);
    setShowEditOrder(true);
  };


  const handleAddPromotion = async (e) => {
    e.preventDefault();

    try {
      const token = localStorage.getItem("token");

      await axios.post(
        "/prestataire/promotions",
        {
          productId: Number(newPromotion.productId),
          discountPercent: Number(newPromotion.discount),
          startDate: newPromotion.startDate,
          endDate: newPromotion.endDate,
          title: "Promotion",
        },
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      // reset form
      setNewPromotion({
        productId: "",
        discount: "",
        startDate: "",
        endDate: "",
      });
      const res = await axios.get("/prestataire/promotions");
      setPromotions(res.data?.data || []);
      setShowAddPromotion(false);
      alerts.success("Promotion ajoutée");

    } catch (error) {
      console.error("Erreur ajout promotion :", error);
      alerts.error("Erreur", "Erreur lors de l’ajout de la promotion");
    }
  };

  // ===== VIEW PRODUCT =====
  const openViewModal = (product) => {
    setSelectedProduct(product);
    setShowViewProduct(true);
  };

  // ===== EDIT PRODUCT =====
  const openEditModal = (product) => {
    setEditProduct({
      id: product.id,
      name: product.name || "",
      price: product.price ?? "",
      stock: product.stock ?? "",
      category: product.category || "",
      description: product.description || "",
      imageUrl: product.imageUrl || "",
      isActive: product.isActive ?? true,
    });
    setShowEditProduct(true);
  };

  const handleUpdateProduct = async (e) => {
    e.preventDefault();

    try {
      await axios.put(
        `/prestataire/products/${editProduct.id}`,
        {
          name: editProduct.name,
          price: Number(editProduct.price),
          stock: Number(editProduct.stock),
          category: editProduct.category,
          description: editProduct.description,
          imageUrl: editProduct.imageUrl,
          isActive: editProduct.isActive,
        },
        {
          headers: {
            Authorization: `Bearer ${localStorage.getItem("token")}`,
          },
        }
      );

      setShowEditProduct(false);

      // refresh list
      const productsRes = await axios.get('/prestataire/products');
      setProducts(productsRes.data?.data || []);
      alerts.success("Produit modifié");

    } catch (err) {
      console.error("Erreur modification produit :", err.response?.data || err.message);
      alerts.error("Erreur", "Erreur lors de la modification du produit");
    }
  };



  const filteredProducts = products.filter(p =>
    p.name?.toLowerCase().includes(searchTerm.toLowerCase()) ||
    p.category?.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const filteredOrders = orders.filter(order => {
    const matchSearch =
      order.customerName?.toLowerCase().includes(searchTerm.toLowerCase()) ||
      order.productName?.toLowerCase().includes(searchTerm.toLowerCase()) ||
      String(order.id).includes(searchTerm);

    const matchStatus =
      orderFilter === "all" || order.status === orderFilter;

    return matchSearch && matchStatus;
  });

  const filteredPromotions = promotions.filter((p) =>
    p.title?.toLowerCase().includes(searchTerm.toLowerCase()) ||
    p.productName?.toLowerCase().includes(searchTerm.toLowerCase()) ||
    p.code?.toLowerCase().includes(searchTerm.toLowerCase())
  );


  const formatDate = (date) => {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString('fr-FR', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    });
  };

  const formatCurrency = (amount) => {
    return new Intl.NumberFormat('fr-FR', {
      style: 'currency',
      currency: 'MAD',
      minimumFractionDigits: 0
    }).format(amount || 0);
  };

  const renderStars = (rating) => {
    return (
      <div className="star-rating">
        {[...Array(5)].map((_, i) => (
          <Star
            key={i}
            size={16}
            fill={i < rating ? "#FFD700" : "none"}
            stroke={i < rating ? "#FFD700" : "#CBD5E1"}
          />
        ))}
      </div>
    );
  };
  const openEditPromotionModal = (promotion) => {
    setEditPromotion({
      id: promotion.id,
      discount: promotion.discount || promotion.discountPercent,
      startDate: promotion.startDate?.slice(0, 10),
      endDate: promotion.endDate?.slice(0, 10),
    });
    setShowEditPromotion(true);
  };
  const handleUpdatePromotion = async (e) => {
    e.preventDefault();

    try {
      await axios.put(
        `/prestataire/promotions/${editPromotion.id}`,
        {
          discountPercent: Number(editPromotion.discount),
          startDate: editPromotion.startDate,
          endDate: editPromotion.endDate,
        },
        {
          headers: {
            Authorization: `Bearer ${localStorage.getItem("token")}`,
          },
        }
      );

      setShowEditPromotion(false);

      const res = await axios.get("/prestataire/promotions");
      setPromotions(res.data?.data || []);
      alerts.success("Promotion modifiée");
    } catch (err) {
      console.error("Erreur modification promotion", err);
      alerts.error("Erreur", "Erreur lors de la modification");
    }
  };
  const handleDeletePromotion = async (id) => {
    const confirmed = await alerts.confirm(
      "Supprimer la promotion",
      "Voulez-vous vraiment supprimer cette promotion ?",
      "Supprimer"
    );
    if (!confirmed) return;

    try {
      await axios.delete(`/prestataire/promotions/${id}`, {
        headers: {
          Authorization: `Bearer ${localStorage.getItem("token")}`,
        },
      });

      const res = await axios.get("/prestataire/promotions");
      setPromotions(res.data?.data || []);
      alerts.success("Promotion supprimée");
    } catch (err) {
      console.error("Erreur suppression promotion", err);
      alerts.error("Erreur", "Erreur lors de la suppression");
    }
  };


  return (
    <div className="dashboard-modern prestataire">
      {/* Sidebar */}
      <aside className="sidebar-modern">
        <div className="sidebar-header">
          <div className="logo-container">
            <div className="logo-icon" style={{ background: 'linear-gradient(135deg, #3B82F6 0%, #1D4ED8 100%)' }}>🏪</div>
            <div>
              <h1 className="logo-title">Flower Market</h1>
              <p className="logo-subtitle">Espace Prestataire</p>
            </div>
          </div>
          <div className="admin-profile">
            <div className="admin-avatar" style={{ background: 'linear-gradient(135deg, #3B82F6 0%, #1D4ED8 100%)' }}>
              P
            </div>
            <div>
              <p className="admin-name">Votre Boutique</p>
              <p className="admin-role">Prestataire</p>
            </div>
          </div>
        </div>

        <nav className="sidebar-nav">
          <div className="nav-section">
            <p className="nav-section-title">MENU PRINCIPAL</p>
            <button
              className={`nav-item ${activeTab === 'dashboard' ? 'active' : ''}`}
              onClick={() => setActiveTab('dashboard')}
            >
              <BarChart3 size={20} />
              <span>Tableau de bord</span>
              {activeTab === 'dashboard' && <div className="nav-indicator" />}
            </button>

            <button
              className={`nav-item ${activeTab === 'products' ? 'active' : ''}`}
              onClick={() => setActiveTab('products')}
            >
              <Package size={20} />
              <span>Mes Produits</span>
              <span className="nav-badge">{stats.totalProducts}</span>
            </button>

            <button
              className={`nav-item ${activeTab === 'orders' ? 'active' : ''}`}
              onClick={() => setActiveTab('orders')}
            >
              <ShoppingBag size={20} />
              <span>Commandes</span>
              <span className="nav-badge">{stats.totalOrders}</span>
            </button>
            <button
              className={`nav-item ${activeTab === 'promotions' ? 'active' : ''}`}
              onClick={() => setActiveTab('promotions')}
            >
              <Tag size={20} />
              <span>Promotions</span>
            </button>
          </div>

          <div className="nav-section">
            <button className="nav-item" onClick={handleLogout}>
              <LogOut size={20} />
              <span>Déconnexion</span>
            </button>
          </div>
        </nav>
      </aside>

      {/* Main Content */}
      <main className="main-content-modern">
        {/* Header */}
        <header className="main-header">
          <div className="header-left">
            <h1 className="page-title">
              {activeTab === 'dashboard' && 'Tableau de bord Prestataire'}
              {activeTab === 'products' && 'Gestion des Produits'}
              {activeTab === 'orders' && 'Gestion des Commandes'}
              {activeTab === 'reviews' && 'Avis des Clients'}
              {activeTab === 'promotions' && 'Gestion des Promotions'}
            </h1>
            <p className="page-subtitle">
              {activeTab === 'dashboard' && 'Vue d\'ensemble de votre activité'}
              {activeTab === 'products' && 'Gérez votre catalogue de produits'}
              {activeTab === 'orders' && 'Suivez et traitez vos commandes'}
              {activeTab === 'reviews' && 'Consultez les avis sur vos produits'}
              {activeTab === 'promotions' && 'Créez et gérez vos promotions'}
            </p>
          </div>

          <div className="header-right">
            <div className="search-container">
              <Search size={18} />
              <input
                type="text"
                placeholder="Rechercher..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
              />
            </div>

          </div>
        </header>

        {/* Dashboard Stats */}
        {activeTab === 'dashboard' && (
          <div className="dashboard-stats">
            <div className="stats-grid">
              <div className="stat-card revenue">
                <div className="stat-icon" style={{ background: 'rgba(59, 130, 246, 0.1)' }}>
                  <DollarSign size={24} style={{ stroke: '#3B82F6' }} />
                </div>
                <div className="stat-content">
                  <p className="stat-label">Revenu total</p>
                  <h3 className="stat-value">{formatCurrency(stats.totalRevenue)}</h3>
                  <div className="stat-trend positive">
                    <TrendingUp size={16} />
                    <span>+12.5%</span>
                  </div>
                </div>
              </div>

              <div className="stat-card orders">
                <div className="stat-icon" style={{ background: 'rgba(16, 185, 129, 0.1)' }}>
                  <ShoppingBag size={24} style={{ stroke: '#10B981' }} />
                </div>
                <div className="stat-content">
                  <p className="stat-label">Commandes totales</p>
                  <h3 className="stat-value">{stats.totalOrders}</h3>
                  <div className="stat-trend positive">
                    <TrendingUp size={16} />
                    <span>+8.2%</span>
                  </div>
                </div>
              </div>

              <div className="stat-card pending">
                <div className="stat-icon" style={{ background: 'rgba(245, 158, 11, 0.1)' }}>
                  <Clock size={24} style={{ stroke: '#F59E0B' }} />
                </div>
                <div className="stat-content">
                  <p className="stat-label">En attente</p>
                  <h3 className="stat-value">{stats.pendingOrders}</h3>
                  <p className="stat-description">Commandes à traiter</p>
                </div>
              </div>
            </div>

            {/* Quick Actions */}
            <div className="quick-actions">
              <h3 className="section-title">Actions rapides</h3>
              <div className="actions-grid">
                <button className="action-btn" onClick={() => setShowAddProduct(true)}>
                  <Plus size={20} />
                  <span>Ajouter un produit</span>
                </button>
                <button className="action-btn" onClick={() => setActiveTab('orders')}>
                  <Eye size={20} />
                  <span>Voir les commandes</span>
                </button>
                <button className="action-btn" onClick={() => setShowAddPromotion(true)}>
                  <Tag size={20} />
                  <span>Créer une promotion</span>
                </button>
                <div className="form-group">
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Products Tab */}
        {activeTab === 'products' && (
          <div className="data-container">
            <div className="table-header">
              <div className="table-info">
                <h3 className="table-title">Mes Produits</h3>
                <p className="table-count">{filteredProducts.length} produit(s)</p>
              </div>
              <div className="table-actions">
                <button className="add-btn" onClick={() => setShowAddProduct(true)}>
                  <Plus size={18} />
                  <span>Ajouter un produit</span>
                </button>
              </div>
            </div>

            <div className="table-wrapper">
              <table className="modern-table">
                <thead>
                  <tr>
                    <th>PRODUIT</th>
                    <th>PRIX</th>
                    <th>CATÉGORIE</th>
                    <th>STOCK</th>
                    <th>Image</th>
                    <th>STATUT</th>
                    <th>ACTIONS</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredProducts.map((product, index) => (
                    <tr key={product.id || index}>

                      {/* PRODUIT */}
                      <td>
                        <div className="product-cell">
                          <div className="product-icon">🌷</div>
                          <div>
                            <p className="product-name">{product.name}</p>
                            <p className="product-description">
                              {product.description?.slice(0, 40)}...
                            </p>
                          </div>
                        </div>
                      </td>

                      {/* PRIX */}
                      <td className="price-cell">
                        {formatCurrency(product.price)}
                      </td>

                      {/* CATÉGORIE */}
                      <td>
                        <span className="category-tag">
                          {product.category || "—"}
                        </span>
                      </td>

                      {/* STOCK */}
                      <td>
                        <span className={`stock-indicator ${product.stock > 10
                            ? "in-stock"
                            : product.stock > 0
                              ? "low-stock"
                              : "out-of-stock"
                          }`}>
                          {product.stock} unités
                        </span>
                      </td>

                      {/* IMAGE */}
                      <td>
                        <div className="table-image">
                          {product.imageUrl ? (
                            <img
                              src={`https://localhost:44302${product.imageUrl}`}
                              alt={product.name}
                            />

                          ) : (
                            <span className="image-placeholder">📷</span>
                          )}
                        </div>
                      </td>

                      {/* STATUT */}
                      <td>
                        <span className={`status-badge ${product.isActive ? "approved" : "pending"
                          }`}>
                          {product.isActive ? "Actif" : "Inactif"}
                        </span>
                      </td>

                      {/* ACTIONS */}
                      <td>
                        <div className="action-buttons">
                          <button className="btn-view" onClick={() => openViewModal(product)}>
                            <Eye size={16} />
                            Voir
                          </button>

                          <button className="btn-edit" onClick={() => openEditModal(product)}>
                            <Edit size={16} />
                            Modifier
                          </button>
                          <button
                            className="btn-delete"
                            onClick={() => handleDeleteProduct(product.id)}
                          >
                            🗑 Supprimer
                          </button>
                        </div>
                      </td>

                    </tr>
                  ))}
                </tbody>

              </table>

              {filteredProducts.length === 0 && (
                <div className="empty-state">
                  <div className="empty-icon">📦</div>
                  <h3>Aucun produit trouvé</h3>
                  <p>Vous n'avez pas encore ajouté de produits</p>
                  <button className="empty-action-btn" onClick={() => setShowAddProduct(true)}>
                    <Plus size={16} />
                    Ajouter votre premier produit
                  </button>
                </div>
              )}
            </div>
          </div>
        )}

        {/* Orders Tab */}
        {activeTab === 'orders' && (
          <div className="data-container">
            <div className="table-header">
              <div className="table-info">
                <h3 className="table-title">Commandes Clients</h3>
                <p className="table-count">{filteredOrders.length} commande(s)</p>
              </div>
              <div className="table-actions">


                <select
                  className="filter-select"
                  value={orderFilter}
                  onChange={(e) => setOrderFilter(e.target.value)}
                >
                  <option value="all">Toutes les commandes</option>
                  <option value="pending">En attente</option>
                  <option value="confirmed">Confirmées</option>
                  <option value="processing">En traitement</option>
                  <option value="shipped">Expédiées</option>
                  <option value="delivered">Terminées</option>
                </select>

              </div>
            </div>

            <div className="table-wrapper">
              <table className="modern-table">
                <thead>
                  <tr>
                    <th>COMMANDE</th>
                    <th>CLIENT</th>
                    <th>PRODUITS</th>
                    <th>MONTANT</th>
                    <th>STATUT</th>
                    <th>DATE</th>
                    <th>ACTIONS</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredOrders.map((order, index) => (
                    <tr key={order.id || order._id || index}>
                      <td>
                        <div className="order-cell">
                          <p className="order-id">CMD-{String(index + 1).padStart(3, '0')}</p>
                        </div>
                      </td>
                      <td>
                        <div className="user-cell-small">
                          <div className="user-avatar-small">
                            {order.customerName?.charAt(0) || 'C'}
                          </div>
                          <div>
                            <p className="user-name">{order.customerName || 'Client'}</p>
                            <p className="user-email-small">{order.customerEmail?.slice(0, 20) || ''}...</p>
                          </div>
                        </div>
                      </td>
                      <td>
                        <div className="order-products">
                          <span className="product-chip">
                            {order.productName?.slice(0, 20) || 'Produit'}...
                          </span>
                        </div>
                      </td>
                      <td className="price-cell">{formatCurrency(order.totalAmount)}</td>
                      <td>
                        <select
                          className={`status-select ${order.status || 'pending'}`}
                          value={order.status || 'pending'}
                          onChange={(e) => handleUpdateOrderStatus(order.id || order._id, e.target.value)}
                        >
                          <option value="pending">⏳ En attente</option>
                          <option value="confirmed">✅ Confirmée</option>
                          <option value="processing">🔄 En traitement</option>
                          <option value="shipped">🚚 Expédiée</option>
                          <option value="delivered">📦 Terminée</option>
                          <option value="cancelled">❌ Annulée</option>
                        </select>
                      </td>
                      <td className="date-cell">{formatDate(order.createdAt)}</td>
                      <td>
                        <div className="action-buttons">
                          <button className="btn-view-small" onClick={() => openViewOrderModal(order)}>
                            <Eye size={14} />Voir
                          </button>
                          <button className="btn-edit-small" onClick={() => openEditOrderModal(order)}>
                            <Edit size={14} />Modifier
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>

              {filteredOrders.length === 0 && (
                <div className="empty-state">
                  <div className="empty-icon">🛒</div>
                  <h3>Aucune commande trouvée</h3>
                  <p>Aucune commande n'a été passée sur vos produits</p>
                  <button className="empty-action-btn" onClick={() => setActiveTab('products')}>
                    <Package size={16} />
                    Voir mes produits
                  </button>
                </div>
              )}
            </div>
          </div>
        )}
        {/* Promotions Tab */}
        {activeTab === 'promotions' && (
          <div className="data-container">
            <div className="table-header">
              <div className="table-info">
                <h3 className="table-title">Promotions Actives</h3>
                <p className="table-count">{promotions.length} promotion(s) active(s)</p>
              </div>
              <div className="table-actions">
                <button className="add-btn" onClick={() => setShowAddPromotion(true)}>
                  <Tag size={18} />
                  <span>Nouvelle promotion</span>
                </button>
              </div>
            </div>

            <div className="promotions-grid">
              {filteredPromotions.map((promotion, index) => (
                <div className="promotion-card" key={promotion.id || promotion._id || index}>
                  <div className="promotion-header">
                    <div className="promotion-icon">
                      <Tag size={24} />
                    </div>
                    <div>
                      <h4 className="promotion-title">{promotion.title || 'Promotion'}</h4>
                      <p className="promotion-code">Code: <strong>{promotion.code || 'N/A'}</strong></p>
                    </div>
                  </div>
                  <div className="promotion-body">
                    <div className="promotion-details">
                      <div className="detail-item">
                        <span className="detail-label">Produit:</span>
                        <span className="detail-value">{promotion.productName || 'Tous produits'}</span>
                      </div>
                      <div className="detail-item">
                        <span className="detail-label">Réduction:</span>
                        <span className="detail-value discount">{promotion.discount || 0}%</span>
                      </div>
                      <div className="detail-item">
                        <span className="detail-label">Valide jusqu'au:</span>
                        <span className="detail-value">{formatDate(promotion.endDate)}</span>
                      </div>
                    </div>
                    <div className="promotion-stats">
                      <div className="stat-item">
                        <span className="stat-label">Utilisations</span>
                        <span className="stat-value">{promotion.usageCount || 0}</span>
                      </div>
                      <div className="stat-item">
                        <span className="stat-label">Limite</span>
                        <span className="stat-value">{promotion.usageLimit || 'Illimité'}</span>
                      </div>
                    </div>
                  </div>
                  <div className="promotion-actions">
                    <button
                      className="btn-edit"
                      onClick={() => openEditPromotionModal(promotion)}
                    >
                      <Edit size={16} />
                      Modifier
                    </button>

                    <button
                      className="btn-delete"
                      onClick={() => handleDeletePromotion(promotion.id)}
                    >
                      <Trash2 size={16} />
                      Supprimer
                    </button>

                  </div>
                </div>
              ))}

              {promotions.length === 0 && (
                <div className="empty-state centered">
                  <div className="empty-icon">🏷️</div>
                  <h3>Aucune promotion active</h3>
                  <p>Créez votre première promotion pour attirer plus de clients</p>
                  <button className="empty-action-btn" onClick={() => setShowAddPromotion(true)}>
                    <Tag size={16} />
                    Créer une promotion
                  </button>
                </div>
              )}
            </div>
          </div>
        )}

        {/* Modals */}
        {showAddProduct && (
          <div className="modal-overlay">
            <div className="modal">
              <div className="modal-header">
                <h3>Ajouter un nouveau produit</h3>
                <button
                  className="modal-close"
                  onClick={() => setShowAddProduct(false)}
                >
                  ×
                </button>
              </div>

              <form onSubmit={handleAddProduct} encType="multipart/form-data">
                <div className="form-group">
                  <label>Nom du produit *</label>
                  <input
                    type="text"
                    required
                    value={newProduct.name}
                    onChange={(e) =>
                      setNewProduct({ ...newProduct, name: e.target.value })
                    }
                  />
                </div>

                <div className="form-row">
                  <div className="form-group">
                    <label>Prix (MAD) *</label>
                    <input
                      type="number"
                      required
                      value={newProduct.price}
                      onChange={(e) =>
                        setNewProduct({ ...newProduct, price: e.target.value })
                      }
                    />
                  </div>

                  <div className="form-group">
                    <label>Stock *</label>
                    <input
                      type="number"
                      required
                      value={newProduct.stock}
                      onChange={(e) =>
                        setNewProduct({ ...newProduct, stock: e.target.value })
                      }
                    />
                  </div>
                </div>

                <div className="form-group">
                  <label>Catégorie</label>
                  <select
                    value={newProduct.category}
                    onChange={(e) =>
                      setNewProduct({ ...newProduct, category: e.target.value })
                    }
                  >
                    <option value="">Sélectionner une catégorie</option>
                    <option value="fleurs">Fleurs</option>
                    <option value="bouquets">Bouquets</option>
                    <option value="plantes">Plantes</option>
                    <option value="deco">Décoration</option>
                  </select>
                </div>

                <div className="form-group">
                  <label>Description</label>
                  <textarea
                    rows="3"
                    value={newProduct.description}
                    onChange={(e) =>
                      setNewProduct({ ...newProduct, description: e.target.value })
                    }
                  />
                </div>

                {/* ✅ IMAGE UPLOAD */}
                <div className="form-group">
                  <label>Image du produit</label>
                  <input
                    type="file"
                    accept="image/*"
                    onChange={(e) =>
                      setNewProduct({ ...newProduct, image: e.target.files[0] })
                    }
                  />
                </div>

                <div className="modal-actions">
                  <button
                    type="button"
                    className="btn-cancel"
                    onClick={() => setShowAddProduct(false)}
                  >
                    Annuler
                  </button>

                  <button type="submit" className="btn-submit">
                    <Plus size={16} />
                    Ajouter le produit
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}
        {showAddPromotion && (
          <div className="modal-overlay">
            <div className="modal">
              <div className="modal-header">
                <h3>Créer une nouvelle promotion</h3>
                <button className="modal-close" onClick={() => setShowAddPromotion(false)}>×</button>
              </div>
              <form onSubmit={handleAddPromotion}>
                <div className="form-group">
                  <label>Produit concerné</label>
                  <select
                    value={newPromotion.productId}
                    onChange={(e) => setNewPromotion({ ...newPromotion, productId: e.target.value })}
                  >
                    <option value="">Tous les produits</option>
                    {products.map(product => (
                      <option key={product.id} value={product.id}>
                        {product.name}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="form-group">
                  <label>Pourcentage de réduction *</label>
                  <div className="discount-input">
                    <input
                      type="number"
                      min="1"
                      max="100"
                      required
                      value={newPromotion.discount}
                      onChange={(e) => setNewPromotion({ ...newPromotion, discount: e.target.value })}
                    />
                    <span className="percent-symbol">%</span>
                  </div>
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label>Date de début</label>
                    <input
                      type="date"
                      value={newPromotion.startDate}
                      onChange={(e) => setNewPromotion({ ...newPromotion, startDate: e.target.value })}
                    />
                  </div>
                  <div className="form-group">
                    <label>Date de fin</label>
                    <input
                      type="date"
                      value={newPromotion.endDate}
                      onChange={(e) => setNewPromotion({ ...newPromotion, endDate: e.target.value })}
                    />
                  </div>
                </div>
                <div className="modal-actions">
                  <button type="button" className="btn-cancel" onClick={() => setShowAddPromotion(false)}>
                    Annuler
                  </button>
                  <button type="submit" className="btn-submit">
                    <Tag size={16} />
                    Créer la promotion
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}
        {showViewProduct && selectedProduct && (
          <div className="modal-overlay">
            <div className="modal">
              <div className="modal-header">
                <h3>Détails du produit</h3>
                <button className="modal-close" onClick={() => setShowViewProduct(false)}>×</button>
              </div>

              <div className="modal-body">
                <div className="product-details">
                  <img
                    src={
                      selectedProduct.imageUrl
                        ? `https://localhost:44302${selectedProduct.imageUrl}`
                        : "https://via.placeholder.com/80"
                    }
                    alt={selectedProduct.name}
                  />


                  <div>
                    <h4>{selectedProduct.name}</h4>
                    <div className="product-meta">
                      <span><strong>Prix :</strong> {selectedProduct.price} MAD</span>
                      <span><strong>Stock :</strong> {selectedProduct.stock}</span>
                      <span><strong>Catégorie :</strong> {selectedProduct.category}</span>
                      <span><strong>Statut :</strong> {selectedProduct.isActive ? "Actif" : "Inactif"}</span>
                    </div>
                  </div>
                </div>
              </div>

              <div className="modal-actions">
                <button className="btn-secondary" onClick={() => setShowViewProduct(false)}>
                  Fermer
                </button>
              </div>
            </div>
          </div>
        )}

        {showEditProduct && (
          <div className="modal-overlay">
            <div className="modal">
              <div className="modal-header">
                <h3>Modifier le produit</h3>
                <button className="modal-close" onClick={() => setShowEditProduct(false)}>×</button>
              </div>

              <form onSubmit={handleUpdateProduct}>
                <div className="form-group">
                  <label>Nom du produit</label>
                  <input
                    type="text"
                    value={editProduct.name}
                    onChange={(e) => setEditProduct({ ...editProduct, name: e.target.value })}
                  />
                </div>

                <div className="form-group">
                  <label>Prix (MAD)</label>
                  <input
                    type="number"
                    value={editProduct.price}
                    onChange={(e) => setEditProduct({ ...editProduct, price: e.target.value })}
                  />
                </div>

                <div className="form-group">
                  <label>Stock</label>
                  <input
                    type="number"
                    value={editProduct.stock}
                    onChange={(e) => setEditProduct({ ...editProduct, stock: e.target.value })}
                  />
                </div>

                <div className="form-group">
                  <label>Description</label>
                  <textarea
                    rows="3"
                    value={editProduct.description}
                    onChange={(e) => setEditProduct({ ...editProduct, description: e.target.value })}
                  />
                </div>

                <div className="modal-actions">
                  <button type="button" className="btn-secondary" onClick={() => setShowEditProduct(false)}>
                    Annuler
                  </button>
                  <button type="submit" className="btn-primary">
                    Enregistrer
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}
        {showEditPromotion && (
          <div className="modal-overlay">
            <div className="modal">
              <div className="modal-header">
                <h3>Modifier la promotion</h3>
                <button
                  className="modal-close"
                  onClick={() => setShowEditPromotion(false)}
                >
                  ×
                </button>
              </div>

              <form onSubmit={handleUpdatePromotion}>
                <div className="form-group">
                  <label>Réduction (%)</label>
                  <input
                    type="number"
                    min="1"
                    max="100"
                    required
                    value={editPromotion.discount}
                    onChange={(e) =>
                      setEditPromotion({ ...editPromotion, discount: e.target.value })
                    }
                  />
                </div>

                <div className="form-row">
                  <div className="form-group">
                    <label>Date début</label>
                    <input
                      type="date"
                      value={editPromotion.startDate}
                      onChange={(e) =>
                        setEditPromotion({ ...editPromotion, startDate: e.target.value })
                      }
                    />
                  </div>

                  <div className="form-group">
                    <label>Date fin</label>
                    <input
                      type="date"
                      value={editPromotion.endDate}
                      onChange={(e) =>
                        setEditPromotion({ ...editPromotion, endDate: e.target.value })
                      }
                    />
                  </div>
                </div>

                <div className="modal-actions">
                  <button
                    type="button"
                    className="btn-secondary"
                    onClick={() => setShowEditPromotion(false)}
                  >
                    Annuler
                  </button>
                  <button type="submit" className="btn-primary">
                    Enregistrer
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}
        {showViewOrder && selectedOrder && (
          <div className="modal-overlay">
            <div className="modal modal-lg">
              <div className="modal-header">
                <h3>Détails de la commande</h3>
                <button className="modal-close" onClick={() => setShowViewOrder(false)}>×</button>
              </div>

              <div className="modal-body">
                <p><strong>ID :</strong> CMD-{selectedOrder.id}</p>
                <p><strong>Client :</strong> {selectedOrder.customerName}</p>
                <p><strong>Email :</strong> {selectedOrder.customerEmail}</p>
                <p><strong>Produit :</strong> {selectedOrder.productName}</p>
                <p><strong>Montant :</strong> {formatCurrency(selectedOrder.totalAmount)}</p>
                <p><strong>Statut :</strong> {selectedOrder.status}</p>
                <p><strong>Date :</strong> {formatDate(selectedOrder.createdAt)}</p>
              </div>

              <div className="modal-actions">
                <button className="btn-secondary" onClick={() => setShowViewOrder(false)}>
                  Fermer
                </button>
              </div>
            </div>
          </div>
        )}
        {showEditOrder && selectedOrder && (
          <div className="modal-overlay">
            <div className="modal">
              <div className="modal-header">
                <h3>Modifier le statut</h3>
                <button className="modal-close" onClick={() => setShowEditOrder(false)}>×</button>
              </div>

              <form
                onSubmit={(e) => {
                  e.preventDefault();
                  handleUpdateOrderStatus(selectedOrder.id, selectedOrder.status);
                  setShowEditOrder(false);
                }}
              >
                <div className="form-group">
                  <label>Statut</label>
                  <select
                    value={selectedOrder.status}
                    onChange={(e) =>
                      setSelectedOrder({ ...selectedOrder, status: e.target.value })
                    }
                  >
                    <option value="pending">En attente</option>
                    <option value="confirmed">Confirmée</option>
                    <option value="processing">En traitement</option>
                    <option value="shipped">Expédiée</option>
                    <option value="delivered">Terminée</option>
                    <option value="cancelled">Annulée</option>
                  </select>
                </div>

                <div className="modal-actions">
                  <button type="button" className="btn-secondary" onClick={() => setShowEditOrder(false)}>
                    Annuler
                  </button>
                  <button type="submit" className="btn-primary">
                    Enregistrer
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}





        {loading && (
          <div className="loading-overlay">
            <div className="loading-spinner"></div>
            <p>Chargement...</p>
          </div>
        )}
      </main>
    </div>
  );
};

export default PrestataireDashboard;