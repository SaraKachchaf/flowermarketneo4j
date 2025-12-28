import React, { createContext, useContext, useState, useEffect } from 'react';
import axios from '../api/axios';

const CartContext = createContext();

export const useCart = () => {
    const context = useContext(CartContext);
    if (!context) {
        throw new Error('useCart must be used within a CartProvider');
    }
    return context;
};

export const CartProvider = ({ children }) => {
    // Initial count from localStorage if needed, or start at 0
    const [cartCount, setCartCount] = useState(() => {
        const saved = localStorage.getItem('cartCount');
        return saved ? parseInt(saved, 10) : 0;
    });

    useEffect(() => {
        localStorage.setItem('cartCount', cartCount);
    }, [cartCount]);

    const refreshCount = async () => {
        const token = localStorage.getItem('token');
        if (!token) {
            setCartCount(0);
            return;
        }
        try {
            // On ajoute un flag pour indiquer à l'intercepteur de NE PAS rediriger ici
            const res = await axios.get('/market/my-orders', { _noRedirect: true });
            const allOrders = res.data?.data || [];
            const pendingCount = allOrders.filter(o => ['pending', 'confirmed'].includes(o.status)).length;
            setCartCount(pendingCount);
            localStorage.setItem('cartCount', pendingCount);
        } catch (error) {
            console.error("Erreur sync panier (silent):", error.message);
            // On ne fait rien pour ne pas déconnecter l'utilisateur pendant qu'il navigue
        }
    };

    useEffect(() => {
        refreshCount();
    }, []);

    const addToCart = () => {
        setCartCount(prev => prev + 1);
    };

    const removeFromCartCount = () => {
        setCartCount(prev => (prev > 0 ? prev - 1 : 0));
    };

    const clearCart = () => {
        setCartCount(0);
    };

    return (
        <CartContext.Provider value={{ cartCount, addToCart, removeFromCartCount, clearCart, refreshCount }}>
            {children}
        </CartContext.Provider>
    );
};
