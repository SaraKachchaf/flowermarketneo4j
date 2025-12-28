# 🚀 Backend Integration Guide

## ✅ Current Setup

You already have:
- **API Configuration**: `src/api/axios.js` with your backend URL `https://localhost:44302/api`
- **Authentication**: Login, Register, Dashboard components
- **Token Management**: Automatic token handling in API requests

## 🔧 How to Connect Frontend to Backend

### 1. **Your Existing API Setup**
```javascript
// src/api/axios.js - Already configured ✅
const api = axios.create({
  baseURL: 'https://localhost:44302/api',
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
});
```

### 2. **Available Services**
I've created service files that use your existing API:

#### **Product Service** (`src/services/productService.js`)
```javascript
import api from '../api/axios';

// Get all products
const products = await productService.getAllProducts();

// Get product by ID
const product = await productService.getProductById(id);

// Create new product
const newProduct = await productService.createProduct(productData);
```

#### **Order Service** (`src/services/orderService.js`)
```javascript
import api from '../api/axios';

// Create order
const order = await orderService.createOrder(orderData);

// Get user orders
const orders = await orderService.getUserOrders(userId);

// Process payment
const payment = await orderService.processPayment(paymentData);
```

#### **Auth Service** (`src/services/authService.js`)
```javascript
import api from '../api/axios';

// Login (already working in your Login.jsx)
const result = await authService.login({ email, password });

// Check if authenticated
const isAuth = authService.isAuthenticated();
```

### 3. **Updated Components**

#### **ProductsPage** - Now uses your API ✅
```javascript
// Uses your existing API configuration
const response = await api.get('/prestataire/products/');
```

#### **Enhanced Checkout Flow** ✅
- CartPage with progress indicator
- DeliveryInfo with form validation
- PaymentMethod with modern UI
- OrderConfirmation with success animation

### 4. **Backend Endpoints You Need**

Based on your frontend, make sure your backend has these endpoints:

#### **Products**
```
GET    /prestataire/products/          - Get all products
GET    /prestataire/products/:id       - Get product by ID
POST   /prestataire/products/          - Create product
PUT    /prestataire/products/:id       - Update product
DELETE /prestataire/products/:id       - Delete product
GET    /prestataire/products/search    - Search products
```

#### **Orders**
```
POST   /orders                         - Create order
GET    /orders/user/:userId            - Get user orders
GET    /orders/:orderId                - Get order by ID
PATCH  /orders/:orderId/status         - Update order status
POST   /orders/payment                 - Process payment
```

#### **Auth** (Already working ✅)
```
POST   /auth/login                     - Login
POST   /auth/register                  - Register
POST   /auth/refresh                   - Refresh token
```

### 5. **How to Use the Services**

#### **In ProductsPage** (Already updated ✅)
```javascript
import api from '../api/axios';

const fetchProducts = async () => {
  try {
    const response = await api.get('/prestataire/products/');
    setProducts(response.data);
  } catch (error) {
    console.error('Error:', error);
  }
};
```

#### **In CartPage** (Ready for integration)
```javascript
import { orderService } from '../services/orderService';

const handleCheckout = async () => {
  try {
    const orderData = {
      items: cartItems,
      total: total,
      deliveryInfo: deliveryInfo
    };
    
    const order = await orderService.createOrder(orderData);
    navigate('/confirmation', { state: { order } });
  } catch (error) {
    console.error('Checkout error:', error);
  }
};
```

### 6. **Environment Configuration**

Update your `.env` file:
```env
REACT_APP_API_URL=https://localhost:44302/api
REACT_APP_ENV=development
```

### 7. **Error Handling**

Your API already handles errors globally:
- **401**: Redirects to login
- **403**: Access denied
- **404**: Resource not found
- **500**: Server error

### 8. **Next Steps**

1. **Test your existing endpoints**:
   ```bash
   # Test if your backend is running
   curl https://localhost:44302/api/prestataire/products/
   ```

2. **Add missing endpoints** to your backend if needed

3. **Test the checkout flow**:
   - Add items to cart
   - Go through delivery info
   - Process payment
   - Confirm order

4. **Add real data** to replace mock data in components

### 9. **Quick Integration Example**

Here's how to quickly connect a component to your backend:

```javascript
import { useState, useEffect } from 'react';
import api from '../api/axios';

const MyComponent = () => {
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        const response = await api.get('/your-endpoint');
        setData(response.data);
      } catch (err) {
        setError(err.response?.data?.message || err.message);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, []);

  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {error}</div>;

  return (
    <div>
      {data.map(item => (
        <div key={item.id}>{item.name}</div>
      ))}
    </div>
  );
};
```

## 🎉 You're Ready!

Your frontend is now properly configured to work with your backend. The enhanced checkout flow is ready, and you can start testing the integration immediately!

### Test URLs:
- **Home**: http://localhost:5174/
- **Login**: http://localhost:5174/login
- **Products**: http://localhost:5174/produits
- **Cart**: http://localhost:5174/cart
- **Dashboard**: http://localhost:5174/dashboard