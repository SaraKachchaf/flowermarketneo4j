import { useEffect, useState } from "react";
import axios from "axios";
import "../styles/page.css";

export default function PromotionsPage() {
    const [promos, setPromos] = useState([]);

    useEffect(() => {
        axios.get("https://localhost:7254/api/prestataire/promotions/my/")
            .then(res => setPromos(res.data))
            .catch(err => console.log(err));
    }, []);

    return (
        <div className="page-container">
            <img src={plantImage} className="plant-bg" />

            <h1 className="page-title">Promotions</h1>

            <div className="page-grid">
                {promos.map((promo) => (
                    <div className="page-card" key={promo.id}>
                        <h3>{promo.title}</h3>
                        <p>{promo.description}</p>
                        <p>-{promo.discountPercent}%</p>
                        <p>Du {promo.startDate.slice(0,10)} au {promo.endDate.slice(0,10)}</p>
                    </div>
                ))}
            </div>
        </div>
    );
}
