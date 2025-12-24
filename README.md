ESHOPPER — Ứng dụng bán hàng (Đồ án Lập trình Web)

Mô tả dự án ngắn gọn: ESHOPPER là một ứng dụng bán hàng (ASP.NET MVC) mẫu được xây dựng để làm đồ án môn lập trình web. Ứng dụng bao gồm chức năng quản lý sản phẩm, danh mục, người dùng, đặt hàng, và tích hợp cơ sở dữ liệu SQL Server.

**Nội dung chính**
- **Tên dự án**: ESHOPPER
- **Ngôn ngữ & nền tảng**: C#, ASP.NET MVC (Framework), Entity Framework 6
- **Môi trường phát triển**: Visual Studio (Windows), IIS / IIS Express
- **Cơ sở dữ liệu**: SQL Server (Database file: [Database.sql](Database.sql))

**Tính năng chính**
- Quản lý sản phẩm (thêm, sửa, xóa, danh sách)
- Quản lý danh mục sản phẩm
- Đăng ký/đăng nhập người dùng (cơ bản)
- Giỏ hàng và xử lý đơn hàng
- Trang quản trị (Admin) với quyền hạn cơ bản

**Cấu trúc dự án (chú ý các tệp quan trọng)**
- Thư mục mã nguồn chính: [ESHOPPER/](ESHOPPER/)
- Cấu hình ứng dụng: [ESHOPPER/appsettings.json](ESHOPPER/appsettings.json) và [Web.config](ESHOPPER/Web.config)
- Tệp dự án: [ESHOPPER/ESHOPPER.csproj](ESHOPPER/ESHOPPER.csproj)
- Tệp cơ sở dữ liệu mẫu: [Database.sql](Database.sql)

---

## Yêu cầu môi trường (Prerequisites)

- Windows 10/11
- Visual Studio 2019/2022 (phiên bản hỗ trợ ASP.NET MVC và .NET Framework)
- SQL Server Express / SQL Server (để khôi phục/restore `Database.sql`)
- .NET Framework phù hợp (kiểm tra trong `ESHOPPER.csproj`)

## Cài đặt & Chạy trên máy phát triển

1. Clone repository về máy của bạn:

```bash
git clone <repo-url>
cd "Đồ án Lập trình web"
```

2. Mở giải pháp `ESHOPPER.sln` trong Visual Studio:

- File → Open → Project/Solution → chọn [ESHOPPER.sln](ESHOPPER.sln)

3. Cấu hình cơ sở dữ liệu:
- Mở SQL Server Management Studio (SSMS) hoặc một công cụ tương tự.
- Tạo một database mới (ví dụ: `eshopper_db`) và chạy script [Database.sql](Database.sql) để khôi phục dữ liệu mẫu và cấu trúc bảng.
- Cập nhật chuỗi kết nối trong [ESHOPPER/appsettings.json](ESHOPPER/appsettings.json) hoặc `[ESHOPPER]/Web.config` (tùy ứng dụng dùng cấu hình nào) để trỏ tới database của bạn.

4. Cài đặt các gói NuGet (nếu cần):

- Trong Visual Studio: Tools → NuGet Package Manager → Package Manager Console

```powershell
Update-Package -reinstall
```

5. Chạy ứng dụng:

- Chọn cấu hình `IIS Express` hoặc `Local IIS` và nhấn `F5` để chạy debug.

## Cấu hình quan trọng

- `ESHOPPER/appsettings.json`: cấu hình ứng dụng và chuỗi kết nối. Hãy kiểm tra và chỉnh sửa giá trị `DefaultConnection` nếu cần.
- `Web.config`: cấu hình cho IIS, authentication, và cấu hình khác liên quan đến production.

## Triển khai (Deployment)

- Tùy chọn 1 — Triển khai bằng Visual Studio: Build → Publish, chọn target `IIS` hoặc `Folder`.
- Tùy chọn 2 — Triển khai lên IIS: xuất bản (Publish) ra thư mục, cấu hình site/app pool trên IIS, cập nhật chuỗi kết nối và quyền truy cập database.

## Kiểm thử & Bảo trì

- Đảm bảo restore `Database.sql` trước khi chạy chức năng liên quan đến dữ liệu mẫu.
- Nếu gặp lỗi migration hoặc lỗi Entity Framework, kiểm tra connection string và quyền truy cập database.

## Hướng dẫn đóng góp

- Fork repo → tạo branch mới từ `main` (ví dụ: `feature/<mô-tả>`)
- Thực hiện thay đổi, viết commit rõ ràng và push lên fork
- Tạo Pull Request mô tả rõ thay đổi và cách kiểm tra

## License

- Vui lòng thêm thông tin license nếu cần (ví dụ MIT). Hiện tại repository chưa chỉ định license rõ ràng.

## Liên hệ / Tài liệu thêm

- Người thực hiện đồ án: (thêm tên, email hoặc thông tin liên hệ nếu muốn)
- Tệp cấu hình và dữ liệu: xem [ESHOPPER/](ESHOPPER/) và [Database.sql](Database.sql)

---

Nếu bạn muốn, tôi có thể:
- Thêm hướng dẫn chi tiết hơn cho môi trường Windows + IIS,
- Tạo script PowerShell để tự động restore `Database.sql` và cập nhật connection string,
- Hoặc tạo file `CONTRIBUTING.md` và `LICENSE`.

"Chúc bạn thành công với đồ án!"
# Đồ án Lập trình web