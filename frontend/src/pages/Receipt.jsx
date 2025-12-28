import React from "react";
import jsPDF from "jspdf";
import "./Receipt.css";

const Receipt = ({ orderData, onClose }) => {
  if (!orderData) return null;

  const handleDownloadPDF = () => {
    const doc = new jsPDF();

    // -- COLORS --
    const primaryColor = [16, 185, 129]; // #10b981
    const darkColor = [31, 41, 55]; // #1f2937
    const grayColor = [107, 114, 128]; // #6b7280

    // -- HEADER (Minimalist) --
    doc.setTextColor(...darkColor);
    doc.setFont("helvetica", "bold");
    doc.setFontSize(28);
    doc.text("FlowerMarket", 20, 25);

    doc.setFontSize(10);
    doc.setFont("helvetica", "normal");
    doc.setTextColor(...grayColor);
    doc.text("PREMIUM FLORAL SOLUTIONS", 20, 32);

    // Order Info (Right)
    doc.setTextColor(...darkColor);
    doc.setFont("helvetica", "bold");
    doc.text(`REÇU #${orderData.orderNumber}`, 190, 25, { align: "right" });
    doc.setFont("helvetica", "normal");
    doc.setTextColor(...grayColor);
    doc.text(`Date: ${orderData.date}`, 190, 32, { align: "right" });

    doc.setDrawColor(229, 231, 235);
    doc.line(20, 40, 190, 40);

    let y = 55;

    // -- INFO SECTION --
    doc.setFontSize(9);
    doc.setTextColor(...grayColor);
    doc.text("DÉTAILS DU PRESTATAIRE", 20, y);
    doc.text("DÉTAILS DU CLIENT", 120, y);

    y += 7;
    doc.setFontSize(11);
    doc.setTextColor(...darkColor);
    doc.setFont("helvetica", "bold");
    doc.text(orderData.shopName || "FlowerMarket", 20, y);
    doc.text(orderData.customerName || "Client", 120, y);

    y += 5;
    doc.setFont("helvetica", "normal");
    doc.setFontSize(10);
    doc.setTextColor(...grayColor);
    doc.text(orderData.shopEmail || "admin@flowermarket.ma", 20, y);
    doc.text(`Paiement: ${orderData.paymentMethod}`, 120, y);

    y += 20;

    // -- TABLE HEADER --
    doc.setFillColor(249, 250, 251);
    doc.rect(20, y, 170, 10, "F");

    doc.setFont("helvetica", "bold");
    doc.setFontSize(9);
    doc.setTextColor(...grayColor);
    doc.text("ARTICLE", 25, y + 7);
    doc.text("QUANTITÉ", 130, y + 7);
    doc.text("PRIX UNITAIRE", 170, y + 7, { align: "right" });

    y += 18;

    // -- ITEMS --
    doc.setFont("helvetica", "normal");
    doc.setFontSize(10);
    doc.setTextColor(...darkColor);

    orderData.items.forEach((item) => {
      doc.text(item.name, 25, y);
      doc.text(`${item.quantity}`, 135, y, { align: "center" });
      doc.text(`${item.price} MAD`, 190, y, { align: "right" });
      y += 10;
    });

    y += 5;
    doc.line(20, y, 190, y);
    y += 12;

    // -- TOTALS --
    const labelsX = 135;
    const valuesX = 190;

    doc.setFontSize(10);
    doc.setTextColor(...grayColor);
    doc.text("Sous-total", labelsX, y);
    doc.text(`${orderData.subtotal.toFixed(2)} MAD`, valuesX, y, { align: "right" });

    y += 7;
    doc.text("TVA (13%)", labelsX, y);
    doc.text(`${orderData.tax.toFixed(2)} MAD`, valuesX, y, { align: "right" });

    y += 12;
    doc.setFontSize(16);
    doc.setTextColor(...primaryColor);
    doc.setFont("helvetica", "bold");
    doc.text("TOTAL :", labelsX, y);
    doc.text(`${orderData.total.toFixed(2)} MAD`, valuesX, y, { align: "right" });

    // -- FOOTER --
    doc.setFontSize(9);
    doc.setTextColor(...grayColor);
    doc.setFont("helvetica", "italic");
    doc.text("Merci pour votre confiance chez FlowerMarket.", 105, 270, { align: "center" });
    doc.setFont("helvetica", "normal");
    doc.text("Ce document est un reçu de paiement électronique.", 105, 275, { align: "center" });

    doc.save(`Recu_${orderData.orderNumber}.pdf`);
  };

  return (
    <div className="modal-overlay">
      <div className="modal-content receipt-box">
        <button className="close-modal-btn" onClick={onClose}>
          &times;
        </button>

        <h2 className="receipt-title">Confirmation de Paiement</h2>

        <div className="receipt-info">
          <div>
            <b>Commande</b>
            <p>#{orderData.orderNumber}</p>
            <b>Date</b>
            <p>{orderData.date} à {orderData.time}</p>
          </div>
          <div>
            <b>Méthode</b>
            <p>{orderData.paymentMethod}</p>
            <b>Client</b>
            <p>{orderData.customerName}</p>
          </div>
        </div>

        <div className="receipt-items">
          {orderData.items.map((item, i) => (
            <div key={i} className="receipt-item">
              <span>{item.name} <small style={{ color: '#9ca3af' }}>x{item.quantity}</small></span>
              <span style={{ fontWeight: '600' }}>{item.price} MAD</span>
            </div>
          ))}
        </div>

        <div className="receipt-total">
          <p>Sous-total : {orderData.subtotal.toFixed(2)} MAD</p>
          <p>TVA (13%) : {orderData.tax.toFixed(2)} MAD</p>
          <h3>TOTAL : {orderData.total.toFixed(2)} MAD</h3>
        </div>

        <button className="btn-download" onClick={handleDownloadPDF}>
          <span>📄</span> Télécharger le reçu officiel
        </button>

        <button className="btn-primary" onClick={onClose}>
          Retour aux achats
        </button>
      </div>
    </div>
  );
};

export default Receipt;
