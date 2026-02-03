# 🏠 Motel Management System

**Hệ thống quản lý nhà trọ – ASP.NET Core MVC (.NET 8)**

---

## 📌 Giới thiệu | Introduction

### 🇻🇳 Tiếng Việt

**Motel Management System** là **đồ án môn học** xây dựng hệ thống quản lý nhà trọ, hỗ trợ chủ trọ trong việc:

* Quản lý nhà trọ và phòng
* Quản lý người thuê và hợp đồng
* Theo dõi điện nước và hóa đơn hàng tháng
* Thanh toán hóa đơn *(dự kiến tích hợp ZaloPay)*
* Lưu trữ file hợp đồng (scan)

Dự án được thiết kế theo hướng **thực tế**, sử dụng **Data First**, **soft delete**, **ràng buộc dữ liệu chặt chẽ ở tầng database**, phù hợp cho làm việc nhóm và mở rộng sau này.

---

### 🇺🇸 English

**Motel Management System** is a **university assignment project** simulating a real-world motel management system.

It helps landlords manage:

* Properties and rooms
* Tenants and rental contracts
* Monthly utility bills
* Payments *(ZaloPay integration – planned)*
* Contract file storage

The project follows a **Data First approach** with strong **database constraints**, **soft delete**, and is designed for teamwork and future extensibility.

---

## 🎯 Đối tượng sử dụng | Target Audience

* 👨‍🏫 Lecturers / Instructors
* 👩‍💻 IT Students
* 💼 Recruiters / HR
* 🏘️ Landlords (business perspective)

---

## 🛠️ Công nghệ sử dụng | Tech Stack

* **.NET 8**
* **ASP.NET Core MVC**
* **Entity Framework Core – Data First**
* **SQL Server (SSMS 2022)**
* **ASP.NET Core Identity**
* **ZaloPay** *(planned)*

---

## 📐 Thiết kế hệ thống | System Design

### 🗂️ Database Design (Data First)

Database được thiết kế trực tiếp bằng SQL, sau đó **scaffold model bằng EF Core**.

Các kỹ thuật chính:

* **Soft Delete** (`IsDeleted`)
* **Unique Filtered Index**
* **Trigger chặn xóa phòng khi còn hợp đồng active**
* **Stored Procedure xóa mềm nhà trọ**

#### 🔑 Ràng buộc nghiệp vụ quan trọng

* Mỗi phòng **chỉ có 1 hợp đồng active**
* Mỗi phòng **chỉ có 1 người thuê chính (primary occupancy)**
* Mỗi phòng **chỉ có 1 hóa đơn / tháng**
* Không cho xóa phòng nếu còn hợp đồng hiệu lực

👉 Thiết kế DB nhằm **chặn lỗi nghiệp vụ ngay từ tầng dữ liệu**, không phụ thuộc hoàn toàn vào code.

---

## 📦 Chức năng chính | Main Features

### 👤 Tài khoản & phân quyền

* Đăng nhập bằng ASP.NET Core Identity
* Mỗi chủ trọ gắn với một tài khoản người dùng

### 🏠 Quản lý nhà trọ

* Tạo / cập nhật / xóa mềm nhà trọ
* Một chủ trọ có thể quản lý nhiều nhà trọ

### 🚪 Quản lý phòng

* Trạng thái: Trống / Đang thuê / Bảo trì
* Giá phòng, tiền cọc
* Lịch sử điện nước

### 👥 Người thuê & hợp đồng

* Quản lý thông tin người thuê
* Theo dõi người đang ở trong phòng
* Hợp đồng thuê (thời hạn, trạng thái)

### 💡 Điện – Nước – Hóa đơn

* Ghi chỉ số điện nước theo tháng
* Tạo hóa đơn hàng tháng
* Chi tiết từng khoản chi phí

### 💰 Thanh toán *(Planned)*

* Tạo Payment Intent
* Thanh toán bằng QR (ZaloPay)
* Cập nhật trạng thái hóa đơn

### 📁 Lưu trữ file

* Lưu file scan hợp đồng
* Thiết kế sẵn sàng chuyển từ local storage sang cloud

---

## 🗄️ Database Setup (Data First – BẮT BUỘC)

⚠️ **Dự án sử dụng Data First. KHÔNG dùng EF Migration để tạo database.**

### Thứ tự setup database

1. Mở **SQL Server Management Studio (SSMS 2022)**
2. Chạy file:

   ```
   /database/db.sql
   ```
3. Seed dữ liệu hệ thống (admin, landlord):

   ```
   /database/seed_core.sql
   ```
4. Seed dữ liệu demo nghiệp vụ:

   ```
   /database/seed_demo.sql
   ```

---

## ▶️ Chạy dự án | How to Run

### 1️⃣ Clone project

```bash
git clone https://github.com/your-username/motel-management-system.git
```

### 2️⃣ Cấu hình `appsettings.json`

Tạo file `appsettings.json` từ `appsettings.example.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=MotelDB;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

> ⚠️ `appsettings.json` **không được commit lên Git**.

### 3️⃣ Chạy project

```bash
dotnet restore
dotnet run
```

---

## 👨‍💻 Tài khoản mặc định (Dev Only)

| Role     | Email                                                 | Password |
| -------- | ----------------------------------------------------- | -------- |
| Admin    | [admin@motel.local](mailto:admin@motel.local)         | 123456   |
| Landlord | [landlord1@motel.local](mailto:landlord1@motel.local) | 123456   |

> Các tài khoản này được tạo bằng **SQL seed (Data First)**.

---

## 🗺️ Roadmap | Future Improvements

* ✅ ZaloPay integration
* 📄 Xuất hóa đơn PDF
* 🔔 Nhắc thanh toán tự động
* ☁️ Cloud file storage (Azure / AWS)
* 📊 Dashboard thống kê doanh thu

---

## 📜 License

This project is created for **educational purposes only**.

---