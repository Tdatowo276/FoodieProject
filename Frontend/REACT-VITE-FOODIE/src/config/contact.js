/**
 * Thông tin liên hệ của shop: dùng cho footer, bản đồ và các nút liên hệ nổi.
 *
 * Tọa độ bản đồ (lat, lng): giá trị dưới đây là ƯỚC LƯỢNG khu vực ngõ 10 Láng Hạ.
 * Cách lấy tọa độ chính xác: mở Google Maps → bấm chuột phải đúng vị trí shop →
 * bấm dòng số đầu tiên (ví dụ 21.0207, 105.8148) để sao chép → dán vào lat, lng bên dưới.
 */
const contact = {
    shopName: "Foodie",
    address: "54 ngõ 10 Láng Hạ, Hà Nội",
    phone: "0974679274",            // dùng cho liên kết gọi điện
    phoneDisplay: "0974 679 274",   // dùng để hiển thị
    email: "WupyTdat@gmail.com",
    openingHours: "8:00 - 22:00 hằng ngày",
    lat: 21.0207,
    lng: 105.8148,
};

/** Mở chỉ đường trên Google Maps theo địa chỉ (chính xác kể cả khi tọa độ ước lượng lệch). */
export const googleMapsUrl = `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(contact.address)}`;

export default contact;
