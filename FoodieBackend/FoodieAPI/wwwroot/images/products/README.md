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
3. **Tạo CSDL mới** (máy mới clone, hoặc xóa DB tạo lại): 13 món mẫu tự được gắn ảnh nếu có file đúng tên bên dưới.

## Tên file cho 13 món mẫu

| Món | Tên file (đuôi .png / .jpg / .jpeg / .webp) |
|---|---|
| Burger bò phô mai | `burger-bo-pho-mai` |
| Pizza hải sản | `pizza-hai-san` |
| Gà rán giòn cay | `ga-ran-gion-cay` |
| Khoai tây chiên | `khoai-tay-chien` |
| Cơm tấm sườn bì chả | `com-tam-suon-bi-cha` |
| Phở bò tái nạm | `pho-bo-tai-nam` |
| Mì Ý sốt bò bằm | `mi-y-sot-bo-bam` |
| Sushi tổng hợp 12 miếng | `sushi-tong-hop-12-mieng` |
| Trà đào cam sả | `tra-dao-cam-sa` |
| Cà phê sữa đá | `ca-phe-sua-da` |
| Sinh tố bơ | `sinh-to-bo` |
| Kem dâu tây | `kem-dau-tay` |
| Bánh flan caramel | `banh-flan-caramel` |

Món mới do admin thêm cũng theo quy tắc trên: bỏ dấu, viết thường, thay khoảng trắng bằng `-`.

⚠️ Không đổi tên / xóa file đang được món sử dụng: món đó sẽ mất ảnh (CSDL vẫn trỏ tới tên cũ).
