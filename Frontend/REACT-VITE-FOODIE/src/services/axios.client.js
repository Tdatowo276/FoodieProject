import axios from "axios";
import { TOKEN_KEY, USER_KEY, storage } from "../utils/helpers";
const API_URL = import.meta.env.VITE_API_URL || "https://localhost:7000/api";
const axiosClient = axios.create({
    baseURL: API_URL,
    timeout: 10000,
    headers: { "Content-Type": "application/json" },
});

axiosClient.interceptors.request.use((config) => {
    const token = localStorage.getItem(TOKEN_KEY);
    if (token) config.headers.Authorization = `Bearer ${token}`;
    return config;
});

axiosClient.interceptors.response.use(
    (response) => response.data,
    (error) => {
        if (error.response?.status === 401) {
            storage.remove(TOKEN_KEY);
            storage.remove(USER_KEY);
        }

        const data = error.response?.data;
        let message = "Có lỗi xảy ra, vui lòng thử lại.";

        if (error.code === "ECONNABORTED") message = "Máy chủ phản hồi quá chậm.";
        else if (error.message === "Network Error") message = "Không kết nối được máy chủ. Kiểm tra backend và CORS.";
        else if (data?.message) message = data.message;
        else if (data?.errors) message = Object.values(data.errors).flat().join(" "); // ValidationProblemDetails của ASP.NET
        else if (data?.title) message = data.title; // ProblemDetails của ASP.NET

        return Promise.reject(new Error(message));
    }
);

export default axiosClient;