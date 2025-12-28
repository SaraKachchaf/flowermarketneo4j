import axios from "axios";

const API_URL = "https://localhost:5001/api/graph";

export const fetchOrdersGraph = async () => {
  const response = await axios.get(`${API_URL}/orders`);
  return response.data;
};
