Markdown
# 🍔 Foodie Project - Fullstack Food Ordering Web Application

Dự án thương mại điện tử bán đồ ăn trực tuyến (Foodie) được phát triển theo mô hình tách biệt hoàn toàn giữa **Frontend (ReactJS + Vite)** và **Backend (ASP.NET Core Web API)**.

---

## 📂 1. Cấu trúc thư mục dự án & Ý nghĩa các thành phần

Dự án được quản lý tập trung trong một thư mục gốc với các phân cấp như sau:

```text
FoodieProject/
│
├── FoodieBackend/                  # Phía Backend (.NET Core)
│   └── FoodieAPI/                  # Source code chính của API
│       ├── Controllers/            # Xử lý các HTTP Request từ client
│       ├── Services/               # Viết logic nghiệp vụ (Business Logic)
│       ├── Repositories/           # Tương tác trực tiếp với cơ sở dữ liệu
│       ├── Models/                 # Định nghĩa các cấu trúc dữ liệu / Entity
│       ├── Middlewares/            # Xử lý trung gian (xác thực, lỗi,...)
│       ├── Program.cs              # Điểm khởi chạy ứng dụng & cấu hình DI, Swagger
│       └── appsettings.json        # Cấu hình chuỗi kết nối Database và môi trường
│
├── Frontend/                       # Phía Frontend (ReactJS + Vite)
│   └── REACT-VITE-FOODIE/
│       ├── public/                 # Chứa tài nguyên tĩnh công khai (ảnh, icon,...)
│       └── src/
│           ├── assets/             # CSS toàn cục, hình ảnh, style chung
│           ├── components/         # Các component nhỏ tái sử dụng (Navbar, Footer, Cart,...)
│           ├── context/            # Quản lý Global State (AuthContext, CartContext,...)
│           ├── pages/              # Các trang chính của website (Home, Login, Products, Cart,...)
│           ├── routes/             # Cấu hình định tuyến trang (React Router)
│           ├── services/           # Các file gọi API kết nối Backend (Axios client)
│           ├── utils/              # Các hàm tiện ích, format dữ liệu, validation
│           ├── App.jsx             # Component layout khung toàn cục
│           └── main.jsx            # Điểm khởi chạy ứng dụng React
│
└── README.md                       # Tài liệu hướng dẫn dự án


🚀 2. Hướng dẫn Clone và Chạy dự án (Cross-Platform: macOS & Windows)
Bước 1: Clone code từ GitHub về máy
Mở Terminal (macOS) hoặc Command Prompt / Git Bash (Windows) và chạy lệnh:

Bash
git clone <URL-repository-cua-ban>
cd FoodieProject


Bước 2: Thiết lập và chạy Backend (ASP.NET Core Web API)
Yêu cầu hệ thống: .NET 8 SDK trở lên và SQL Server (bản Express là đủ).

Database `FoodieDB` được Entity Framework Core tự tạo khi chạy ứng dụng. Không cần chạy script SQL thủ công.

#### 🔐 Cấu hình bí mật (bắt buộc, làm 1 lần sau khi clone)

Theo yêu cầu bảo mật, chuỗi kết nối, khóa JWT và mật khẩu admin **không nằm trong mã nguồn hay `appsettings.json`**.
Khi phát triển, các giá trị này lưu trong **User Secrets** (nằm ngoài thư mục dự án, không bị commit).
Nếu thiếu, ứng dụng sẽ dừng ngay khi khởi động và báo thiếu khóa nào.

**Windows (khuyên dùng):** mở PowerShell (hoặc terminal của VS Code) tại thư mục gốc dự án, chạy script, sau đó nhập mật khẩu admin khi được hỏi:

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
| `Seed:AdminPassword` | ✅ (lần chạy đầu) | Mật khẩu của tài khoản admin `admin@foodie.vn` được tạo tự động |
| `Database:Provider` | ❌ | `SqlServer` (mặc định) hoặc `Sqlite` |

Trên môi trường **Production**, đặt các giá trị trên bằng biến môi trường, thay `:` bằng `__`. Ví dụ: `ConnectionStrings__Default`, `Jwt__Key`.

Chạy ứng dụng:

Di chuyển vào thư mục chứa project API:

Bash
cd FoodieBackend/FoodieAPI
Khởi chạy server:

Bash
dotnet run

Sau khi chạy thành công, Backend sẽ khởi chạy và bạn có thể truy cập Swagger UI để kiểm tra API tại: http://localhost:5037 (hoặc cổng hiển thị trên terminal của bạn).

Bước 3: Thiết lập và chạy Frontend (ReactJS + Vite)
Yêu cầu hệ thống: Đã cài đặt Node.js (khuyên dùng bản LTS).

Cài đặt thư viện:

Mở một cửa sổ Terminal mới, di chuyển đến thư mục Frontend:

Bash

cd Frontend/REACT-VITE-FOODIE
Cài đặt các gói phụ thuộc (dependencies):

Bash


npm install
npm install antd @ant-design/icons axios react react-dom

2. Danh sách các dependencies chuẩn trong package.json:
Khi mọi người chạy npm install, npm sẽ tự động quét file package.json để cài đặt. Dưới đây là đoạn cấu hình chuẩn phần dependencies mà dự án của bạn đang sử dụng:

JSON
"dependencies": {
  "@ant-design/icons": "^5.x.x",
  "antd": "^5.x.x",
  "axios": "^1.x.x",
  "react": "^18.x.x || ^19.0.0",
  "react-dom": "^18.x.x || ^19.0.0"
}


Chạy ứng dụng ở môi trường Development:

Bash

npm run dev

Truy cập ứng dụng:

Mở trình duyệt (Chrome/Edge/Safari) và truy cập đường dẫn local được cung cấp trên terminal (thường là http://localhost:3000 hoặc http://localhost:3001).

🛠 3. Công nghệ sử dụng
Frontend: ReactJS, Vite, React Router DOM, Axios, CSS/Inline Styles.

Backend: ASP.NET Core Web API (.NET 9), C#, Entity Framework Core / Ado.NET, Swagger.

Database: SQL Server.

Version Control: Git & GitHub.