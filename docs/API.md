# Tài liệu Web API: Foodie

Tài liệu này **sinh tự động từ Swagger/OpenAPI** của backend, nên luôn khớp với code. Tài liệu tương tác (thử gọi API trực tiếp):
**http://localhost:5037/swagger** (chỉ bật ở môi trường Development).

Tổng cộng **47 thao tác** trên **36 đường dẫn**.

## Quy ước chung

| Mục | Quy ước |
|---|---|
| Định dạng | JSON (`camelCase`); upload ảnh dùng `multipart/form-data`, field `file` |
| Xác thực | `Authorization: Bearer <JWT>`. Lấy token từ `POST /api/auth/login` hoặc `/register`; token hết hạn sau 60 phút |
| Phân quyền | **Công khai**: ai cũng gọi được · **Đăng nhập**: cần JWT · **Admin**: cần JWT của tài khoản vai trò Admin (kiểm tra phía server) |
| Phương thức | `GET` đọc · `POST` tạo mới (trả `201 Created` + `Location`) · `PUT` thay thế / ghi đè · `PATCH` sửa một phần (trạng thái đơn, khóa tài khoản) · `DELETE` xóa (trả `204 No Content`) |
| Phân trang | Tham số `page`, `pageSize` (1–50, mặc định 10). Kết quả: `{ items, total, page, pageSize }` |
| Lỗi | `application/problem+json` (ProblemDetails): `{ title, status }`; lỗi dữ liệu có thêm `errors: { "tenTruong": ["thông báo"] }`, kể cả trường lồng như `items[1].quantity` |
| Mã lỗi | `400` dữ liệu không hợp lệ · `401` chưa đăng nhập / token hết hạn / tài khoản bị khóa · `403` không đủ quyền · `404` không tìm thấy · `409` xung đột (trùng, đang được dùng) · `429` gọi quá nhiều lần |
| Giới hạn | Đăng nhập / đăng ký / đổi mật khẩu: 10 lần/phút/IP · Gửi góp ý: 5 lần/10 phút/IP |

## Tài khoản

| Phương thức | Đường dẫn | Quyền | Mô tả | Mã trả về |
|---|---|---|---|---|
| `POST` | `/api/auth/login` | Công khai | Đăng nhập, trả về JWT | 200, 400, 401, 403, 429 |
| `GET` | `/api/auth/me` | Đăng nhập | Thông tin tài khoản đang đăng nhập | 200, 401 |
| `POST` | `/api/auth/register` | Công khai | Đăng ký tài khoản khách hàng, trả về JWT | 200, 400, 409, 429 |

## Thực đơn

| Phương thức | Đường dẫn | Quyền | Mô tả | Mã trả về |
|---|---|---|---|---|
| `GET` | `/api/categories` | Công khai | Danh mục đang hiển thị (kèm số món) | 200 |
| `GET` | `/api/products` | Công khai | Danh sách món (lọc, tìm kiếm, phân trang) | 200 |
| `GET` | `/api/products/best-sellers` | Công khai | Món bán chạy (theo số phần đã bán trong các đơn hoàn thành, mặc định 30 ngày) | 200 |
| `GET` | `/api/products/featured` | Công khai | Tối đa 8 món nổi bật, xếp theo đánh giá | 200 |
| `GET` | `/api/products/{id}` | Công khai | Chi tiết một món | 200, 404 |

## Đơn hàng

| Phương thức | Đường dẫn | Quyền | Mô tả | Mã trả về |
|---|---|---|---|---|
| `POST` | `/api/orders` | Đăng nhập | Đặt hàng | 201, 400, 401, 403 |
| `GET` | `/api/orders/me` | Đăng nhập | Đơn hàng của tôi (mới nhất trước, phân trang) | 200, 401 |
| `GET` | `/api/orders/{code}` | Đăng nhập | Chi tiết một đơn của tôi | 200, 401, 404 |

## Hồ sơ cá nhân

| Phương thức | Đường dẫn | Quyền | Mô tả | Mã trả về |
|---|---|---|---|---|
| `GET` | `/api/profile` | Đăng nhập | Xem hồ sơ (họ tên, ảnh đại diện, địa chỉ mặc định) | 200, 401 |
| `PUT` | `/api/profile` | Đăng nhập | Đổi họ tên | 200, 400, 401 |
| `PUT` | `/api/profile/address` | Đăng nhập | Lưu địa chỉ giao hàng mặc định | 200, 400, 401 |
| `DELETE` | `/api/profile/address` | Đăng nhập | Xóa địa chỉ mặc định | 200, 401 |
| `POST` | `/api/profile/avatar` | Đăng nhập | Đổi ảnh đại diện (multipart/form-data, field "file") | 200, 400, 401 |
| `DELETE` | `/api/profile/avatar` | Đăng nhập | Gỡ ảnh đại diện | 200, 401 |
| `PUT` | `/api/profile/password` | Đăng nhập | Đổi mật khẩu (bắt buộc nhập đúng mật khẩu hiện tại) | 204, 400, 401, 429 |

## Đánh giá món

| Phương thức | Đường dẫn | Quyền | Mô tả | Mã trả về |
|---|---|---|---|---|
| `GET` | `/api/products/{productId}/reviews` | Công khai | Đánh giá của một món + điểm trung bình, số lượt theo từng mức sao | 200, 404 |
| `GET` | `/api/products/{productId}/reviews/me` | Đăng nhập | Tôi đã được phép đánh giá chưa (đã mua và nhận hàng) + đánh giá hiện tại của tôi | 200, 401 |
| `PUT` | `/api/products/{productId}/reviews/me` | Đăng nhập | Thêm hoặc sửa đánh giá của tôi (1-5 sao, nhận xét tối đa 500 ký tự) | 200, 400, 401, 403, 404 |
| `DELETE` | `/api/products/{productId}/reviews/me` | Đăng nhập | Xóa đánh giá của tôi | 204, 401, 404 |

## Góp ý

| Phương thức | Đường dẫn | Quyền | Mô tả | Mã trả về |
|---|---|---|---|---|
| `POST` | `/api/feedback` | Công khai | Gửi góp ý cho chủ shop | 201, 400, 429 |

## Quản trị - Tổng quan

| Phương thức | Đường dẫn | Quyền | Mô tả | Mã trả về |
|---|---|---|---|---|
| `GET` | `/api/admin/stats` | Admin | Doanh thu, số đơn, khách hàng tháng này so với tháng trước | 200, 401, 403 |
| `GET` | `/api/admin/stats/top-products` | Admin | Món bán chạy (số phần + doanh thu, chỉ đơn hoàn thành) | 200, 401, 403 |

## Quản trị - Món ăn

| Phương thức | Đường dẫn | Quyền | Mô tả | Mã trả về |
|---|---|---|---|---|
| `GET` | `/api/admin/images` | Admin | Ảnh có sẵn trong thư mục wwwroot/images/products | 200, 401, 403 |
| `GET` | `/api/admin/products` | Admin | Danh sách món (gồm cả món thuộc danh mục đang ẩn) | 200, 401, 403 |
| `POST` | `/api/admin/products` | Admin | Thêm món | 201, 400, 401, 403 |
| `GET` | `/api/admin/products/{id}` | Admin | Chi tiết món | 200, 401, 403, 404 |
| `PUT` | `/api/admin/products/{id}` | Admin | Sửa món (ảnh cũ tự xóa nếu đổi ảnh) | 200, 400, 401, 403, 404 |
| `DELETE` | `/api/admin/products/{id}` | Admin | Xóa món (đơn cũ vẫn giữ tên và giá món) | 204, 401, 403, 404 |
| `POST` | `/api/admin/uploads` | Admin | Upload ảnh món (multipart/form-data, field "file") | 200, 400, 401, 403 |

## Quản trị - Danh mục

| Phương thức | Đường dẫn | Quyền | Mô tả | Mã trả về |
|---|---|---|---|---|
| `GET` | `/api/admin/categories` | Admin | Tất cả danh mục (gồm cả danh mục đang ẩn) | 200, 401, 403 |
| `POST` | `/api/admin/categories` | Admin | Thêm danh mục (tên không được trùng) | 201, 400, 401, 403 |
| `GET` | `/api/admin/categories/{id}` | Admin | Chi tiết danh mục | 200, 401, 403, 404 |
| `PUT` | `/api/admin/categories/{id}` | Admin | Sửa danh mục / ẩn hiện danh mục | 200, 400, 401, 403, 404 |
| `DELETE` | `/api/admin/categories/{id}` | Admin | Xóa danh mục (chỉ khi không còn món) | 204, 401, 403, 404, 409 |

## Quản trị - Đơn hàng

| Phương thức | Đường dẫn | Quyền | Mô tả | Mã trả về |
|---|---|---|---|---|
| `GET` | `/api/admin/orders` | Admin | Danh sách đơn (lọc trạng thái, tìm theo mã đơn / tên người nhận) | 200, 400, 401, 403 |
| `GET` | `/api/admin/orders/{code}` | Admin | Chi tiết đơn (kèm các trạng thái được phép chuyển tới) | 200, 401, 403, 404 |
| `PATCH` | `/api/admin/orders/{code}/status` | Admin | Đổi trạng thái đơn | 200, 400, 401, 403, 404, 409 |

## Quản trị - Người dùng

| Phương thức | Đường dẫn | Quyền | Mô tả | Mã trả về |
|---|---|---|---|---|
| `GET` | `/api/admin/users` | Admin | Danh sách tài khoản (tìm theo email / họ tên, lọc vai trò) | 200, 400, 401, 403 |
| `PATCH` | `/api/admin/users/{id}/lock` | Admin | Khóa / mở khóa tài khoản khách hàng | 200, 400, 401, 403, 404 |

## Quản trị - Đánh giá

| Phương thức | Đường dẫn | Quyền | Mô tả | Mã trả về |
|---|---|---|---|---|
| `GET` | `/api/admin/reviews` | Admin | Danh sách đánh giá (lọc theo số sao, tìm theo món / người / nội dung) | 200, 401, 403 |
| `DELETE` | `/api/admin/reviews/{id}` | Admin | Xóa đánh giá không phù hợp | 204, 401, 403, 404 |

## Quản trị - Góp ý

| Phương thức | Đường dẫn | Quyền | Mô tả | Mã trả về |
|---|---|---|---|---|
| `GET` | `/api/admin/feedback` | Admin | Danh sách góp ý (chưa đọc trước, mới nhất trước) | 200, 401, 403 |
| `DELETE` | `/api/admin/feedback/{id}` | Admin | Xóa góp ý | 204, 401, 403, 404 |
| `PATCH` | `/api/admin/feedback/{id}/read` | Admin | Đánh dấu đã đọc (?read=false để đánh dấu chưa đọc) | 200, 401, 403, 404 |

## Ví dụ

Đăng nhập rồi đặt hàng (PowerShell):

```powershell
$login = Invoke-RestMethod -Method Post -Uri http://localhost:5037/api/auth/login `
  -ContentType "application/json" -Body '{"email":"khach@example.com","password":"MatKhau123"}'
$headers = @{ Authorization = "Bearer $($login.token)" }

Invoke-RestMethod -Method Post -Uri http://localhost:5037/api/orders -Headers $headers `
  -ContentType "application/json; charset=utf-8" -Body '{
    "items": [ { "productId": 6, "quantity": 2 } ],
    "receiverName": "Nguyễn Văn A", "phone": "0901234567",
    "address": "1 Lê Lợi, Hà Nội", "paymentMethod": "cod"
  }'
# => 201 { "orderCode": "FD123456", "total": 129000, "status": "pending", ... }
```

Lỗi dữ liệu (400):

```json
{
  "title": "Dữ liệu không hợp lệ.",
  "status": 400,
  "errors": {
    "phone": ["Số điện thoại chưa đúng (ví dụ 0901234567)."],
    "items[1].quantity": ["Mỗi món từ 1 đến 99 phần."]
  }
}
```
