// ... (imports remain same)
import React, { useState, useEffect } from 'react';
import axios from '../api/axios';
import './AdminDashboard.css';
import { useNavigate } from "react-router-dom";
import {
    BarChart3, Users, Package, ShoppingBag,
    Filter, Search, Bell, ChevronDown,
    CheckCircle, XCircle, MoreVertical,
    TrendingUp, DollarSign, Clock, UserCheck,
    Settings, LogOut, Calendar, Eye, Check
} from 'lucide-react';
import CustomDropdown from '../components/CustomDropdown';
import alerts from '../utils/alerts';

const AdminDashboard = () => {
    const [users, setUsers] = useState([]);
    const [products, setProducts] = useState([]);
    const [orders, setOrders] = useState([]);
    const [prestataires, setPrestataires] = useState([]);
    const [loading, setLoading] = useState(false);
    const [searchTerm, setSearchTerm] = useState('');
    const [activeTab, setActiveTab] = useState('dashboard');
    const [notifications, setNotifications] = useState([]);
    const [showNotifications, setShowNotifications] = useState(false);
    const [stats, setStats] = useState({
        pendingPrestataires: 0,
        totalClients: 0,
        totalProducts: 0,
        totalOrders: 0,
        totalRevenue: 0,
        pendingOrders: 0
    });
    const navigate = useNavigate();

    // RESTORED: handleLogout
    const handleLogout = () => {
        localStorage.removeItem("token");
        localStorage.removeItem("role");
        localStorage.removeItem("user");
        navigate("/login");
    };

    // NEW: Selected User State
    const [selectedUser, setSelectedUser] = useState(null);
    const [showUserModal, setShowUserModal] = useState(false);

    // Boutique Filter State
    const [selectedStore, setSelectedStore] = useState('all');

    // Role Filter State
    const [selectedRole, setSelectedRole] = useState('all');

    // ... (auth functions remain same)

    // Helper for API errors
    const handleApiError = (err) => {
        console.error('Erreur API:', err);
        if (err.response && (err.response.status === 401 || err.response.status === 403)) {
            handleLogout();
        }
    };

    useEffect(() => {
        const fetchData = async () => {
            try {
                setLoading(true);
                switch (activeTab) {
                    case 'dashboard':
                        const statsRes = await axios.get('/admin/stats');
                        setStats(statsRes.data?.data || stats);
                        break;
                    case 'users':
                        const usersRes = await axios.get('/admin/users');
                        setUsers(usersRes.data?.data || []);
                        break;
                    case 'products':
                        const productsRes = await axios.get('/admin/products');
                        setProducts(productsRes.data?.data || []);
                        break;
                    case 'orders':
                        const ordersRes = await axios.get('/admin/orders');
                        setOrders(ordersRes.data?.data || []);
                        break;
                    case 'prestataires':
                        const prestatairesRes = await axios.get('/admin/prestataires');
                        setPrestataires(prestatairesRes.data?.data || []);
                        break;
                    default:
                        break;
                }
            } catch (err) {
                console.error("Error fetching data", err);
                // handleApiError(err); // Avoid loop if handleLogout is referenced before definition if hoisted? No, const.
            } finally {
                setLoading(false);
            }
        };
        fetchData();
    }, [activeTab]);

    useEffect(() => {
        setSelectedStore('all');
        setSelectedRole('all');
        setSearchTerm('');
    }, [activeTab]);

    useEffect(() => {
        axios.get('/admin/notifications')
            .then(res => setNotifications(res.data.data || []))
            .catch(err => console.error(err));
    }, []);

    // NEW: Mark Notification as Read
    const handleMarkAsRead = async (id, e) => {
        e.stopPropagation(); // Prevent dropdown from closing
        try {
            await axios.put(`/admin/notifications/${id}/read`);
            // MODIFICATION : On retire la notif de la liste au lieu de juste changer son état
            setNotifications(notifications.filter(n => n.id !== id));
        } catch (err) {
            handleApiError(err);
        }
    };

    // NEW: Open User Modal
    const handleViewUser = (user) => {
        setSelectedUser(user);
        setShowUserModal(true);
    };

    // Helper to get unique stores based on active tab
    const storesList = React.useMemo(() => {
        if (activeTab === 'products') {
            return [...new Set(products.map(p => p.storeName).filter(Boolean))];
        }
        if (activeTab === 'users') {
            return [...new Set(users.map(u => u.storeName).filter(Boolean))];
        }
        return [...new Set(prestataires.map(p => p.storeName).filter(Boolean))];
    }, [activeTab, products, users, prestataires]);

    const filteredPrestataires = prestataires.filter(p =>
        p.fullName?.toLowerCase().includes(searchTerm.toLowerCase()) ||
        p.email?.toLowerCase().includes(searchTerm.toLowerCase())
    );

    const filteredUsers = users.filter(u =>
        (selectedStore === 'all' || u.storeName === selectedStore) &&
        (selectedRole === 'all' || u.role === selectedRole) &&
        (u.fullName?.toLowerCase().includes(searchTerm.toLowerCase()) ||
            u.email?.toLowerCase().includes(searchTerm.toLowerCase()))
    );

    const filteredProducts = products.filter(p =>
        (selectedStore === 'all' || p.storeName === selectedStore) &&
        (p.name?.toLowerCase().includes(searchTerm.toLowerCase()) ||
            p.category?.toLowerCase().includes(searchTerm.toLowerCase()))
    );

    const filteredOrders = orders.filter(o =>
        o.customerName?.toLowerCase().includes(searchTerm.toLowerCase()) ||
        o.productName?.toLowerCase().includes(searchTerm.toLowerCase())
    );

    const handleApprovePrestataire = async (id) => {
        try {
            await axios.post(`/admin/prestataires/${id}/approve`);
            const prestatairesRes = await axios.get('/admin/prestataires');
            setPrestataires(prestatairesRes.data.data);
            const statsRes = await axios.get('/admin/stats');
            setStats(statsRes.data.data);
        } catch (err) {
            handleApiError(err);
        }
    };

    const handleRejectPrestataire = async (id) => {
        const confirmed = await alerts.confirm(
            "Rejeter le prestataire",
            "Voulez-vous vraiment rejeter ce prestataire ?",
            "Rejeter"
        );
        if (confirmed) {
            try {
                await axios.delete(`/admin/prestataires/${id}/reject`);
                setPrestataires(prestataires.filter(p => p.id !== id && p._id !== id));
                alerts.success("Prestataire rejeté");
            } catch (err) {
                handleApiError(err);
                alerts.error("Erreur", "Impossible de rejeter le prestataire.");
            }
        }
    };

    const handleDeleteUser = async (id) => {
        const confirmed = await alerts.confirm(
            "Supprimer l'utilisateur",
            "Voulez-vous vraiment supprimer cet utilisateur ?",
            "Supprimer"
        );
        if (confirmed) {
            try {
                await axios.delete(`/admin/users/${id}`);
                setUsers(users.filter(u => u.id !== id && u._id !== id));
                alerts.success("Utilisateur supprimé");
            } catch (err) {
                handleApiError(err);
                alerts.error("Erreur", "Impossible de supprimer l'utilisateur.");
            }
        }
    };

    const formatDate = (date) => {
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
        }).format(amount);
    };

    return (
        <div className="dashboard-modern">
            {/* Sidebar */}
            <aside className="sidebar-modern">
                {/* ... (sidebar header) */}
                <div className="sidebar-header">
                    <div className="logo-container">
                        <div className="logo-icon">🌿</div>
                        <div>
                            <h1 className="logo-title">Flower Market</h1>
                            <p className="logo-subtitle">Dashboard Admin</p>
                        </div>
                    </div>
                    <div className="admin-profile">
                        <div className="admin-avatar">A</div>
                        <div>
                            <p className="admin-name">Administrateur</p>
                            <p className="admin-role">Super Admin</p>
                        </div>
                    </div>
                </div>

                <nav className="sidebar-nav">
                    {/* ... (nav items) */}
                    <div className="nav-section">
                        <p className="nav-section-title">PRINCIPAL</p>
                        <button
                            className={`nav-item ${activeTab === 'dashboard' ? 'active' : ''}`}
                            onClick={() => setActiveTab('dashboard')}
                        >
                            <BarChart3 size={20} />
                            <span>Tableau de bord</span>
                            {activeTab === 'dashboard' && <div className="nav-indicator" />}
                        </button>
                        <button
                            className={`nav-item ${activeTab === 'prestataires' ? 'active' : ''}`}
                            onClick={() => setActiveTab('prestataires')}
                        >
                            <UserCheck size={20} />
                            <span>Prestataires</span>
                            {/* BADGE: Uses stats.pendingPrestataires */}
                            {stats.pendingPrestataires > 0 && (
                                <span className="nav-badge">{stats.pendingPrestataires}</span>
                            )}
                        </button>
                        {/* ... (other nav items) */}
                        <button
                            className={`nav-item ${activeTab === 'users' ? 'active' : ''}`}
                            onClick={() => setActiveTab('users')}
                        >
                            <Users size={20} />
                            <span>Utilisateurs</span>
                        </button>
                        <button
                            className={`nav-item ${activeTab === 'products' ? 'active' : ''}`}
                            onClick={() => setActiveTab('products')}
                        >
                            <Package size={20} />
                            <span>Produits</span>
                        </button>
                        <button
                            className={`nav-item ${activeTab === 'orders' ? 'active' : ''}`}
                            onClick={() => setActiveTab('orders')}
                        >
                            <ShoppingBag size={20} />
                            <span>Commandes</span>
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
                {/* Header with Notifications */}
                <header className="main-header">
                    <div className="header-left">
                        <h1 className="page-title">
                            {activeTab === 'dashboard' && 'Tableau de bord'}
                            {activeTab === 'prestataires' && 'Gestion des Prestataires'}
                            {activeTab === 'users' && 'Gestion des Utilisateurs'}
                            {activeTab === 'products' && 'Gestion des Produits'}
                            {activeTab === 'orders' && 'Gestion des Commandes'}
                        </h1>
                        <p className="page-subtitle">
                            {activeTab === 'dashboard' && 'Aperçu complet de votre plateforme'}
                            {activeTab === 'prestataires' && 'Approuvez et gérez vos prestataires'}
                            {activeTab === 'users' && 'Gérez les comptes utilisateurs'}
                            {activeTab === 'products' && 'Consultez et modifiez les produits'}
                            {activeTab === 'orders' && 'Suivez toutes les commandes'}
                        </p>
                    </div>

                    <div className="notification-wrapper">
                        <button
                            className="notification-btn"
                            onClick={() => setShowNotifications(!showNotifications)}
                        >
                            <Bell size={20} />
                            {notifications.filter(n => !n.isRead).length > 0 && (
                                <span className="notification-badge">
                                    {notifications.filter(n => !n.isRead).length}
                                </span>
                            )}
                        </button>
                        {showNotifications && (
                            <div className="notification-dropdown">
                                <h4 className="notif-title">Notifications</h4>
                                <div className="notif-list">
                                    {notifications.filter(n => !n.isRead).length === 0 ? (
                                        <p className="notif-empty">Aucune notification</p>
                                    ) : (
                                        notifications.filter(n => !n.isRead).map((n) => (
                                            <div
                                                key={n.id}
                                                className={`notif-item ${n.isRead ? 'read' : 'unread'}`}
                                            >
                                                <div className="notif-header">
                                                    <strong>{n.title}</strong>
                                                    {!n.isRead && (
                                                        <button
                                                            className="mark-read-btn"
                                                            title="Marquer comme lu"
                                                            onClick={(e) => handleMarkAsRead(n.id, e)}
                                                        >
                                                            <Check size={14} />
                                                        </button>
                                                    )}
                                                </div>
                                                <p>{n.message}</p>
                                                <span className="notif-date">
                                                    {new Date(n.createdAt).toLocaleString()}
                                                </span>
                                            </div>
                                        ))
                                    )}
                                </div>
                            </div>
                        )}
                    </div>
                </header>

                {/* ... (Stats and other conditions remain same) */}
                {/* ... (For sake of update, assume stats part is unchanged or re-included) */}

                {/* Filter and Search Bar */}
                {activeTab !== 'dashboard' && (
                    <div className="filters-bar">
                        <div className="search-container">
                            <Search size={20} />
                            <input
                                type="text"
                                placeholder="Rechercher..."
                                value={searchTerm}
                                onChange={e => setSearchTerm(e.target.value)}
                            />
                        </div>

                        {(activeTab === 'users' || activeTab === 'products') && (
                            <div className="filter-group">
                                <CustomDropdown
                                    icon={Filter}
                                    options={[
                                        { value: 'all', label: 'Toutes les boutiques' },
                                        ...storesList.map(s => ({ value: s, label: s }))
                                    ]}
                                    value={selectedStore}
                                    onChange={setSelectedStore}
                                    placeholder="Sélectionner une boutique"
                                />

                                {activeTab === 'users' && (
                                    <CustomDropdown
                                        icon={UserCheck}
                                        options={[
                                            { value: 'all', label: 'Tous les rôles' },
                                            { value: 'Client', label: 'Client' },
                                            { value: 'Prestataire', label: 'Prestataire' },
                                            { value: 'Admin', label: 'Admin' }
                                        ]}
                                        value={selectedRole}
                                        onChange={setSelectedRole}
                                        placeholder="Sélectionner un rôle"
                                    />
                                )}
                            </div>
                        )}
                    </div>
                )}

                {/* Dashboard Stats */}
                {activeTab === 'dashboard' && (
                    <div className="dashboard-stats">
                        {/* ... (stats grid) */}
                        <div className="stats-grid">
                            <div className="stat-card revenue">
                                {/* ... */}
                                <div className="stat-icon"><DollarSign size={24} /></div>
                                <div className="stat-content">
                                    <p className="stat-label">Revenu total</p>
                                    <h3 className="stat-value">{formatCurrency(stats.totalRevenue || 0)}</h3>
                                    <div className="stat-trend positive"><TrendingUp size={16} /><span>+12.5%</span></div>
                                </div>
                            </div>
                            <div className="stat-card orders">
                                <div className="stat-icon"><ShoppingBag size={24} /></div>
                                <div className="stat-content">
                                    <p className="stat-label">Total commandes</p>
                                    <h3 className="stat-value">{stats.totalOrders || 0}</h3>
                                    <div className="stat-trend positive"><TrendingUp size={16} /><span>+8.2%</span></div>
                                </div>
                            </div>
                            <div className="stat-card pending">
                                <div className="stat-icon"><Clock size={24} /></div>
                                <div className="stat-content">
                                    <p className="stat-label">En attente</p>
                                    <h3 className="stat-value">{stats.pendingPrestataires || 0}</h3>
                                    <p className="stat-description">Prestataires à approuver</p>
                                </div>
                            </div>
                            <div className="stat-card users">
                                <div className="stat-icon"><Users size={24} /></div>
                                <div className="stat-content">
                                    <p className="stat-label">Utilisateurs actifs</p>
                                    <h3 className="stat-value">{stats.totalClients || 0}</h3>
                                    <div className="stat-trend positive"><TrendingUp size={16} /><span>+5.7%</span></div>
                                </div>
                            </div>
                        </div>

                        <div className="quick-actions">
                            <h3 className="section-title">Actions rapides</h3>
                            <div className="actions-grid">
                                <button className="action-btn" onClick={() => setActiveTab('prestataires')}>
                                    <UserCheck size={20} />
                                    <span>Approuver prestataires</span>
                                </button>
                                {/* ... */}
                                <button className="action-btn" onClick={() => setActiveTab('products')}>
                                    <Package size={20} />
                                    <span>Voir produit</span>
                                </button>
                                <button className="action-btn" onClick={() => setActiveTab('orders')}>
                                    <BarChart3 size={20} />
                                    <span>Voir commande</span>
                                </button>
                            </div>
                        </div>
                    </div>
                )}


                {/* Users Table with View User Action */}
                {activeTab === 'users' && (
                    <div className="data-container">
                        <div className="table-header">
                            <div className="table-info">
                                <h3 className="table-title">Utilisateurs</h3>
                                <p className="table-count">{filteredUsers.length} utilisateurs</p>
                            </div>
                        </div>
                        <div className="table-wrapper">
                            <table className="modern-table">
                                <thead>
                                    <tr>
                                        <th>UTILISATEUR</th><th>EMAIL</th><th>RÔLE</th><th>DATE D'INSCRIPTION</th><th>ACTIONS</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {filteredUsers.map((u, index) => (
                                        <tr key={u.id || u._id || index}>
                                            {/* ... (cells) */}
                                            <td>
                                                <div className="user-cell">
                                                    <div className="user-avatar">{u.fullName?.charAt(0) || 'U'}</div>
                                                    <div>
                                                        <p className="user-name">{u.fullName || 'Non spécifié'}</p>
                                                        <p className="user-id">ID: {String(u.id || u._id || 'N/A').slice(0, 8)}</p>
                                                    </div>
                                                </div>
                                            </td>
                                            <td>{u.email}</td>
                                            <td>
                                                <span className={`role-badge ${u.role === 'Admin' ? 'admin' : 'user'}`}>
                                                    {u.role || 'Utilisateur'}
                                                </span>
                                            </td>
                                            <td>{formatDate(u.createdAt)}</td>
                                            <td>
                                                <div className="action-buttons">
                                                    <button className="btn-view" onClick={() => handleViewUser(u)}>
                                                        <Eye size={16} /> Voir
                                                    </button>
                                                    {u.role !== 'Admin' && (
                                                        <button
                                                            className="btn-delete"
                                                            onClick={() => handleDeleteUser(u.id || u._id)}
                                                        >
                                                            <XCircle size={16} /> Supprimer
                                                        </button>
                                                    )}
                                                </div>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                            {filteredUsers.length === 0 && (
                                <div className="empty-state">
                                    <div className="empty-icon">👤</div>
                                    <h3>Aucun utilisateur trouvé</h3>
                                    <p>Aucun utilisateur à afficher pour le moment</p>
                                </div>
                            )}
                        </div>
                    </div>
                )}

                {/* Prestataires Table - Similar fix for View */}
                {activeTab === 'prestataires' && (
                    <div className="data-container">
                        {/* ... (header) */}
                        <div className="table-header">
                            <div className="table-info">
                                <h3 className="table-title">Prestataires</h3>
                                <p className="table-count">{filteredPrestataires.length} prestataires</p>
                            </div>
                        </div>
                        <div className="table-wrapper">
                            <table className="modern-table">
                                <thead>
                                    <tr>
                                        <th>PRESTATAIRE</th><th>EMAIL</th><th>STATUT</th><th>DATE D'INSCRIPTION</th><th>ACTIONS</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {filteredPrestataires.map((p, index) => (
                                        <tr key={p.id || p._id || index}>
                                            {/* ... */}
                                            <td>
                                                <div className="user-cell">
                                                    <div className="user-avatar">{p.fullName?.charAt(0) || 'P'}</div>
                                                    <div>
                                                        <p className="user-name">{p.fullName || 'Non spécifié'}</p>
                                                        <p className="user-id">ID: {String(p.id || p._id || 'N/A').slice(0, 8)}</p>
                                                    </div>
                                                </div>
                                            </td>
                                            <td>{p.email}</td>
                                            <td>
                                                <span className={`status-badge ${p.isApproved ? 'approved' : 'pending'}`}>
                                                    {p.isApproved ? 'Approuvé' : 'En attente'}
                                                </span>
                                            </td>
                                            <td>{formatDate(p.createdAt)}</td>
                                            <td>
                                                <div className="action-buttons">
                                                    {!p.isApproved ? (
                                                        <>
                                                            <button
                                                                className="btn-approve"
                                                                onClick={() => handleApprovePrestataire(p.id || p._id)}
                                                            >
                                                                <CheckCircle size={16} /> Approuver
                                                            </button>
                                                            <button
                                                                className="btn-reject"
                                                                onClick={() => handleRejectPrestataire(p.id || p._id)}
                                                            >
                                                                <XCircle size={16} /> Rejeter
                                                            </button>
                                                        </>
                                                    ) : (
                                                        <button className="btn-view" onClick={() => handleViewUser(p)}>
                                                            <Eye size={16} /> Voir
                                                        </button>
                                                    )}
                                                </div>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </div>
                )}
                {/* ... (Products and Orders tabs remain mostly same structure) */}
                {activeTab === 'products' && (
                    <div className="data-container">
                        {/* ... (Assuming content stays same, just re-rendering for completion if needed, otherwise skipping for brevity as user only requested changes on notification and user view) */}
                        <div className="table-header">
                            <div className="table-info">
                                <h3 className="table-title">Produits</h3>
                                <p className="table-count">{filteredProducts.length} produits</p>
                            </div>
                        </div>
                        <div className="table-wrapper">
                            <table className="modern-table">
                                <thead>
                                    <tr>
                                        <th>PRODUIT</th><th>PRIX</th><th>BOUTIQUE</th><th>CATÉGORIE</th><th>STOCK</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {filteredProducts.map((p, index) => (
                                        <tr key={p.id || p._id || index}>
                                            <td>
                                                <div className="product-cell">
                                                    <div className="product-image">
                                                        {p.image ? <img src={p.image} alt={p.name} /> : <div className="product-placeholder">🌷</div>}
                                                    </div>
                                                    <div>
                                                        <p className="product-name">{p.name}</p>
                                                        <p className="product-description">{p.description?.slice(0, 50)}...</p>
                                                    </div>
                                                </div>
                                            </td>
                                            <td className="price-cell">{formatCurrency(p.price || 0)}</td>
                                            <td>{p.storeName || 'Non spécifié'}</td>
                                            <td><span className="category-tag">{p.category || 'Non catégorisé'}</span></td>
                                            <td>
                                                <span className={`stock-indicator ${(p.stock || 0) > 10 ? 'in-stock' : (p.stock || 0) > 0 ? 'low-stock' : 'out-of-stock'}`}>
                                                    {(p.stock || 0) > 10 ? 'Disponible' : (p.stock || 0) > 0 ? 'Stock faible' : 'Rupture'}
                                                </span>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </div>
                )}

                {activeTab === 'orders' && (
                    <div className="data-container">
                        <div className="table-header">
                            <div className="table-info">
                                <h3 className="table-title">Commandes</h3>
                                <p className="table-count">{filteredOrders.length} commandes</p>
                            </div>
                        </div>
                        <div className="table-wrapper">
                            <table className="modern-table">
                                <thead>
                                    <tr>
                                        <th>COMMANDE</th><th>CLIENT</th><th>MONTANT</th><th>STATUT</th><th>DATE</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {filteredOrders.map((o, index) => (
                                        <tr key={o.id || o._id || index}>
                                            <td>
                                                <div className="order-cell">
                                                    <p className="order-id">#ORD-{String(o.id || o._id || index)}</p>
                                                    <p className="order-product">{o.productName || 'Produit non spécifié'}</p>
                                                </div>
                                            </td>
                                            <td>{o.customerName || 'Client non spécifié'}</td>
                                            <td className="price-cell">{formatCurrency(o.totalPrice || 0)}</td>
                                            <td>
                                                <span className={`order-status ${o.status || 'pending'}`}>
                                                    {o.status === 'completed' ? 'Terminée' :
                                                        o.status === 'shipped' ? 'Expédiée' :
                                                            o.status === 'processing' ? 'En traitement' : 'En attente'}
                                                </span>
                                            </td>
                                            <td>{formatDate(o.orderDate || o.createdAt)}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
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

            {/* USER DETAILS MODAL */}
            {showUserModal && selectedUser && (
                <div className="modal-overlay" onClick={() => setShowUserModal(false)}>
                    <div className="modal-content user-modal" onClick={e => e.stopPropagation()}>
                        <div className="modal-header">
                            <h3>Détails Utilisateur</h3>
                            <button className="close-btn" onClick={() => setShowUserModal(false)}>×</button>
                        </div>
                        <div className="modal-body">
                            <div className="user-details-view">
                                <div className="user-info-group">
                                    <label>Nom complet</label>
                                    <p>{selectedUser.fullName || 'Non spécifié'}</p>
                                </div>
                                <div className="user-info-group">
                                    <label>Email</label>
                                    <p>{selectedUser.email}</p>
                                </div>
                                <div className="user-info-group">
                                    <label>Rôle</label>
                                    <span className={`role-badge ${selectedUser.role === 'Admin' ? 'admin' : 'user'}`}>
                                        {selectedUser.role}
                                    </span>
                                </div>
                                <div className="user-info-group">
                                    <label>ID</label>
                                    <p>{selectedUser.id}</p>
                                </div>
                                <div className="user-info-group">
                                    <label>Status</label>
                                    <span className={`status-badge ${selectedUser.isApproved ? 'approved' : 'pending'}`}>
                                        {selectedUser.isApproved ? 'Actif' : 'En attente'}
                                    </span>
                                </div>
                                <div className="user-info-group">
                                    <label>Date d'inscription</label>
                                    <p>{formatDate(selectedUser.createdAt)}</p>
                                </div>
                            </div>
                        </div>
                        <div className="modal-footer">
                            <button className="btn-secondary" onClick={() => setShowUserModal(false)}>Fermer</button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};
export default AdminDashboard;