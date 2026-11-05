import axiosClient from "./axios.client"; // axios instance đã gắn JWT và interceptor
import { validateImageFile } from "../utils/imageValidator";

/**
 * VITE_USE_MOCK mặc định là true. Khi nối backend, đặt VITE_USE_MOCK=false.
 *
 * Quy ước API (ASP.NET Core):
 *   GET    /admin/stats                                   -> { revenue, orders, customers } mỗi mục { value, growth }
 *   GET    /admin/orders?page&pageSize&status&keyword     -> { items, total }
 *   GET    /admin/products?page&pageSize&category&keyword -> { items, total }
 *   POST   /admin/products                                -> product
 *   PUT    /admin/products/{id}                           -> product
 *   DELETE /admin/products/{id}                           -> 204
 *   POST   /admin/uploads (multipart, field "file")       -> { url }
 */
const USE_MOCK = import.meta.env.VITE_USE_MOCK !== "false";

/* =====================================================================
   MOCK
   ===================================================================== */
const delay = (ms = 400) => new Promise((resolve) => setTimeout(resolve, ms));

const normalize = (s = "") =>
    s.normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/đ/g, "d").toLowerCase();

const paginate = (list, page = 1, pageSize = 5) => ({
    items: list.slice((page - 1) * pageSize, page * pageSize).map((x) => ({ ...x })),
    total: list.length,
});

const CUSTOMERS = [
    "Nguyễn Minh Anh", "Trần Quốc Bảo", "Lê Thu Hà", "Phạm Đức Long", "Vũ Ngọc Mai",
    "Đặng Hoài Nam", "Hoàng Thảo Vy", "Bùi Khánh Linh", "Ngô Gia Huy", "Đỗ Phương Thảo",
];
const STATUS_CYCLE = ["pending", "preparing", "delivering", "completed", "completed", "completed", "cancelled"];

const ordersDb = Array.from({ length: 26 }, (_, i) => ({
    code: `FD${482950 - i * 13}`,
    customer: CUSTOMERS[i % CUSTOMERS.length],
    createdAt: new Date(Date.UTC(2026, 10, 6, 2, 0) - i * 95 * 60000).toISOString(),
    total: 45000 + ((i * 37) % 11) * 23000,
    payment: i % 3 === 0 ? "bank" : "cod",
    status: STATUS_CYCLE[i % STATUS_CYCLE.length],
}));

const productsDb = [
    { id: 1, name: "Burger bò phô mai", category: "Đồ ăn nhanh", price: 59000, emoji: "🍔", bg: "#FFE0B2", description: "Bò nướng than, phô mai cheddar tan chảy, rau tươi và sốt đặc biệt." },
    { id: 2, name: "Pizza hải sản", category: "Đồ ăn nhanh", price: 129000, emoji: "🍕", bg: "#FFCCBC", description: "Đế mỏng giòn, tôm, mực, nghêu và phô mai mozzarella." },
    { id: 3, name: "Gà rán giòn cay", category: "Đồ ăn nhanh", price: 75000, emoji: "🍗", bg: "#FFE0B2", description: "Gà tẩm bột chiên giòn, ướp cay vừa phải, kèm sốt mayonnaise." },
    { id: 4, name: "Khoai tây chiên", category: "Đồ ăn nhanh", price: 29000, emoji: "🍟", bg: "#FFF3C4", description: "Khoai cắt sợi chiên vàng, rắc muối biển, kèm tương cà." },
    { id: 5, name: "Cơm tấm sườn bì chả", category: "Món chính", price: 55000, emoji: "🍚", bg: "#FFF3C4", description: "Sườn nướng mật ong, bì, chả trứng và nước mắm chua ngọt." },
    { id: 6, name: "Phở bò tái nạm", category: "Món chính", price: 60000, emoji: "🍜", bg: "#FFE0B2", description: "Nước dùng hầm xương 12 tiếng, bánh phở mềm, thịt bò tái và nạm." },
    { id: 7, name: "Mì Ý sốt bò bằm", category: "Món chính", price: 69000, emoji: "🍝", bg: "#FFCCBC", description: "Mì Ý al dente, sốt cà chua bò bằm, rắc phô mai parmesan." },
    { id: 8, name: "Trà đào cam sả", category: "Đồ uống", price: 35000, emoji: "🍑", bg: "#FFE0B2", description: "Trà đen ủ lạnh, đào miếng, cam tươi và sả thơm." },
    { id: 9, name: "Cà phê sữa đá", category: "Đồ uống", price: 29000, emoji: "☕", bg: "#D7CCC8", description: "Cà phê phin đậm vị pha cùng sữa đặc, đá viên." },
    { id: 10, name: "Sinh tố bơ", category: "Đồ uống", price: 42000, emoji: "🥑", bg: "#DCEDC8", description: "Bơ sáp xay cùng sữa tươi, béo mịn, ít ngọt." },
    { id: 11, name: "Kem dâu tây", category: "Tráng miệng", price: 32000, emoji: "🍨", bg: "#F8BBD0", description: "Kem tươi mát lạnh vị dâu, topping dâu tây thái lát." },
    { id: 12, name: "Bánh flan caramel", category: "Tráng miệng", price: 22000, emoji: "🍮", bg: "#FFF3C4", description: "Flan trứng sữa mịn như lụa, rưới caramel đắng nhẹ." },
];

const readAsDataURL = (file) =>
    new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result);
        reader.onerror = () => reject(new Error("Không đọc được file."));
        reader.readAsDataURL(file);
    });

const mockApi = {
    async getStats() {
        await delay();
        const completed = ordersDb.filter((o) => o.status === "completed");
        return {
            revenue: { value: completed.reduce((sum, o) => sum + o.total, 0), growth: 12.4 },
            orders: { value: ordersDb.length, growth: 8.1 },
            customers: { value: new Set(ordersDb.map((o) => o.customer)).size, growth: -2.3 },
        };
    },

    async getOrders({ page, pageSize, status, keyword } = {}) {
        await delay();
        const q = normalize(keyword);
        const list = ordersDb.filter(
            (o) =>
                (!status || o.status === status) &&
                (!q || normalize(o.code).includes(q) || normalize(o.customer).includes(q))
        );
        return paginate(list, page, pageSize);
    },

    async getProducts({ page, pageSize, category, keyword } = {}) {
        await delay();
        const q = normalize(keyword);
        const list = productsDb.filter(
            (p) => (!category || p.category === category) && (!q || normalize(p.name).includes(q))
        );
        return paginate(list, page, pageSize);
    },

    async createProduct(payload) {
        await delay();
        const product = { ...payload, id: Math.max(0, ...productsDb.map((p) => p.id)) + 1 };
        productsDb.unshift(product);
        return { ...product };
    },

    async updateProduct(id, payload) {
        await delay();
        const found = productsDb.find((p) => p.id === id);
        if (!found) throw new Error("Món ăn không tồn tại hoặc đã bị xóa.");
        Object.assign(found, payload);
        return { ...found };
    },

    async deleteProduct(id) {
        await delay();
        const index = productsDb.findIndex((p) => p.id === id);
        if (index === -1) throw new Error("Món ăn không tồn tại hoặc đã bị xóa.");
        productsDb.splice(index, 1);
    },

    async uploadImage(file) {
        await delay(600);
        // Mô phỏng việc backend kiểm tra lại file
        const result = await validateImageFile(file);
        if (!result.ok) throw new Error(result.message);
        return { url: await readAsDataURL(file) };
    },
};

/* =====================================================================
   API THẬT
   ===================================================================== */
const realApi = {
    getStats: () => axiosClient.get("/admin/stats"),
    getOrders: (params) => axiosClient.get("/admin/orders", { params }),
    getProducts: (params) => axiosClient.get("/admin/products", { params }),
    createProduct: (payload) => axiosClient.post("/admin/products", payload),
    updateProduct: (id, payload) => axiosClient.put(`/admin/products/${id}`, payload),
    deleteProduct: (id) => axiosClient.delete(`/admin/products/${id}`),
    uploadImage: (file) => {
        const formData = new FormData();
        formData.append("file", file);
        return axiosClient.post("/admin/uploads", formData, {
            headers: { "Content-Type": "multipart/form-data" },
        });
    },
};

export const adminService = USE_MOCK ? mockApi : realApi;
