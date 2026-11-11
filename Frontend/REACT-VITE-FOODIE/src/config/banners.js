/**
 * Ảnh banner trang chủ (carousel).
 *
 * Cách thay ảnh: chép ảnh vào thư mục  public/banners/  rồi sửa danh sách bên dưới.
 *  - Kích thước khuyên dùng: 1920 x 640 px (tỉ lệ 3:1), JPG/WebP, dưới 400KB.
 *  - Nội dung quan trọng (chữ, giá) đặt ở giữa ảnh vì trên điện thoại hai bên bị cắt bớt.
 *
 * Nếu file ảnh chưa có, banner tự hiện nền đỏ với tiêu đề/mô tả bên dưới (không bị vỡ khung).
 */
const banners = [
    {
        src: "/banners/banner-1.jpg",
        alt: "Combo gà rán và nước ngọt",
        title: "Gà giòn nóng hổi",
        subtitle: "Nấu mới khi có đơn, giao tận nơi trong 30 phút",
    },
    {
        src: "/banners/banner-2.jpg",
        alt: "Burger bò phô mai",
        title: "Burger bò phô mai",
        subtitle: "Bò nướng than, phô mai cheddar tan chảy",
    },
    {
        src: "/banners/banner-3.jpg",
        alt: "Miễn phí giao hàng",
        title: "Miễn phí giao hàng",
        subtitle: "Cho đơn từ 200.000đ trong nội thành",
    },
];

export default banners;
