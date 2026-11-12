# Thư viện ảnh món ăn

Ảnh trong thư mục này **được đưa lên Git**, cả nhóm clone về đều có.
Trong CSDL, món ăn chỉ lưu đường dẫn, ví dụ `/images/products/pho-bo-tai-nam.png`.

## Quy tắc đặt tên file

- Chỉ dùng **chữ thường không dấu, số và dấu gạch ngang** `-` (không khoảng trắng, không dấu tiếng Việt).
- Đuôi: `.png`, `.jpg`, `.jpeg` hoặc `.webp`.
- File đặt sai tên sẽ **không hiện** trong danh sách chọn ảnh (vì lý do bảo mật).
- Nên dùng ảnh đã xóa nền, khoảng **800 x 800 px**, dung lượng **dưới 300KB** để repo không quá nặng.

## Gắn ảnh cho món

Có 3 cách:

1. **Trang Admin → Món ăn → Sửa** → ô *"Hoặc chọn ảnh có sẵn"* → chọn file → Lưu.
2. **Trang Admin → Món ăn → nút "Tự gắn ảnh theo tên"**: các món *chưa có ảnh* tự nhận file có tên trùng tên món.
3. **Tạo CSDL mới** (máy mới clone, hoặc xóa DB tạo lại): 15 món mẫu tự được gắn đúng ảnh theo bảng dưới
   (khai báo trong `Data/Seeder.cs`).

## Ảnh của 15 món mẫu

| Món | Danh mục | File ảnh |
|---|---|---|
| Mỳ ý sốt cay vừa | Món chính | `my-y-bo-bam.png` |
| Salad rau củ | Đồ ăn nhanh | `salad-rau-cu.png` |
| Viên phô mai | Đồ ăn nhanh | `vien-pho-mai.png` |
| Gà Chiên Giòn | Món chính | `ga-ran-thuong.png` |
| Burger bò | Món chính | `burger-bo.png` |
| Burger gà | Món chính | `burger-ga.png` |
| Gà sốt mật ong | Món chính | `ga-ran-sot-mat-ong.png` |
| Trà Chanh Lipton | Đồ uống | `tra-lipton.png` |
| Pepsi | Đồ uống | `pepsi.png` |
| Mirinda | Đồ uống | `mirinda.png` |
| Kem Vani | Tráng miệng | `kem-vani.png` |
| Gà giòn nguyên con | Món chính | `ga-ran-nua-con.png` |
| Combo Burger Gà Giòn | Combo Siêu Ưu Đãi | `combo-burger-ga-them-pepsi.png` |
| Combo Burger | Combo Siêu Ưu Đãi | `combo-2-burger-them-pepsi.png` |
| Cơm trắng | Đồ ăn kèm | `com-trang.png` |

Muốn đổi ảnh của món mẫu: thay file cùng tên ở thư mục này (không cần sửa code).
Nút "Tự gắn ảnh theo tên" dùng quy tắc tên món → tên file: bỏ dấu, viết thường, thay khoảng trắng bằng `-`
(ví dụ "Burger gà" → `burger-ga.png`).

⚠️ Không đổi tên / xóa file đang được món sử dụng: món đó sẽ mất ảnh (CSDL vẫn trỏ tới tên cũ).
