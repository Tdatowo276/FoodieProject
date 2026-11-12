# Foodie: Hệ thống đặt đồ ăn trực tuyến

Ứng dụng web đặt đồ ăn gồm **trang khách hàng** (React) và **trang quản trị** (ASP.NET Core MVC), dùng chung một backend
**ASP.NET Core 8** (MVC + Web API) với **Entity Framework Core** và **SQL Server**.

| Thành phần | Địa chỉ khi chạy local |
|---|---|
| Trang khách hàng (React) | http://localhost:3000 |
| Trang quản trị (MVC) | http://localhost:5037/Admin |
| Tài liệu API (Swagger) | http://localhost:5037/swagger |

## Chức năng chính

**Khách hàng:**
- Xem thực đơn theo danh mục, tìm món không cần gõ dấu, xem món nổi bật và bán chạy.
- Giỏ hàng; đặt hàng với địa chỉ mặc định hoặc địa chỉ mới.
- Theo dõi tiến độ đơn; đánh giá món đã mua.
- Quản lý tài khoản: ảnh đại diện, họ tên, mật khẩu, địa chỉ.
- Gửi góp ý, xem bản đồ cửa hàng.

**Quản trị viên:**
- Thống kê doanh thu, số đơn, khách hàng, món bán chạy.
- Quản lý món ăn (kèm upload ảnh hoặc chọn từ thư viện ảnh) và danh mục.
- Xử lý đơn theo đúng luồng trạng thái.
- Khóa / mở khóa tài khoản; đọc góp ý; kiểm duyệt đánh giá.

## Tài liệu

| Tài liệu | Nội dung |
|---|---|
| [docs/UseCases.md](docs/UseCases.md) | Tác nhân, use case, quy tắc nghiệp vụ, đặc tả chi tiết, sơ đồ trạng thái đơn, yêu cầu phi chức năng |
| [docs/Architecture.md](docs/Architecture.md) | Kiến trúc, luồng xác thực, xử lý lỗi, quản lý trạng thái, công nghệ, cấu trúc thư mục |
| [docs/Database.md](docs/Database.md) | ERD, quan hệ, ràng buộc, quyết định thiết kế, lịch sử migration |
| [docs/API.md](docs/API.md) | Toàn bộ Web API (47 thao tác), quy ước, ví dụ |
| [docs/Security.md](docs/Security.md) | Đối chiếu yêu cầu bảo mật của đề, các lỗ hổng đã xử lý, hạn chế |
| [docs/TestCases.md](docs/TestCases.md) | Test case tự động + thủ công, bảng truy vết yêu cầu |

## Cài đặt và chạy

### Yêu cầu
- **.NET 8 SDK** trở lên: https://dotnet.microsoft.com/download
- **SQL Server**: bản **Express** là đủ (instance mặc định `.\SQLEXPRESS`). macOS / Linux có thể chạy SQL Server bằng Docker.
- **Node.js 18+** (khuyên dùng bản LTS).

### Bước 1: Clone

```powershell
git clone <URL-repository>
cd FoodieProject
```

### Bước 2: Cấu hình bí mật (bắt buộc, làm 1 lần)

Theo yêu cầu bảo mật, chuỗi kết nối, khóa JWT và mật khẩu admin **không nằm trong mã nguồn hay `appsettings.json`**.
Khi phát triển, các giá trị này lưu trong **User Secrets** (nằm ngoài thư mục dự án, không bị commit).
Thiếu khóa nào thì ứng dụng dừng ngay khi khởi động và báo rõ thiếu khóa đó.

**Windows (khuyên dùng):** mở PowerShell (hoặc terminal của VS Code) tại thư mục gốc dự án, chạy script, rồi nhập mật khẩu admin khi được hỏi:

```powershell
Set-ExecutionPolicy -Scope Process Bypass; .\FoodieBackend\scripts\setup-secrets.ps1
```

**macOS / Linux hoặc cấu hình thủ công:**

```bash
cd FoodieBackend/FoodieAPI
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Database=FoodieDB;User Id=sa;Password=<mật khẩu SQL>;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)"
dotnet user-secrets set "Seed:AdminPassword" "<mật khẩu admin, tối thiểu 8 ký tự>"
```

| Khóa | Bắt buộc | Mô tả |
|---|---|---|
| `ConnectionStrings:Default` | ✅ | Chuỗi kết nối SQL Server |
| `Jwt:Key` | ✅ | Khóa ký token JWT, tối thiểu 32 byte |
| `Seed:AdminPassword` | ✅ (lần chạy đầu) | Mật khẩu của tài khoản admin `admin@foodie.vn`, chỉ dùng khi tạo admin lần đầu |

Trên **Production**, đặt các giá trị trên bằng biến môi trường, thay `:` bằng `__`. Ví dụ: `ConnectionStrings__Default`, `Jwt__Key`.

### Bước 3: Chạy backend

```powershell
cd FoodieBackend\FoodieAPI
dotnet run
```

Lần chạy đầu, ứng dụng **tự tạo CSDL `FoodieDB`** bằng EF Core Migrations và nạp dữ liệu mẫu: tài khoản admin,
6 danh mục, 15 món kèm ảnh. Không cần chạy script SQL nào. Khi thấy dòng `Now listening on: http://localhost:5037` là backend đã sẵn sàng.

### Bước 4: Chạy frontend

Mở **một terminal khác**:

```powershell
cd Frontend\REACT-VITE-FOODIE
npm install
npm run dev
```

Mở http://localhost:3000. Địa chỉ backend cấu hình trong `.env.development` (`VITE_API_URL`).

### Tài khoản

| Vai trò | Đăng nhập |
|---|---|
| Quản trị viên | `admin@foodie.vn` / mật khẩu đã đặt ở Bước 2. Dùng cho http://localhost:5037/Admin hoặc nút Authorize trong Swagger |
| Khách hàng | Tự đăng ký tại trang khách hàng |

### Kiểm thử tự động

```powershell
cd FoodieBackend
dotnet test FoodieAPI.Tests -c Release
```

Có 138 test (unit + integration), chạy khoảng 20–30 giây trên **CSDL tạm tự tạo và tự xóa**, không ảnh hưởng dữ liệu thật.
Chi tiết: [FoodieBackend/FoodieAPI.Tests/README.md](FoodieBackend/FoodieAPI.Tests/README.md).

## Tùy chỉnh nội dung

| Muốn đổi | Sửa ở |
|---|---|
| Ảnh banner trang chủ | `Frontend/REACT-VITE-FOODIE/public/banners/` + `src/config/banners.js` |
| Logo | `Frontend/REACT-VITE-FOODIE/public/logo.png` |
| Địa chỉ, số điện thoại, tọa độ bản đồ | `Frontend/REACT-VITE-FOODIE/src/config/contact.js` |
| Thông tin nhóm (trang Về chúng tôi) | `Frontend/REACT-VITE-FOODIE/src/config/about.js` |
| Ảnh món dùng chung của nhóm | `FoodieBackend/FoodieAPI/wwwroot/images/products/` (quy tắc đặt tên trong README của thư mục) |
| Thực đơn mẫu khi tạo CSDL mới | `FoodieBackend/FoodieAPI/Data/Seeder.cs` |

## Nhật ký hệ thống (log)

Khi backend chạy, log được ghi vào `FoodieBackend/FoodieAPI/logs/`:
- `foodie-<ngày>.txt`: mọi sự kiện và request.
- `errors-<ngày>.txt`: chỉ cảnh báo và lỗi.

Mỗi ngày một file, giữ 14 ngày. Thư mục này không đưa lên Git.

## Xử lý sự cố

| Hiện tượng | Cách xử lý |
|---|---|
| Backend dừng với "Thiếu cấu hình bắt buộc ..." | Chưa làm Bước 2; chạy lại script cấu hình bí mật |
| Không kết nối được SQL Server | Kiểm tra dịch vụ `SQL Server (SQLEXPRESS)` đang chạy; sửa `ConnectionStrings:Default` nếu dùng instance khác |
| `dotnet run` / `dotnet test` báo file `FoodieAPI.exe` đang bị khóa | Một bản backend khác đang chạy: tắt nó (Ctrl+C), hoặc chạy test với `-c Release` |
| Trang React báo "Không kết nối được máy chủ" | Backend chưa chạy, hoặc sai `VITE_API_URL` trong `.env.development` |
| Đăng nhập React bị đăng xuất liên tục | Token cũ không còn hợp lệ (khóa JWT đã đổi): đăng nhập lại |
| Muốn tạo lại CSDL từ đầu | Xóa CSDL `FoodieDB` (SSMS) rồi chạy lại backend; migration và dữ liệu mẫu sẽ tạo lại. Đổi mật khẩu admin bằng `dotnet user-secrets set "Seed:AdminPassword" "..."` trước khi chạy |

## Nhóm thực hiện

- Nguyễn Tiến Đạt
- Trương Thị Luyến
