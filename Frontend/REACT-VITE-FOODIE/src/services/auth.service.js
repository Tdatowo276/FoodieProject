import axiosClient from "./axios.client";
import { delay, MOCK_USERS_KEY, storage } from "../utils/helpers";

const USE_MOCK = import.meta.env.VITE_USE_MOCK !== "false";

const getMockUsers = () => {
    const users = storage.get(MOCK_USERS_KEY, []);
    if (!users.some((u) => u.email === "demo@foodie.vn")) {
        users.push({ name: "Khách Demo", email: "demo@foodie.vn", password: "123456" });
    }
    return users;
};
const authService = {
    // POST /api/auth/login  =>  { token, user: { name, email } }
    async login({ email, password }) {
        if (USE_MOCK) {
            await delay();
            const found = getMockUsers().find((u) => u.email.toLowerCase() === email.toLowerCase());
            if (!found) throw new Error("Email này chưa được đăng ký.");
            if (found.password !== password) throw new Error("Mật khẩu chưa đúng.");
            return { token: "mock-jwt-token", user: { name: found.name, email: found.email } };
        }
        return axiosClient.post("/auth/login", { email, password });
    },

    // POST /api/auth/register  =>  { token, user: { name, email } }
    async register({ name, email, password }) {
        if (USE_MOCK) {
            await delay();
            const users = getMockUsers();
            if (users.some((u) => u.email.toLowerCase() === email.toLowerCase())) {
                throw new Error("Email này đã có tài khoản. Hãy đăng nhập.");
            }
            storage.set(MOCK_USERS_KEY, [...users, { name, email, password }]);
            return { token: "mock-jwt-token", user: { name, email } };
        }
        return axiosClient.post("/auth/register", { name, email, password });
    },
};

const orderService = {
    // POST /api/orders  =>  { orderCode, total }
    async create(payload) {
        if (USE_MOCK) {
            await delay(700);
            return {
                orderCode: "FD" + Math.floor(100000 + Math.random() * 900000),
                total: payload.total,
            };
        }
        return axiosClient.post("/orders", payload);
    },
};

export { authService, orderService }