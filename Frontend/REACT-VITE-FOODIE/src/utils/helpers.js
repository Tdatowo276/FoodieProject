const TOKEN_KEY = "foodie_token";
const USER_KEY = "foodie_user";
const CART_KEY = "foodie_cart";
const MOCK_USERS_KEY = "foodie_mock_users";

const SESSION_EXPIRED_EVENT = "foodie:session-expired";

const SHIPPING_FEE = 15000;
const FREE_SHIP_FROM = 200000;

// Đỏ tươi chủ đạo + nền trắng
const PRIMARY = "#E3121B";
const PRIMARY_DARK = "#B80D14";
// Tiêu đề: chữ đậm, gọn (không dùng font tròn kiểu "hoạt hình")
const display = { fontFamily: "'Be Vietnam Pro', system-ui, sans-serif", fontWeight: 800, letterSpacing: "-0.01em" };
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

// Trang quản trị (ASP.NET Core MVC) chạy trên backend, đăng nhập riêng bằng cookie
export const ADMIN_URL = `${API_ORIGIN}/Admin`;

// Trạng thái đơn hàng (khớp OrderStatuses phía backend)
export const ORDER_STATUS = {
    pending: { label: "Chờ xác nhận", color: "gold" },
    preparing: { label: "Đang chuẩn bị", color: "blue" },
    delivering: { label: "Đang giao", color: "geekblue" },
    completed: { label: "Hoàn thành", color: "green" },
    cancelled: { label: "Đã hủy", color: "default" },
};

export const formatDateTime = (iso) =>
    new Date(iso).toLocaleString("vi-VN", { dateStyle: "short", timeStyle: "short" });

export {
    TOKEN_KEY,
    USER_KEY,
    CART_KEY,
    MOCK_USERS_KEY,
    SESSION_EXPIRED_EVENT,
    SHIPPING_FEE,
    FREE_SHIP_FROM,
    PRIMARY,
    PRIMARY_DARK,
    display,
    formatVND,
    removeAccents,
    delay,
    storage
}