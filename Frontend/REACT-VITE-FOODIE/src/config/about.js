/**
 * Nội dung trang "Về chúng tôi". Sửa trực tiếp file này, không cần đụng tới code giao diện.
 *  - Trường nào để trống ("") sẽ tự ẩn trên trang.
 *  - Ảnh thành viên: chép vào public/team/ rồi ghi đường dẫn, ví dụ "/team/dat.jpg"
 *    (ảnh vuông, khoảng 400x400). Không có ảnh thì hiện chữ cái đầu của tên.
 */

export const project = {
    name: "Foodie",
    tagline: "Đặt món nhanh, giao nóng hổi tận nơi",
    // Thông tin đồ án (điền nếu muốn hiện trên trang)
    course: "",        // ví dụ: "Lập trình Web với ASP.NET Core"
    school: "",        // ví dụ: tên trường / khoa
    lecturer: "",      // ví dụ: tên giảng viên hướng dẫn
    year: "2026",
};

export const team = [
    {
        name: "Nguyễn Tiến Đạt",
        role: "",          // ví dụ: "Trưởng nhóm · Back-end"
        studentId: "",     // mã số sinh viên
        email: "",
        avatar: "",        // ví dụ: "/team/dat.jpg"
        tasks: [],         // ví dụ: ["Thiết kế CSDL", "Web API & Swagger"]
    },
    {
        name: "Trương Thị Luyến",
        role: "",
        studentId: "",
        email: "",
        avatar: "",
        tasks: [],
    },
];
