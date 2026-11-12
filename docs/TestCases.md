# Tài liệu kiểm thử: Hệ thống Foodie

| Mục | Nội dung |
|---|---|
| Hệ thống | Foodie: đặt đồ ăn trực tuyến (React + ASP.NET Core MVC + Web API + SQL Server) |
| Phạm vi | Toàn bộ chức năng khách hàng, quản trị viên, Web API, bảo mật |
| Loại kiểm thử | Tự động (xUnit: unit + integration) và thủ công (giao diện, thiết bị) |
| Số test case | **123** (96 tự động + 27 thủ công); 96 test tự động gồm **138 lượt chạy** do có test chạy nhiều bộ dữ liệu |
| Kết quả tự động gần nhất | **138 / 138 đạt** (ngày 12/11/2026) |

---

## 1. Môi trường và cách chạy

### 1.1. Kiểm thử tự động
- **Công cụ:** xUnit 2.9, `Microsoft.AspNetCore.Mvc.Testing` (chạy toàn bộ ứng dụng trong bộ nhớ, gọi qua HTTP như client thật).
- **CSDL:** mỗi lần chạy tạo một CSDL tạm `FoodieDB_Test_<ngẫu nhiên>` trên SQL Server, chạy xong tự xóa. Không ảnh hưởng dữ liệu thật.
- **Lệnh chạy** (tại thư mục `FoodieBackend`):
  ```powershell
  dotnet test FoodieAPI.Tests -c Release
  ```
- **Mã nguồn test:** `FoodieBackend/FoodieAPI.Tests/` (hướng dẫn chi tiết trong `README.md` của thư mục đó).

### 1.2. Kiểm thử thủ công
- Backend: `cd FoodieBackend/FoodieAPI` → `dotnet run` (http://localhost:5037).
- Frontend: `cd Frontend/REACT-VITE-FOODIE` → `npm run dev` (http://localhost:3000).
- Trình duyệt: Chrome / Edge bản mới; kiểm tra điện thoại bằng máy thật hoặc DevTools (F12 → biểu tượng điện thoại, chọn iPhone SE 375px).
- Tài khoản: admin `admin@foodie.vn` (mật khẩu đặt trong User Secrets); khách hàng tự đăng ký.

### 1.3. Quy ước
- **Kết quả:** `Đạt` / `Không đạt` (ghi rõ hiện tượng ở cột ghi chú).
- **Mã test case:** `TC-<NHÓM>-<số>` cho tự động, `MT-<số>` cho thủ công.
- Cột **"Test tự động"** ghi `Lớp.Phương thức` trong `FoodieAPI.Tests`. Có thể chạy riêng một test bằng
  `dotnet test FoodieAPI.Tests -c Release --filter "FullyQualifiedName~<Phương thức>"`.

---

## 2. Test case tự động

### 2.1. Quy tắc nghiệp vụ (Unit test)

| Mã TC | Chức năng | Mô tả / dữ liệu | Kết quả mong đợi | Test tự động | Kết quả |
|---|---|---|---|---|---|
| TC-BR-01 | Luồng trạng thái đơn | Các bước hợp lệ: chờ xác nhận → đang chuẩn bị → đang giao → hoàn thành; hủy khi chưa giao (5 bộ dữ liệu) | Cho phép | `OrderStatusTests.CanChange_ValidFlow_ReturnsTrue` | Đạt |
| TC-BR-02 | Luồng trạng thái đơn | Nhảy cóc, lùi bước, hủy khi đang giao, đổi trạng thái cuối (6 bộ) | Từ chối | `OrderStatusTests.CanChange_InvalidFlow_ReturnsFalse` | Đạt |
| TC-BR-03 | Luồng trạng thái đơn | Các trạng thái kế tiếp của "chờ xác nhận" / "hoàn thành" / "đã hủy" | [chuẩn bị, hủy] / rỗng / rỗng | `OrderStatusTests.NextOf_ReturnsAllowedNextStatuses` | Đạt |
| TC-BR-04 | Trạng thái hợp lệ | `pending`, `completed`, `PENDING`, `hacked`, null (5 bộ) | Chỉ chấp nhận đúng giá trị chữ thường | `OrderStatusTests.IsValid` | Đạt |
| TC-BR-05 | Phí giao hàng | Tạm tính 0 / 199.999 / 200.000 / 1.000.000đ (4 bộ) | 15.000 / 15.000 / 0 / 0đ | `ShippingFeeTests.ShippingFeeFor_AppliesFreeShippingThreshold` | Đạt |
| TC-BR-06 | Tìm kiếm không dấu | "Phở Bò Tái", "Đồ uống", chuỗi rỗng, null (5 bộ) | "pho bo tai", "do uong", "" | `TextUtilTests.Normalize_RemovesDiacriticsAndLowercases` | Đạt |
| TC-BR-07 | Tên file ảnh từ tên món | "Combo Burger Gà Giòn", "  Mỳ  Ý!! " (3 bộ) | `combo-burger-ga-gion`, `my-y` | `TextUtilTests.Slug_CreatesSafeFileName` | Đạt |
| TC-BR-08 | Từ khóa tìm kiếm | Từ khóa toàn khoảng trắng; từ khóa 500 ký tự | null; cắt còn 100 ký tự | `TextUtilTests.Keyword_EmptyIsNull_LongIsTruncatedTo100` | Đạt |
| TC-BR-09 | Phân trang | Trang/kích thước: mặc định, (3,20), (0,1000), (-5,0) (4 bộ) | Trang ≥ 1, 1–50 dòng/trang, skip đúng | `PagingTests.Normalize_ClampsValues` | Đạt |
| TC-BR-10 | Nhận dạng ảnh thật | Byte đầu file PNG / JPG | Nhận đúng `png` / `jpg` | `ImageSnifferTests.Detect_RecognizesRealPngAndJpg` | Đạt |
| TC-BR-11 | Nhận dạng ảnh giả | GIF, mã PHP đổi đuôi, file quá ngắn | Không nhận là ảnh | `ImageSnifferTests.Detect_RejectsFakeOrOtherFormats` | Đạt |
| TC-BR-12 | Đường dẫn ảnh hợp lệ | `/uploads/<guid>.png`, `/images/products/burger-ga.png`... (4 bộ) | Chấp nhận | `ImagePathPatternTests.ProductImagePattern_AcceptsSystemPaths` | Đạt |
| TC-BR-13 | Đường dẫn ảnh nguy hiểm | `../../appsettings.json`, link ngoài, chữ hoa, có dấu, `.svg` (7 bộ) | Từ chối | `ImagePathPatternTests.ProductImagePattern_RejectsUnsafePaths` | Đạt |

### 2.2. Kiểm tra dữ liệu: Data Annotations (Unit test)

| Mã TC | Chức năng | Dữ liệu | Kết quả mong đợi | Test tự động | Kết quả |
|---|---|---|---|---|---|
| TC-VAL-01 | Đăng ký | Tên, email, mật khẩu hợp lệ | Không lỗi | `DtoValidationTests.RegisterRequest_Valid_Passes` | Đạt |
| TC-VAL-02 | Đăng ký | Tên 1 ký tự, email sai, mật khẩu 3 ký tự | Lỗi đủ 3 trường `name`, `email`, `password` | `DtoValidationTests.RegisterRequest_Invalid_ReportsEachField` | Đạt |
| TC-VAL-03 | Đặt hàng | Món thứ 2 có số lượng 0 | Lỗi đúng trường `items[1].quantity` | `DtoValidationTests.OrderRequest_ValidatesNestedItems` | Đạt |
| TC-VAL-04 | Đặt hàng | Giỏ rỗng, SĐT "123", thanh toán "momo" | Lỗi `items`, `phone`, `paymentMethod` | `DtoValidationTests.OrderRequest_RejectsEmptyCartBadPhoneAndUnknownPayment` | Đạt |
| TC-VAL-05 | Đổi mật khẩu | Nhập lại mật khẩu không khớp | Lỗi `confirmPassword` (`[Compare]`) | `DtoValidationTests.ChangePasswordRequest_ConfirmMustMatch` | Đạt |
| TC-VAL-06 | Món ăn | Ảnh `/images/products/../../appsettings.json` | Lỗi `imageUrl` | `DtoValidationTests.ProductRequest_RejectsPathTraversalImage` | Đạt |
| TC-VAL-07 | Góp ý | Chủ đề lạ, 6 sao, nội dung quá ngắn | Lỗi `topic`, `rating`, `message` | `DtoValidationTests.FeedbackRequest_ValidatesTopicRatingAndLength` | Đạt |
| TC-VAL-08 | Địa chỉ | SĐT `+84901234567` / `090123` | Hợp lệ / lỗi `phone` | `DtoValidationTests.AddressRequest_RequiresVietnamesePhoneFormat` | Đạt |

### 2.3. Khởi động hệ thống

| Mã TC | Chức năng | Mô tả | Kết quả mong đợi | Test tự động | Kết quả |
|---|---|---|---|---|---|
| TC-SYS-01 | Migration + CSDL test | Khởi động ứng dụng ở môi trường Testing | Dùng CSDL tạm, không còn migration chờ | `SmokeTests.App_UsesTemporaryDatabase_NotRealOne` | Đạt |
| TC-SYS-02 | Dữ liệu mẫu | CSDL mới tạo | Có admin, 6 danh mục, 15 món của thực đơn | `SmokeTests.Seeder_CreatesAdminCategoriesAndMenu` | Đạt |
| TC-SYS-03 | Thực đơn công khai | `GET /api/products` | 200, có đủ món mẫu | `SmokeTests.PublicMenu_ContainsSeededMenu` | Đạt |

### 2.4. Đăng ký / đăng nhập

| Mã TC | Chức năng | Các bước / dữ liệu | Kết quả mong đợi | Test tự động | Kết quả |
|---|---|---|---|---|---|
| TC-AUTH-01 | Đăng ký | Gửi tên, email mới, mật khẩu hợp lệ | 200, có JWT, vai trò `Customer` | `AuthTests.Register_ReturnsToken_WithCustomerRole` | Đạt |
| TC-AUTH-02 | Đăng ký: không tự chọn vai trò | Gửi kèm `"role": "Admin"` | Vai trò vẫn là `Customer` | `AuthTests.Register_CannotChooseOwnRole` | Đạt |
| TC-AUTH-03 | Đăng ký trùng email | Email đã có, viết HOA | 409 Conflict | `AuthTests.Register_DuplicateEmail_IgnoringCase_Returns409` | Đạt |
| TC-AUTH-04 | Đăng ký sai dữ liệu | Tên 1 ký tự, email sai, mật khẩu ngắn | 400, lỗi theo từng trường | `AuthTests.Register_InvalidData_Returns400WithFieldErrors` | Đạt |
| TC-AUTH-05 | Lưu mật khẩu an toàn | Đăng ký rồi đọc CSDL | Mật khẩu lưu dạng băm BCrypt, không có mật khẩu gốc | `AuthTests.Password_IsStoredAsBCryptHash_NotPlainText` | Đạt |
| TC-AUTH-06 | Đăng nhập | Email + mật khẩu đúng | 200, có JWT | `AuthTests.Login_Succeeds_WithCorrectPassword` | Đạt |
| TC-AUTH-07 | Đăng nhập sai | Sai mật khẩu; email không tồn tại | Cả hai 401, **cùng thông báo** (không dò được email) | `AuthTests.Login_WrongPasswordAndUnknownEmail_GiveSameError` | Đạt |
| TC-AUTH-08 | Xác thực token | Không token / token giả / token hợp lệ gọi `/api/auth/me` | 401 / 401 / 200 đúng người | `AuthTests.Me_RequiresValidToken` | Đạt |

### 2.5. Phân quyền

| Mã TC | Chức năng | Các bước / dữ liệu | Kết quả mong đợi | Test tự động | Kết quả |
|---|---|---|---|---|---|
| TC-AZ-01 | API quản trị | 5 API `/api/admin/*`, gọi bằng: khách vãng lai / khách hàng / admin (5 bộ) | 401 / 403 / 200 | `AuthorizationTests.AdminApi_Anonymous401_Customer403_Admin200` | Đạt |
| TC-AZ-02 | Tách cookie và JWT | Đăng nhập Admin MVC (cookie) rồi gọi `/api/admin/stats` | Trang Admin 200; API 401 (API chỉ nhận JWT) | `AuthorizationTests.AdminCookie_IsNotAccepted_ByApi` | Đạt |
| TC-AZ-03 | Tách cookie và JWT | Gửi JWT admin vào `/Admin/Dashboard` | Chuyển về trang đăng nhập | `AuthorizationTests.AdminJwt_IsNotAccepted_ByMvcArea` | Đạt |
| TC-AZ-04 | Khóa tài khoản | Admin khóa khách đang đăng nhập; khách đăng nhập lại; admin mở khóa | Token cũ bị từ chối ngay (401); đăng nhập lại 403; mở khóa dùng lại được | `AuthorizationTests.LockingAccount_RevokesExistingToken_Immediately` | Đạt |
| TC-AZ-05 | Khóa tài khoản | Admin khóa tài khoản admin | 400, không cho khóa | `AuthorizationTests.AdminAccount_CannotBeLocked` | Đạt |

### 2.6. Thực đơn, danh mục, món ăn

| Mã TC | Chức năng | Các bước / dữ liệu | Kết quả mong đợi | Test tự động | Kết quả |
|---|---|---|---|---|---|
| TC-PRD-01 | Tìm kiếm | "burger ga", "BURGER GÀ", "burger gà" (3 bộ) | Đều tìm thấy "Burger gà" | `ProductTests.Search_IgnoresCaseAndDiacritics` | Đạt |
| TC-PRD-02 | Phân trang + lọc | `page=1&pageSize=5`; lọc danh mục "Đồ uống" | Đúng 5 món; chỉ món "Đồ uống" | `ProductTests.List_SupportsPagingAndCategoryFilter` | Đạt |
| TC-PRD-03 | Món nổi bật | `GET /api/products/featured` | 1–8 món, đều là món nổi bật | `ProductTests.Featured_ReturnsAtMost8FeaturedItems` | Đạt |
| TC-PRD-04 | Món không tồn tại | `GET /api/products/999999` | 404, dạng `application/problem+json` | `ProductTests.GetById_Unknown_Returns404ProblemJson` | Đạt |
| TC-PRD-05 | Ẩn danh mục | Admin ẩn danh mục có món | Khách không thấy món và danh mục; admin vẫn thấy | `ProductTests.HidingCategory_HidesItsProductsFromCustomers_ButNotFromAdmin` | Đạt |
| TC-PRD-06 | Danh mục trùng tên | Thêm "ĐỒ UỐNG" khi đã có "Đồ uống" | 400, lỗi `name` | `ProductTests.Category_DuplicateName_IgnoringCase_Returns400` | Đạt |
| TC-PRD-07 | Xóa danh mục | Xóa danh mục còn món; danh mục rỗng; xóa lại | 409 / 204 / 404 | `ProductTests.Category_WithProducts_CannotBeDeleted_EmptyOneCan` | Đạt |
| TC-PRD-08 | CRUD món | Thêm → sửa giá + nổi bật → xóa | 201 → 200 (dữ liệu mới) → 204, khách không còn thấy | `ProductTests.Product_CreateUpdateDelete` | Đạt |
| TC-PRD-09 | Món sai dữ liệu | Tên rỗng, giá 5đ, ảnh link ngoài, thiếu danh mục | 400, lỗi 4 trường | `ProductTests.Product_InvalidData_Returns400PerField` | Đạt |
| TC-PRD-10 | Món sai danh mục | `categoryId` không tồn tại | 400, lỗi `categoryId` | `ProductTests.Product_UnknownCategory_Returns400` | Đạt |
| TC-PRD-11 | Thư viện ảnh | Liệt kê ảnh; gắn ảnh không có thật; gắn ảnh có thật; xóa món | Có trong danh sách; 400; 201; file thư viện vẫn còn | `ProductTests.LibraryImage_MustExistOnDisk_AndIsNeverDeleted` | Đạt |

### 2.7. Upload ảnh an toàn

| Mã TC | Chức năng | Các bước / dữ liệu | Kết quả mong đợi | Test tự động | Kết quả |
|---|---|---|---|---|---|
| TC-UP-01 | Upload PNG | File PNG thật, tên `../../anh cua toi.png` | 200; tên file do server sinh `/uploads/<guid>.png` (chặn path traversal) | `UploadTests.ValidPng_IsSaved_WithServerGeneratedName` | Đạt |
| TC-UP-02 | Upload JPG | File JPG thật, đuôi `.jpeg` | 200 | `UploadTests.ValidJpg_IsAccepted` | Đạt |
| TC-UP-03 | Sai đuôi file | `shell.php`, `anh.gif`, `anh.svg` (3 bộ) | 400 | `UploadTests.WrongExtension_IsRejected` | Đạt |
| TC-UP-04 | Nội dung giả | Mã PHP đặt tên `anh.png` | 400, không lưu file rác | `UploadTests.FakeContent_WithImageExtension_IsRejected` | Đạt |
| TC-UP-05 | Đuôi không khớp nội dung | Nội dung JPG, đuôi `.png` | 400 | `UploadTests.ExtensionNotMatchingContent_IsRejected` | Đạt |
| TC-UP-06 | Giới hạn dung lượng | File 2MB + 1 byte | 400 | `UploadTests.FileOver2MB_IsRejected` | Đạt |
| TC-UP-07 | Quyền upload | Khách hàng gọi API upload | 403 | `UploadTests.Customer_CannotUpload` | Đạt |
| TC-UP-08 | Dọn ảnh cũ | Thay ảnh của món | File ảnh cũ bị xóa, ảnh mới còn | `UploadTests.ReplacingProductImage_DeletesOldUploadedFile` | Đạt |

### 2.8. Đặt hàng và xử lý đơn

| Mã TC | Chức năng | Các bước / dữ liệu | Kết quả mong đợi | Test tự động | Kết quả |
|---|---|---|---|---|---|
| TC-ORD-01 | Tính tiền | 2 × Burger gà (50.000đ); client gửi kèm `total = 1` | Tổng 115.000đ (giá lấy từ CSDL + 15.000đ ship), mã đơn `FDxxxxxx`, trạng thái chờ xác nhận | `OrderTests.Create_UsesDatabasePrices_AndAddsShippingFee` | Đạt |
| TC-ORD-02 | Miễn phí giao | Gà sốt mật ong + Pepsi = 210.000đ | Tổng 210.000đ (miễn phí ship) | `OrderTests.Create_FreeShipping_From200k` | Đạt |
| TC-ORD-03 | Bắt buộc đăng nhập | Đặt hàng không có token | 401 | `OrderTests.Create_WithoutLogin_Returns401` | Đạt |
| TC-ORD-04 | Kiểm tra món lồng | Món thứ 2 số lượng 0 | 400, lỗi `items[1].quantity` | `OrderTests.Create_InvalidNestedItem_ReportsExactField` | Đạt |
| TC-ORD-05 | Gộp dòng trùng | Cùng món 2 dòng × 60 phần (= 120 > 99) | 400 | `OrderTests.Create_DuplicateLinesOver99_IsRejected` | Đạt |
| TC-ORD-06 | Món không tồn tại | `productId = 999999` | 400 | `OrderTests.Create_UnknownProduct_IsRejected` | Đạt |
| TC-ORD-07 | Món thuộc danh mục ẩn | Đặt món của danh mục đã ẩn | 400 | `OrderTests.ProductInHiddenCategory_CannotBeOrdered` | Đạt |
| TC-ORD-08 | Quyền xem đơn | Chủ đơn / người khác xem chi tiết; "Đơn của tôi" | 200 / **404**; mỗi người chỉ thấy đơn của mình | `OrderTests.OrderDetail_OnlyVisibleToOwner` | Đạt |
| TC-ORD-09 | Luồng trạng thái | Admin đổi: nhảy cóc, trạng thái lạ, đi đúng luồng, hủy khi đang giao | 400, 400, 200, 400; khách thấy "hoàn thành" | `OrderTests.StatusFlow_FollowsBusinessRules` | Đạt |
| TC-ORD-10 | Doanh thu | Hoàn thành đơn 228.000đ | Doanh thu tháng tăng đúng 228.000đ | `OrderTests.CompletingOrder_IncreasesMonthlyRevenue` | Đạt |

### 2.9. Hồ sơ cá nhân

| Mã TC | Chức năng | Các bước / dữ liệu | Kết quả mong đợi | Test tự động | Kết quả |
|---|---|---|---|---|---|
| TC-PRF-01 | Bắt buộc đăng nhập | `GET /api/profile` không token | 401 | `ProfileTests.Profile_RequiresLogin` | Đạt |
| TC-PRF-02 | Đổi họ tên | Tên toàn khoảng trắng; tên hợp lệ | 400; 200 và `/api/auth/me` thấy tên mới | `ProfileTests.UpdateName_Validates_AndPersists` | Đạt |
| TC-PRF-03 | Đổi mật khẩu | Nhập lại không khớp; sai mật khẩu hiện tại; trùng mật khẩu cũ; hợp lệ | 400, 400, 400, 204; mật khẩu cũ hết dùng được, mật khẩu mới đăng nhập được | `ProfileTests.ChangePassword_RequiresCurrentPassword_AndMatchingConfirmation` | Đạt |
| TC-PRF-04 | Ảnh đại diện | Upload → thay ảnh → gỡ ảnh → upload ảnh giả | Ảnh cũ bị xóa mỗi lần đổi; ảnh giả bị từ chối (400) | `ProfileTests.Avatar_UploadReplaceRemove_CleansUpOldFiles` | Đạt |
| TC-PRF-05 | Địa chỉ mặc định | SĐT sai; lưu hợp lệ; xóa | 400; lưu đúng; trả về rỗng | `ProfileTests.DefaultAddress_SaveAndClear` | Đạt |

### 2.10. Góp ý

| Mã TC | Chức năng | Các bước / dữ liệu | Kết quả mong đợi | Test tự động | Kết quả |
|---|---|---|---|---|---|
| TC-FB-01 | Khách vãng lai góp ý | Gửi không đăng nhập | 201, không gắn tài khoản | `FeedbackTests.Anonymous_CanSendFeedback` | Đạt |
| TC-FB-02 | Thành viên góp ý | Gửi khi đã đăng nhập | 201, gắn tài khoản | `FeedbackTests.Member_Feedback_IsLinkedToAccount` | Đạt |
| TC-FB-03 | Góp ý sai dữ liệu | Tên rỗng, email sai, chủ đề lạ, 9 sao, nội dung ngắn | 400, lỗi 5 trường | `FeedbackTests.InvalidFeedback_Returns400PerField` | Đạt |
| TC-FB-04 | Quản lý góp ý | Lọc chưa đọc → đánh dấu đã đọc → xóa | Thấy góp ý; hết khỏi danh sách chưa đọc; 204 | `FeedbackTests.Admin_CanFilterUnread_MarkRead_AndDelete` | Đạt |

### 2.11. Khu quản trị MVC và bảo mật web

| Mã TC | Chức năng | Các bước / dữ liệu | Kết quả mong đợi | Test tự động | Kết quả |
|---|---|---|---|---|---|
| TC-MVC-01 | Bắt buộc đăng nhập | Vào `/Admin/Products` khi chưa đăng nhập | Chuyển tới trang đăng nhập kèm `ReturnUrl` | `AdminMvcTests.AdminArea_RedirectsAnonymousToLogin` | Đạt |
| TC-MVC-02 | Form đăng nhập | Mở trang đăng nhập | Có anti-forgery token và kiểm tra phía trình duyệt (`data-val`) | `AdminMvcTests.LoginForm_ContainsAntiforgeryToken_AndClientValidation` | Đạt |
| TC-MVC-03 | **Chống CSRF** | POST đăng nhập không có anti-forgery token | 400 | `AdminMvcTests.Post_WithoutAntiforgeryToken_IsRejected` | Đạt |
| TC-MVC-04 | Cookie an toàn | Admin đăng nhập | Cookie `HttpOnly`, `SameSite=Lax`; vào được Dashboard | `AdminMvcTests.AdminLogin_SetsHttpOnlyCookie_AndOpensDashboard` | Đạt |
| TC-MVC-05 | Khách vào khu quản trị | Tài khoản khách hàng đăng nhập trang Admin | Báo "không có quyền quản trị", không vào được | `AdminMvcTests.CustomerAccount_CannotEnterAdminArea` | Đạt |
| TC-MVC-06 | **Chống Open Redirect** | `ReturnUrl=https://evil.example.com/phish` | Chuyển về `/Admin`, bỏ qua link ngoài | `AdminMvcTests.Login_IgnoresExternalReturnUrl_OpenRedirect` | Đạt |
| TC-MVC-07 | Đăng xuất | Đăng xuất rồi vào Dashboard | Chuyển về trang đăng nhập | `AdminMvcTests.Logout_EndsSession` | Đạt |
| TC-MVC-08 | **Chống XSS** | Góp ý chứa `<script>alert(...)</script>`, admin mở trang Góp ý | Hiển thị dạng chữ đã mã hóa, không có thẻ `<script>` thật | `AdminMvcTests.FeedbackPage_EncodesUserContent_XssSafe` | Đạt |
| TC-MVC-09 | AJAX | Lọc món với header `X-Requested-With` | Chỉ trả bảng (partial view), không cả trang | `AdminMvcTests.AjaxRequest_ReturnsPartialTableOnly` | Đạt |
| TC-MVC-10 | Xử lý lỗi | Trang MVC không tồn tại; API không tồn tại | Trang HTML "Không tìm thấy trang" / JSON ProblemDetails | `AdminMvcTests.Errors_AreHtmlForMvc_AndProblemJsonForApi` | Đạt |
| TC-MVC-11 | Header bảo mật | Mở trang bất kỳ | `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff` | `AdminMvcTests.SecurityHeaders_ArePresent` | Đạt |

### 2.12. Chống dò mật khẩu và spam (giới hạn thật)

| Mã TC | Chức năng | Các bước / dữ liệu | Kết quả mong đợi | Test tự động | Kết quả |
|---|---|---|---|---|---|
| TC-RL-01 | Rate limit đăng nhập | 11 lần đăng nhập sai trong 1 phút | 10 lần đầu 401, lần 11 **429** | `RateLimitTests.Login_11thAttemptWithinAMinute_Returns429` | Đạt |
| TC-RL-02 | Rate limit góp ý | 6 góp ý trong 10 phút | 5 lần đầu 201, lần 6 **429** | `RateLimitTests.Feedback_6thWithin10Minutes_Returns429` | Đạt |

### 2.13. Đánh giá món và món bán chạy

| Mã TC | Chức năng | Các bước / dữ liệu | Kết quả mong đợi | Test tự động | Kết quả |
|---|---|---|---|---|---|
| TC-REV-01 | Chỉ người đã mua được đánh giá | Chưa mua; đơn đang giao; đơn đã hủy | Đều 403; `canReview = false` | `ReviewTests.CannotReview_WithoutPurchase_OrBeforeOrderCompleted` | Đạt |
| TC-REV-02 | Đánh giá + điểm trung bình | 2 khách có đơn hoàn thành đánh giá 5★ và 2★, khách thứ 2 sửa thành 4★ | Mỗi khách 1 đánh giá (gửi lại = sửa); trung bình 4.5, 2 lượt, phân bố đúng; điểm trên món được cập nhật | `ReviewTests.AfterCompletedOrder_CanReview_OncePerProduct_AndAverageIsUpdated` | Đạt |
| TC-REV-03 | Đánh giá sai dữ liệu | 6 sao, nhận xét 501 ký tự | 400, lỗi `rating`, `comment` | `ReviewTests.InvalidRating_Returns400` | Đạt |
| TC-REV-04 | Xóa đánh giá | Admin xóa đánh giá không phù hợp; khách tự xóa đánh giá của mình | 204; điểm trung bình tính lại; hết đánh giá thì về 0 | `ReviewTests.OwnerAndAdmin_CanDeleteReview_RatingIsRecalculated` | Đạt |
| TC-REV-05 | Quyền quản trị đánh giá | Khách hàng gọi API quản trị đánh giá | 403 | `ReviewTests.Customer_CannotUseAdminReviewApi` | Đạt |
| TC-REV-06 | Đánh giá của món đang ẩn | Xem đánh giá của món thuộc danh mục đã ẩn | 404 | `ReviewTests.ReviewsOfHiddenProduct_AreNotPublic` | Đạt |
| TC-TOP-01 | Món bán chạy | 99 phần (đơn hoàn thành), 50 phần (đơn hủy), 30 phần (đang giao) | Chỉ tính 99 phần, doanh thu 990.000đ, đứng đầu cả trang Admin lẫn trang chủ | `ReviewTests.TopProducts_CountOnlyCompletedOrders` | Đạt |
| TC-TOP-02 | Trang Admin | Mở Tổng quan; mở trang Đánh giá có nhận xét chứa `<script>` | Có bảng "Món bán chạy" chứa món; nhận xét được mã hóa (chống XSS) | `ReviewTests.AdminMvc_DashboardShowsTopProducts_AndReviewsPageEncodesComments` | Đạt |

---

## 3. Test case thủ công

Những phần cần mắt người hoặc thiết bị thật (giao diện, hiệu ứng, điện thoại, bản đồ). Nhóm điền 3 cột cuối khi kiểm thử.

| Mã TC | Chức năng | Các bước | Kết quả mong đợi | Kết quả thực tế | Người test | Ngày |
|---|---|---|---|---|---|---|
| MT-01 | Banner trang chủ | Mở trang chủ, chờ 5 giây; bấm mũi tên trái/phải | Banner tự chuyển; mũi tên chuyển ảnh; ảnh không bị mất chữ | | | |
| MT-02 | Tab danh mục | Bấm từng tab; cuộn trang lên xuống | Bấm tab cuộn tới đúng khu; tab tự đổi theo khu đang xem; thanh tab dính dưới header | | | |
| MT-03 | Tìm món ở trang chủ | Gõ "pho mai" + Enter | Chuyển sang trang Thực đơn, hiện "Viên phô mai" | | | |
| MT-04 | Giỏ hàng | Thêm 2 món, đổi số lượng, xóa 1 món, tải lại trang (F5) | Số trên biểu tượng giỏ đúng; giỏ vẫn còn sau F5 | | | |
| MT-05 | Thanh toán khi chưa đăng nhập | Chưa đăng nhập → Giỏ hàng → Thanh toán | Chuyển tới trang đăng nhập, có thông báo; giỏ hàng vẫn giữ | | | |
| MT-06 | Thanh toán bằng địa chỉ mặc định | Lưu địa chỉ mặc định → thanh toán | Địa chỉ mặc định được chọn sẵn; đặt hàng thành công | | | |
| MT-07 | Thanh toán địa chỉ mới | Chọn "Giao tới địa chỉ khác", tick "Đặt làm mặc định", đặt hàng, mở lại thanh toán | Địa chỉ vừa nhập trở thành mặc định | | | |
| MT-08 | Theo dõi đơn | Đặt hàng → "Theo dõi đơn hàng"; admin đổi trạng thái; bấm "Làm mới" | Tiến độ đơn cập nhật đúng bước | | | |
| MT-09 | Ảnh đại diện | Tài khoản của tôi → Đổi ảnh | Ảnh mới hiện ngay ở trang hồ sơ và trên header | | | |
| MT-10 | Đổi mật khẩu (giao diện) | Đổi mật khẩu → đăng xuất → đăng nhập bằng mật khẩu mới | Đăng nhập được; mật khẩu cũ báo sai | | | |
| MT-11 | Tự đăng xuất khi bị khóa | Khách đang đăng nhập ở React; admin khóa tài khoản; khách bấm "Đơn của tôi" | Bị đưa về trang đăng nhập kèm thông báo | | | |
| MT-12 | Nút liên hệ nổi | Cuộn trang xuống > 300px rồi cuộn lên đầu | 3 nút hiện ra khi cuộn xuống, ẩn khi ở đầu trang | | | |
| MT-13 | Nút điện thoại | Bấm nút đỏ; trên điện thoại bấm "Gọi ngay" | Hiện "Có góp ý xin vui lòng liên hệ số điện thoại 0974 679 274"; điện thoại mở trình gọi | | | |
| MT-14 | Gửi góp ý | Bấm nút xanh → điền form → Gửi; admin mở trang Góp ý | Hiện "Cảm ơn bạn đã góp ý!"; góp ý có trong Admin, menu có số chưa đọc | | | |
| MT-15 | Bản đồ | Bấm bản đồ nhỏ ở footer / nút cam; bấm "Chỉ đường" | Bản đồ lớn có biểu tượng ngôi nhà đúng vị trí shop; mở Google Maps | | | |
| MT-16 | Về chúng tôi | Menu → Về chúng tôi | Hiện thông tin hệ thống và 2 thành viên | | | |
| MT-17 | Admin: lọc AJAX | Admin → Món ăn → gõ từ khóa, đổi danh mục | Bảng cập nhật không tải lại cả trang; URL đổi theo bộ lọc | | | |
| MT-18 | Admin: Session nhớ bộ lọc | Lọc "burger" → sửa một món → Lưu | Quay lại danh sách vẫn đang lọc "burger" | | | |
| MT-19 | Admin: ảnh món | Thêm món, chọn file ảnh / chọn ảnh thư viện | Xem trước ảnh ngay khi chọn; lưu xong ảnh hiện ở trang khách | | | |
| MT-20 | Admin: TempData + xác nhận xóa | Xóa một món | Hộp xác nhận trước khi xóa; sau khi xóa hiện thông báo xanh một lần | | | |
| MT-21 | Admin: khóa tài khoản AJAX | Người dùng → bấm "Khóa" | Nút và nhãn trạng thái đổi ngay, không tải lại trang | | | |
| MT-22 | Admin: kiểm tra phía trình duyệt | Thêm món, để trống tên, bấm Lưu | Lỗi hiện ngay dưới ô, không gửi lên server | | | |
| MT-23 | Swagger | Mở `/swagger` → login lấy token → Authorize → gọi `GET /api/admin/stats` | Trả 200; API có ổ khóa khi chưa Authorize thì trả 401 | | | |
| MT-24 | Responsive trang khách | DevTools → iPhone SE (375px): trang chủ, thực đơn, giỏ, thanh toán, hồ sơ | Không tràn ngang; nút bấm được; menu thu gọn | | | |
| MT-25 | Responsive trang Admin | DevTools → 375px: Tổng quan, Món ăn, Đơn hàng | Sidebar thành nút menu; bảng cuộn ngang trong khung, không tràn trang | | | |
| MT-26 | Viết đánh giá (giao diện) | Đơn của tôi → đơn đã hoàn thành → "Đánh giá" → chọn sao, viết nhận xét → Gửi; sau đó Sửa và Xóa | Đánh giá hiện trong danh sách; điểm trung bình và số lượt trên trang món / thẻ món cập nhật | | | |
| MT-27 | Món bán chạy | Hoàn thành vài đơn ở Admin → mở trang chủ và Admin → Tổng quan | Trang chủ có tab "Bán chạy"; Tổng quan có bảng Top 5 với thanh tỉ lệ | | | |

---

## 4. Truy vết yêu cầu của đề → test case

| Yêu cầu (đề bài) | Test case |
|---|---|
| 3.1 Hai vai trò, phân quyền phía server | TC-AUTH-02, TC-AZ-01 → 05, TC-MVC-01, TC-MVC-05, TC-UP-07, TC-REV-01, TC-REV-05 |
| 3.3 EF Core, quan hệ, LINQ; migrations | TC-SYS-01, TC-SYS-02, TC-PRD-05, TC-PRD-07, TC-ORD-08, TC-ORD-10, TC-REV-02, TC-TOP-01 |
| 3.3 Phân trang, lọc, AJAX | TC-BR-09, TC-PRD-01, TC-PRD-02, TC-FB-04, TC-MVC-09, MT-17 |
| 3.3 Thêm / Sửa / Xóa | TC-PRD-06 → 11, TC-FB-04, TC-PRF-02 → 05, TC-REV-02, TC-REV-04, MT-19, MT-20, MT-26 |
| 3.3 Quản lý trạng thái (Session, Cookie, TempData, token) | TC-AUTH-08, TC-MVC-04, TC-MVC-07, MT-18, MT-20 |
| 3.3 Kiểm tra dữ liệu + Data Annotations | TC-VAL-01 → 08, TC-AUTH-04, TC-PRD-09, TC-ORD-04, TC-FB-03, MT-22 |
| 3.3 Web API RESTful + Swagger | Toàn bộ nhóm 2.4 → 2.10 (gọi qua HTTP), MT-23 |
| 3.3 Upload file / ảnh | TC-UP-01 → 08, TC-PRD-11, TC-PRF-04, MT-19 |
| 3.3 Xử lý ngoại lệ ở cấp ứng dụng | TC-PRD-04, TC-MVC-10 |
| 3.3 Ghi log | Kiểm tra thủ công qua log khi chạy (đăng nhập sai, tạo đơn, thao tác admin) |
| 3.4 Responsive | MT-24, MT-25 |
| 3.5 Xác thực, phân quyền phía server | TC-AUTH-06 → 08, TC-AZ-01 → 05 |
| 3.5 Băm mật khẩu | TC-AUTH-05 |
| 3.5 Chống CSRF | TC-MVC-02, TC-MVC-03, TC-AZ-02 |
| 3.5 Kiểm tra dữ liệu phía server | Nhóm 2.2, TC-ORD-01 (bỏ qua tổng tiền client gửi) |
| 3.5 Upload an toàn (định dạng, kích thước) | TC-UP-01 → 06, TC-BR-10 → 13 |
| 3.5 Không viết cứng bí mật | TC-SYS-01 (test chạy bằng cấu hình riêng, không đọc User Secrets) |
| 3.5 Không tin vai trò / quyền từ client | TC-AUTH-02, TC-ORD-08, TC-AZ-04 |
| 3.5 Lỗ hổng phổ biến khác | TC-MVC-06 (Open Redirect), TC-MVC-08 + TC-TOP-02 (XSS), TC-MVC-11 (header), TC-RL-01, TC-RL-02 (dò mật khẩu, spam), TC-AUTH-07 (dò email), TC-UP-01 (path traversal) |

---

## 5. Lịch sử chạy kiểm thử

| Ngày | Loại | Kết quả | Ghi chú |
|---|---|---|---|
| 12/11/2026 | Tự động (3 lần liên tiếp) | 130 / 130 đạt | CSDL tạm được tự xóa sau mỗi lần chạy |
| 12/11/2026 | Tự động (thêm đánh giá món + bán chạy) | 138 / 138 đạt | |
| | Thủ công | | Nhóm điền sau khi kiểm thử mục 3 |
