import axiosClient from "./axios.client";
import { delay, removeAccents } from "../utils/helpers";

const MOCK_PRODUCTS = [
    { id: 1, name: "Burger bò phô mai", category: "Đồ ăn nhanh", price: 59000, rating: 4.8, emoji: "🍔", bg: "#FFE0B2", isFeatured: true, description: "Bò nướng than, phô mai cheddar tan chảy, rau tươi và sốt đặc biệt của quán." },
    { id: 2, name: "Pizza hải sản", category: "Đồ ăn nhanh", price: 129000, rating: 4.7, emoji: "🍕", bg: "#FFCCBC", isFeatured: true, description: "Đế mỏng giòn, tôm, mực, nghêu và phô mai mozzarella nướng vàng." },
    { id: 3, name: "Gà rán giòn cay", category: "Đồ ăn nhanh", price: 75000, rating: 4.9, emoji: "🍗", bg: "#FFE0B2", isFeatured: true, description: "Gà tẩm bột chiên giòn, ướp cay vừa phải, ăn kèm sốt mayonnaise." },
    { id: 4, name: "Khoai tây chiên", category: "Đồ ăn nhanh", price: 29000, rating: 4.5, emoji: "🍟", bg: "#FFF3C4", isFeatured: false, description: "Khoai cắt sợi chiên vàng, rắc muối biển, kèm tương cà." },
    { id: 5, name: "Hot dog xúc xích Đức", category: "Đồ ăn nhanh", price: 45000, rating: 4.4, emoji: "🌭", bg: "#FFCCBC", isFeatured: false, description: "Xúc xích Đức nướng, bánh mì mềm, hành phi và mù tạt." },
    { id: 6, name: "Cơm tấm sườn bì chả", category: "Món chính", price: 55000, rating: 4.9, emoji: "🍚", bg: "#FFF3C4", isFeatured: true, description: "Sườn nướng mật ong, bì, chả trứng và nước mắm chua ngọt." },
    { id: 7, name: "Phở bò tái nạm", category: "Món chính", price: 60000, rating: 4.8, emoji: "🍜", bg: "#FFE0B2", isFeatured: true, description: "Nước dùng hầm xương 12 tiếng, bánh phở mềm, thịt bò tái và nạm." },
    { id: 8, name: "Mì Ý sốt bò bằm", category: "Món chính", price: 69000, rating: 4.6, emoji: "🍝", bg: "#FFCCBC", isFeatured: false, description: "Mì Ý al dente, sốt cà chua bò bằm, rắc phô mai parmesan." },
    { id: 9, name: "Sushi tổng hợp 12 miếng", category: "Món chính", price: 159000, rating: 4.7, emoji: "🍣", bg: "#FFE0B2", isFeatured: false, description: "Cá hồi, cá ngừ, tôm và trứng cá, kèm wasabi và nước tương." },
    { id: 10, name: "Trà đào cam sả", category: "Đồ uống", price: 35000, rating: 4.8, emoji: "🍑", bg: "#FFE0B2", isFeatured: true, description: "Trà đen ủ lạnh, đào miếng, cam tươi và sả thơm." },
    { id: 11, name: "Cà phê sữa đá", category: "Đồ uống", price: 29000, rating: 4.9, emoji: "☕", bg: "#D7CCC8", isFeatured: false, description: "Cà phê phin đậm vị pha cùng sữa đặc, đá viên." },
    { id: 12, name: "Nước ép cam tươi", category: "Đồ uống", price: 39000, rating: 4.6, emoji: "🍊", bg: "#FFE0B2", isFeatured: false, description: "Cam vắt nguyên chất, không đường, giữ trọn vitamin C." },
    { id: 13, name: "Sinh tố bơ", category: "Đồ uống", price: 42000, rating: 4.7, emoji: "🥑", bg: "#DCEDC8", isFeatured: false, description: "Bơ sáp xay cùng sữa tươi, béo mịn, ít ngọt." },
    { id: 14, name: "Kem dâu tây", category: "Tráng miệng", price: 32000, rating: 4.5, emoji: "🍨", bg: "#F8BBD0", isFeatured: false, description: "Kem tươi mát lạnh vị dâu, topping dâu tây thái lát." },
    { id: 15, name: "Bánh donut phủ socola", category: "Tráng miệng", price: 25000, rating: 4.6, emoji: "🍩", bg: "#D7CCC8", isFeatured: false, description: "Bánh mềm xốp phủ socola đen và hạt cốm nhiều màu." },
    { id: 16, name: "Bánh flan caramel", category: "Tráng miệng", price: 22000, rating: 4.8, emoji: "🍮", bg: "#FFF3C4", isFeatured: true, description: "Flan trứng sữa mịn như lụa, rưới caramel đắng nhẹ." },
];
const USE_MOCK = import.meta.env.VITE_USE_MOCK !== "false";
const productService = {
    // GET /api/products?keyword=&categoryId=
    async getAll(params = {}) {
        if (USE_MOCK) {
            await delay();
            const q = removeAccents(params.keyword || "");
            return MOCK_PRODUCTS.filter(
                (p) =>
                    (!params.category || p.category === params.category) &&
                    (!params.categoryId || p.category === [...new Set(MOCK_PRODUCTS.map((x) => x.category))][params.categoryId - 1]) &&
                    removeAccents(p.name).includes(q)
            );
        }
        return axiosClient.get("/products", { params });
    },
    async getById(id) {
        if (USE_MOCK) {
            await delay();
            return MOCK_PRODUCTS.find((p) => String(p.id) === String(id)) || null;
        }
        return axiosClient.get(`/products/${id}`);
    },

    // GET /api/products/featured
    async getFeatured() {
        if (USE_MOCK) {
            await delay();
            return MOCK_PRODUCTS.filter((p) => p.isFeatured);
        }
        return axiosClient.get("/products/featured");
    },

    // GET /api/products/best-sellers  =>  [{ productId, name, imageUrl, quantity, revenue }]
    // Món bán chạy 30 ngày qua (chỉ tính đơn đã hoàn thành)
    async getBestSellers(take = 8) {
        if (USE_MOCK) return [];
        return axiosClient.get("/products/best-sellers", { params: { take } });
    },
};

const categoryService = {
    // GET /api/categories  =>  [{ id, name, description, isActive, productCount }]
    // Danh mục do admin quản lý trên trang /Admin; chỉ trả danh mục đang hiển thị
    async getAll() {
        if (USE_MOCK) {
            await delay(150);
            const names = [...new Set(MOCK_PRODUCTS.map((p) => p.category))];
            return names.map((name, i) => ({
                id: i + 1,
                name,
                productCount: MOCK_PRODUCTS.filter((p) => p.category === name).length,
            }));
        }
        return axiosClient.get("/categories");
    },
};

export { productService, categoryService, MOCK_PRODUCTS }