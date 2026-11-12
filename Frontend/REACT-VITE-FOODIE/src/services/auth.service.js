import axiosClient from "./axios.client";
import { delay, MOCK_USERS_KEY, USER_KEY, storage } from "../utils/helpers";

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

    // GET /api/auth/me  =>  { id, name, email, role }
    // Gọi khi mở trang để kiểm tra token còn hợp lệ (hết hạn / tài khoản bị khóa => 401)
    async me() {
        if (USE_MOCK) {
            await delay(100);
            return storage.get(USER_KEY, null);
        }
        return axiosClient.get("/auth/me");
    },
};

const orderService = {
    // POST /api/orders  =>  { orderCode, total, status, createdAt }
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

    // GET /api/orders/me?page=&pageSize=  =>  { items: [{ code, createdAt, total, status, itemCount }], total, page, pageSize }
    async getMine({ page = 1, pageSize = 10 } = {}) {
        if (USE_MOCK) {
            await delay();
            return { items: [], total: 0, page, pageSize };
        }
        return axiosClient.get("/orders/me", { params: { page, pageSize } });
    },

    // GET /api/orders/{code}  =>  chi tiết đơn (chỉ xem được đơn của chính mình)
    async getByCode(code) {
        if (USE_MOCK) {
            await delay();
            throw new Error("Chế độ dữ liệu mẫu không có chi tiết đơn hàng.");
        }
        return axiosClient.get(`/orders/${encodeURIComponent(code)}`);
    },
};

export { authService, orderService }