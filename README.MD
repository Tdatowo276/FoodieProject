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
├── FoodieDB.sql                    # File script khởi tạo Cơ sở dữ liệu SQL Server
└── README.md                       # Tài liệu hướng dẫn dự án


🚀 2. Hướng dẫn Clone và Chạy dự án (Cross-Platform: macOS & Windows)
Bước 1: Clone code từ GitHub về máy
Mở Terminal (macOS) hoặc Command Prompt / Git Bash (Windows) và chạy lệnh:

Bash
git clone <URL-repository-cua-ban>
cd FoodieProject


Bước 2: Thiết lập và chạy Backend (ASP.NET Core Web API)
Yêu cầu hệ thống: Đã cài đặt .NET 9 SDK trở lên.

Cấu hình Cơ sở dữ liệu:

Mở file FoodieDB.sql để tạo database trên SQL Server (hoặc cấu hình lại chuỗi kết nối trong file FoodieBackend/FoodieAPI/appsettings.json).

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