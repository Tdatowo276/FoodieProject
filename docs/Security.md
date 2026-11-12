# Bảo mật: Foodie

Đối chiếu từng yêu cầu bảo mật của đề (mục 3.5) với cách hệ thống đáp ứng, vị trí trong mã nguồn và test chứng minh
(mã test case xem `docs/TestCases.md`). Đường dẫn mã nguồn tính từ `FoodieBackend/FoodieAPI/`.

## 1. Đối chiếu yêu cầu của đề

| Yêu cầu (mục 3.5) | Cách đáp ứng | Mã nguồn | Test |
|---|---|---|---|
| **Xác thực và phân quyền phía server** | JWT (API) và Cookie (trang quản trị), tách biệt theo đường dẫn. Policy `Admin` bắt buộc vai trò Admin. Mỗi request kiểm tra lại tài khoản trong CSDL (chưa khóa, vai trò không đổi) | `Common/AuthenticationSetup.cs`, `Areas/Admin/Controllers/AdminControllerBase.cs` | TC-AZ-01 → 05, TC-MVC-01, TC-MVC-05 |
| **Lưu mật khẩu an toàn** | **BCrypt** (có salt, chậm có chủ đích). Không lưu và không ghi log mật khẩu gốc. Đổi mật khẩu phải nhập đúng mật khẩu hiện tại | `Services/AuthService.cs`, `Services/ProfileService.cs` | TC-AUTH-05, TC-PRF-03 |
| **Chống CSRF** | Trang quản trị: `AutoValidateAntiforgeryToken` toàn cục, AJAX gửi token qua header `X-CSRF-TOKEN`; cookie `SameSite=Lax`. API dùng JWT trong header (trình duyệt không tự gửi) nên không bị CSRF; cookie admin **không** có hiệu lực với `/api` | `Program.cs`, `wwwroot/admin/admin.js` | TC-MVC-02, TC-MVC-03, TC-AZ-02 |
| **Kiểm tra dữ liệu phía server** | Data Annotations trên DTO / ViewModel, kiểm tra cả phần tử lồng (`items[i].quantity`). Quy tắc nghiệp vụ kiểm tra lại trong Service. Phía trình duyệt chỉ để tiện người dùng | `Common/DataValidation.cs`, `DTOs/`, `Areas/Admin/Models/` | TC-VAL-01 → 08 |
| **Upload file an toàn** | Chỉ `.jpg/.jpeg/.png`; tối đa 2MB (kèm giới hạn request 3MB của Kestrel); **đọc magic bytes** để xác nhận ảnh thật và đuôi khớp nội dung; tên file do server đặt (GUID), chặn path traversal; file sai không được lưu; ảnh cũ được dọn | `Services/ImageStorage.cs` | TC-UP-01 → 08, TC-PRF-04 |
| **Không hardcode bí mật** | Chuỗi kết nối, khóa JWT, mật khẩu admin lấy từ **User Secrets** (Development) hoặc **biến môi trường** (Production). Thiếu cấu hình thì ứng dụng dừng và hướng dẫn, không dùng giá trị mặc định | `Program.cs` (`RequireSetting`), `scripts/setup-secrets.ps1` | TC-SYS-01 |
| **Không tin vai trò / quyền từ client** | Vai trò chỉ đọc từ token do server ký. Đăng ký luôn gán `Customer` (bỏ qua `role` client gửi). API cá nhân không có `{userId}` trên URL (lấy từ token) nên không sửa / xem được dữ liệu người khác. Đơn của người khác trả 404 | `Services/AuthService.cs`, `Endpoints/OrderEndpoints.cs`, `Endpoints/ProfileEndpoints.cs` | TC-AUTH-02, TC-ORD-08 |
| **Chống lỗ hổng phổ biến** | Xem mục 2 | | |

## 2. Các lỗ hổng phổ biến khác đã xử lý

| Lỗ hổng | Biện pháp | Test |
|---|---|---|
| **SQL Injection** | Mọi truy vấn qua EF Core / LINQ (tham số hóa), không ghép chuỗi SQL | Thiết kế |
| **XSS** | Razor và React tự mã hóa HTML khi hiển thị; nội dung người dùng (góp ý, đánh giá) không bao giờ render dạng HTML; Razor giữ chữ tiếng Việt nhưng vẫn mã hóa `< > & " '` | TC-MVC-08, TC-TOP-02 |
| **Dò mật khẩu (brute force)** | Rate limit 10 lần / phút / IP cho đăng nhập, đăng ký, đổi mật khẩu | TC-RL-01 |
| **Dò email đã đăng ký** | Sai email và sai mật khẩu trả **cùng một thông báo**; luôn chạy BCrypt (kể cả khi email không tồn tại) để thời gian phản hồi như nhau | TC-AUTH-07 |
| **Spam** | Góp ý: 5 lần / 10 phút / IP | TC-RL-02 |
| **Open Redirect** | Sau đăng nhập chỉ chuyển tới URL nội bộ (`Url.IsLocalUrl`) | TC-MVC-06 |
| **IDOR** (truy cập dữ liệu người khác) | Đơn hàng, hồ sơ, đánh giá luôn lọc theo người dùng trong token | TC-ORD-08 |
| **Sửa giá phía client** | Giá, phí ship, tổng tiền luôn tính lại phía server từ CSDL | TC-ORD-01 |
| **Path traversal qua đường dẫn ảnh** | Đường dẫn ảnh phải khớp mẫu chặt (`/uploads/<guid>` hoặc `/images/products/<chữ-thường-không-dấu>`) và file phải tồn tại | TC-BR-13, TC-PRD-11 |
| **Clickjacking, đoán kiểu file** | Header `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Referrer-Policy` | TC-MVC-11 |
| **Đánh cắp cookie** | Cookie `HttpOnly` (JavaScript không đọc được), `Secure` khi chạy HTTPS | TC-MVC-04 |
| **CORS** | Chỉ cho phép origin của trang React (`Cors:AllowedOrigins`) | Thiết kế |
| **Lộ thông tin lỗi** | Người dùng chỉ thấy thông báo chung + mã tra cứu; chi tiết lỗi chỉ ghi vào log | TC-MVC-10 |
| **Tài khoản bị chiếm / vi phạm** | Admin khóa tài khoản, có hiệu lực ngay cả với token còn hạn; không khóa được admin | TC-AZ-04, TC-AZ-05 |
| **HTTPS** | Production: HSTS + chuyển hướng HTTPS | `Program.cs` |
| **Không lưu dữ liệu thẻ** | Chỉ lưu cách thanh toán (`cod` / `bank`), không lưu số thẻ hay CVV | Thiết kế CSDL |

## 3. Hạn chế đã biết

| Hạn chế | Ảnh hưởng | Hướng khắc phục |
|---|---|---|
| JWT lưu ở `localStorage` của trình duyệt | Nếu trang React có lỗ hổng XSS, token có thể bị đọc. Đã giảm thiểu: React tự mã hóa nội dung, không dùng `dangerouslySetInnerHTML` | Chuyển sang cookie `HttpOnly` + anti-forgery cho API |
| Đổi mật khẩu không đăng xuất các thiết bị khác | Thiết bị cũ dùng tiếp tới khi token hết hạn (60 phút) | Thêm `SecurityStamp` vào token và kiểm tra ở mỗi request |
| Rate limit lưu trong bộ nhớ một máy chủ | Khởi động lại thì bộ đếm về 0; không chia sẻ khi chạy nhiều máy chủ | Dùng Redis cho rate limit |
| Chưa có Content-Security-Policy (CSP) | Thiếu một lớp phòng thủ chống XSS | Thêm header CSP liệt kê nguồn script / style được phép |
