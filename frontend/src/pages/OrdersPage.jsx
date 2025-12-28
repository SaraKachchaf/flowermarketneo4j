import { useEffect, useState } from "react";
import axios from "axios";
import "../styles/page.css";
import plantImage from "../assets/plant.png";

export default function OrdersPage() {
    const [orders, setOrders] = useState([]);

    useEffect(() => {
        axios.get("https://localhost:7254/api/prestataire/orders/my")
            .then(res => setOrders(res.data))
            .catch(err => console.log(err));
    }, []);

    return (
        <div className="page-container">
            <img src={plantImage} className="plant-bg" />

            <h1 className="page-title">Commandes</h1>

            <div className="page-grid">
                {orders.map((o) => (
                    <div className="page-card" key={o.id}>
                        <h3>Commande #{o.id}</h3>
                        <p>Client : {o.customerName}</p>
                        <p>Total : {o.total} MAD</p>
                        <p>Status : {o.status}</p>
                    </div>
                ))}
            </div>
        </div>
    );
}
