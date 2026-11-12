# FoodieAPI.Tests: kiểm thử tự động

Bộ kiểm thử xUnit cho backend Foodie: **130 test**, chạy khoảng 20 giây.

## Chạy test

Tại thư mục `FoodieBackend`:

```powershell
dotnet test FoodieAPI.Tests -c Release
```

> Dùng `-c Release` để không đụng tới file `bin/Debug` đang bị khóa khi `dotnet run` (backend) còn chạy.
> Nếu backend đã tắt thì `dotnet test FoodieAPI.Tests` cũng được.

Xem tên từng test:

```powershell
dotnet test FoodieAPI.Tests -c Release --logger "console;verbosity=normal"
```

## Yêu cầu

- SQL Server đang chạy (mặc định `.\SQLEXPRESS`, giống ứng dụng).
- Máy dùng SQL Server khác (ví dụ LocalDB) thì đặt biến môi trường trước khi chạy:

  ```powershell
  $env:FOODIE_TEST_SQLSERVER = "(localdb)\MSSQLLocalDB"
  ```

- **Không cần** cấu hình User Secrets; test tự dùng khóa JWT và mật khẩu admin riêng.

## An toàn với dữ liệu thật

- Mỗi lần chạy tạo một CSDL tạm `FoodieDB_Test_<ngẫu nhiên>`, chạy xong **tự xóa**.
- Ứng dụng trong test chạy ở môi trường `Testing`: User Secrets (chuỗi kết nối tới `FoodieDB` thật)
  **không được nạp**, nên test không thể chạm vào dữ liệu thật.
- Ảnh upload trong test ghi vào thư mục tạm, không ghi vào `wwwroot` của dự án.

## Cấu trúc

| Thư mục / file | Nội dung |
|---|---|
| `Unit/BusinessRuleTests.cs` | Luồng trạng thái đơn, phí giao hàng, tìm kiếm không dấu, tên file ảnh, phân trang, nhận dạng ảnh thật, chặn đường dẫn ảnh nguy hiểm |
| `Unit/DtoValidationTests.cs` | Data Annotations của các DTO (kể cả phần tử lồng `items[1].quantity`, `[Compare]` mật khẩu) |
| `Integration/SmokeTests.cs` | Ứng dụng chạy trên CSDL tạm, đủ migration, có dữ liệu mẫu |
| `Integration/AuthTests.cs` | Đăng ký, đăng nhập, JWT, mật khẩu băm BCrypt, không tự chọn vai trò |
| `Integration/AuthorizationTests.cs` | Phân quyền 401/403, cookie và JWT tách biệt, khóa tài khoản có hiệu lực ngay |
| `Integration/ProductTests.cs` | Thực đơn, tìm kiếm, phân trang, ẩn danh mục, CRUD món/danh mục, thư viện ảnh |
| `Integration/UploadTests.cs` | Upload an toàn: đuôi file, nội dung giả, quá 2MB, tên do server sinh, xóa ảnh cũ |
| `Integration/OrderTests.cs` | Đặt hàng (giá lấy từ CSDL, phí ship), đơn của tôi, chống xem đơn người khác, luồng trạng thái, doanh thu |
| `Integration/ProfileAndFeedbackTests.cs` | Hồ sơ (tên, mật khẩu, ảnh đại diện, địa chỉ), góp ý |
| `Integration/AdminMvcTests.cs` | Khu Admin MVC: CSRF, cookie HttpOnly, open redirect, XSS, trang lỗi, header bảo mật, AJAX; rate limit |
