const TOKEN_KEY = "foodie_token";
const USER_KEY = "foodie_user";
const CART_KEY = "foodie_cart";
const MOCK_USERS_KEY = "foodie_mock_users";

const CATEGORIES = ["Tất cả", "Đồ ăn nhanh", "Món chính", "Đồ uống", "Tráng miệng"];
const SHIPPING_FEE = 15000;
const FREE_SHIP_FROM = 200000;

const PRIMARY = "#E8321E";
const display = { fontFamily: "'Baloo 2', 'Be Vietnam Pro', sans-serif", fontWeight: 800 };
const formatVND = (n) => Number(n).toLocaleString("vi-VN") + "đ";

const removeAccents = (s) =>
    s.normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/đ/g, "d").toLowerCase();

const delay = (ms = 400) => new Promise((r) => setTimeout(r, ms));

const storage = {
  get(key, fallback = null) {
    try {
      const raw = localStorage.getItem(key);
      if (!raw) return fallback;
      try {
        return JSON.parse(raw);
      } catch {
        return raw; // Trả về chuỗi thuần nếu không phải JSON (dành cho TOKEN_KEY)
      }
    } catch {
      return fallback;
    }
  },
  set(key, value) {
    try {
      // Nếu value là chuỗi thuần (như Token), lưu thẳng string. Nếu là Object/Array thì JSON.stringify
      const data = typeof value === 'string' ? value : JSON.stringify(value);
      localStorage.setItem(key, data);
    } catch {
      /* bỏ qua nếu trình duyệt chặn storage */
    }
  },
  remove(key) {
    try {
      localStorage.removeItem(key);
    } catch {
      /* bỏ qua */
    }
  },
};
const API_ORIGIN = new URL(import.meta.env.VITE_API_URL).origin;
export const resolveImageUrl = (url) => (url?.startsWith("/uploads/") ? API_ORIGIN + url : url);

export {
    TOKEN_KEY,
    USER_KEY,
    CART_KEY,
    MOCK_USERS_KEY,
    CATEGORIES,
    SHIPPING_FEE,
    FREE_SHIP_FROM,
    PRIMARY,
    display,
    formatVND,
    removeAccents,
    delay,
    storage
}