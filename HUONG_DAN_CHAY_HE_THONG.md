# Hướng dẫn chạy toàn bộ hệ thống từ đầu đến cuối

> Dành cho: toàn bộ nhóm. Đọc **1 lần duy nhất** — sau đó mỗi người chỉ cần `dotnet run` và `npm run dev`.

---

## Yêu cầu cài đặt (chỉ cần cài 1 lần)

| Phần mềm | Tải tại | Kiểm tra |
|----------|---------|---------|
| .NET 8 SDK | https://dot.net/download | `dotnet --version` → phải ra `8.x.x` |
| Node.js 18+ | https://nodejs.org | `node --version` → phải ra `v18+` |
| SQL Server LocalDB | Cài kèm VS 2022 / SQL Server Express | `sqllocaldb i` → thấy `MSSQLLocalDB` |
| EF Core CLI | `dotnet tool install -g dotnet-ef` | `dotnet ef --version` |

> **Nếu không có LocalDB (Mac/Linux):** Dùng Docker — xem phần cuối file này.

---

## Bước 1 — Clone và chuẩn bị

```bash
git clone <repo-url>
cd SchedulingApp
```

---

## Bước 2 — Khởi tạo Database (chỉ cần làm 1 lần)

```bash
cd BE/src/SchedulingApp.Api

# Tạo migration (nếu chưa có — thường đã có sẵn trong repo)
dotnet ef migrations add InitialCreate --project ../SchedulingApp.Infrastructure --startup-project .

# Áp dụng migration và tạo DB
dotnet ef database update --project ../SchedulingApp.Infrastructure --startup-project .
```

> **Lưu ý:** Khi API chạy, nó tự gọi `context.Database.Migrate()` và `SeedData.Initialize()` nên **sau bước này bạn không cần chạy tay nữa**.

---

## Bước 3 — Chạy Backend

Mở **Terminal 1**, chạy:

```bash
cd BE/src/SchedulingApp.Api
dotnet run
```

Kết quả mong đợi:
```
HH:mm:ss info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
HH:mm:ss info: Microsoft.Hosting.Lifetime[0]
      Application started.
```

Kiểm tra API hoạt động:
- Swagger UI → http://localhost:5000/swagger
- Thử gọi `GET /api/v1/companies` → phải trả về danh sách công ty từ SeedData

---

## Bước 4 — Chạy Frontend

Mở **Terminal 2** (để song song với Terminal 1), chạy:

```bash
cd FE

# Lần đầu: tạo file .env
copy .env.example .env        # Windows CMD
# Copy-Item .env.example .env # Windows PowerShell
# cp .env.example .env        # macOS / Linux

# Lần đầu: cài thư viện
npm install

# Chạy FE
npm run dev
```

Kết quả mong đợi:
```
  VITE v8.x.x  ready in xxx ms
  ➜  Local:   http://localhost:5173/
```

Mở trình duyệt → http://localhost:5173

---

## Bước 5 — Kiểm tra end-to-end

1. Truy cập http://localhost:5173 → thấy sidebar 7 mục
2. Click **"Danh mục"** → bảng công ty/phòng ban/vị trí load dữ liệu
3. Click **"Nhân viên"** → thấy nhân viên từ SeedData
4. Click **"Lịch phân công"** → bấm **"Chạy xếp lịch"** (algorithm = greedy)
5. Quan sát Terminal 1 (Backend) — phải thấy log:

```
HH:mm:ss info: ▶ Bắt đầu xếp lịch | Algorithm=greedy | Shifts=1 | Employees=1
HH:mm:ss info: ✅ Hoàn thành xếp lịch | Algorithm=greedy | Shifts=1 | Unfilled=0 | Penalty=0 | Time=2ms
```

---

## Đọc log cảnh báo (khi solver bị sai logic)

Log hiện ra **ngay trong Terminal chạy `dotnet run`**. Không cần mở file riêng.

### Ý nghĩa từng mức log

| Ký hiệu | Level | Ý nghĩa |
|---------|-------|---------|
| `▶ info` | Information | Bắt đầu chạy solver — bình thường |
| `✅ info` | Information | Solver chạy xong — bình thường |
| `ℹ info` | Information | Có rejection log / soft penalty log |
| `⚠ warn` | **Warning** | **Solver trả về kết quả sai logic — cần kiểm tra** |
| `❌ error` | Error | Exception / crash toàn hệ thống |

### Các cảnh báo phổ biến và cách sửa

| Thông báo cảnh báo | Nguyên nhân | Cách sửa |
|-------------------|-------------|---------|
| `ExecutionTimeMs = 0` | Solver chưa dùng `Stopwatch` | Thêm `var sw = Stopwatch.StartNew()` đầu `Run()`, `result.ExecutionTimeMs = sw.ElapsedMilliseconds` cuối |
| `TotalPenaltyScore < 0` | Công thức tính penalty sai | Kiểm tra logic tính điểm — phải luôn >= 0 |
| `Solver trả về 0 ca` | `input.ShiftIds` rỗng hoặc solver không đọc input | Kiểm tra FE có truyền `shiftIds` không, kiểm tra vòng lặp trong solver |
| `HoursByEmployee rỗng` | Solver không tính giờ làm | Thêm tích lũy giờ khi gán nhân viên vào ca |
| `Phân công TRÙNG GIỜ` | Solver không check overlap | Vi phạm ràng buộc cứng HC1 — xem `IConstraintValidator` |
| `Dư người trong ca` | Logic đếm nhân viên sai | Kiểm tra điều kiện dừng vòng lặp gán |

### Ví dụ output log khi solver GA bị lỗi (chưa dùng Stopwatch)

```
08:30:01 info: ▶ Bắt đầu xếp lịch | Algorithm=ga | Shifts=5 | Employees=3
08:30:02 warn: ⚠ [ga] ExecutionTimeMs = 0. Solver chưa dùng Stopwatch → màn hình So sánh thuật toán sẽ trống.
08:30:02 warn: ⚠ [ga] HoursByEmployee rỗng — thống kê giờ làm sẽ không hiển thị ở FE.
08:30:02 info: ✅ Hoàn thành xếp lịch | Algorithm=ga | Shifts=5 | Unfilled=2 | Penalty=200 | Time=0ms
```

→ Nhìn vào là biết ngay thành viên GA **chưa viết Stopwatch và chưa tính HoursByEmployee**.

---

## Chạy test độc lập (không cần DB, không cần FE)

```bash
cd BE

# Test tất cả
dotnet test

# Test riêng từng người
dotnet test --filter "FullyQualifiedName~GreedySolverTests"
dotnet test --filter "FullyQualifiedName~GaSolverTests"
dotnet test --filter "FullyQualifiedName~SaSolverTests"
dotnet test --filter "FullyQualifiedName~HybridSolverTests"
dotnet test --filter "FullyQualifiedName~ConstraintValidatorTests"

# Xem kết quả chi tiết
dotnet test --logger "console;verbosity=detailed"
```

---

## Dùng Docker thay LocalDB (Mac/Linux)

**Bước 1** — Khởi động SQL Server:
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 --name sqlserver -d \
  mcr.microsoft.com/mssql/server:2022-latest
```

**Bước 2** — Sửa connection string trong `BE/src/SchedulingApp.Api/appsettings.json`:
```json
"DefaultConnection": "Server=localhost,1433;Database=SchedulingAppDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
```

**Bước 3** — Chạy bình thường như trên.

Để dừng / xoá container:
```bash
docker stop sqlserver
docker rm sqlserver
```

---

## Checklist nhanh mỗi lần mở máy lên code

```
□ Terminal 1: cd BE/src/SchedulingApp.Api && dotnet run
              → Thấy "Now listening on: http://localhost:5000" là OK
□ Terminal 2: cd FE && npm run dev
              → Thấy "http://localhost:5173/" là OK
□ Mở http://localhost:5173 → dữ liệu load được là end-to-end OK
□ Sau khi sửa solver: cd BE && dotnet test --filter "XxxSolverTests"
```
