# Backend — SchedulingApp.Api

> ASP.NET Core 10 Web API theo kiến trúc Clean Architecture + Entity Framework Core + SQL Server.

---

## 👉 Bạn được phân công code 1 thuật toán?

Đọc **[HUONG_DAN_CAM_THUAT_TOAN.md](HUONG_DAN_CAM_THUAT_TOAN.md)** trước khi bắt đầu.

Bạn chỉ cần sửa **đúng 1 file** trong `Application/Solver/` — không cần hiểu toàn bộ hệ thống, không cần động vào Controller hay Database.

| Thuật toán | File của bạn | Chạy test riêng |
|-----------|-------------|-----------------|
| Greedy | `Application/Solver/FakeGreedySolver.cs` | `dotnet test --filter "GreedySolverTests"` |
| GA | `Application/Solver/GaSolver.cs` | `dotnet test --filter "GaSolverTests"` |
| SA | `Application/Solver/SaSolver.cs` | `dotnet test --filter "SaSolverTests"` |
| Hybrid | `Application/Solver/HybridSolver.cs` | `dotnet test --filter "HybridSolverTests"` |

---

## Cấu trúc thư mục Chuẩn (be/)

```
be/
├── SchedulingApp.sln
├── src/
│   ├── SchedulingApp.Domain/
│   │   └── Entities/
│   │       ├── Common/
│   │       │   └── BaseEntity.cs                   ← Base entity class (Id, IsDeleted)
│   │       ├── Company.cs
│   │       ├── Department.cs
│   │       ├── Position.cs
│   │       ├── CompanyPosition.cs
│   │       ├── Employee.cs
│   │       ├── EmployeeAssignment.cs
│   │       ├── EmployeeLeave.cs
│   │       ├── EmployeePreference.cs
│   │       ├── Shift.cs
│   │       ├── AlgorithmRun.cs
│   │       └── FaceAttendance/                     ← Entities kết nối CSDL FaceAttendanceSystem
│   │           ├── User.cs
│   │           ├── UserCompanyAssignment.cs
│   │           ├── UserDepartmentAssignment.cs
│   │           ├── FaceCompany.cs
│   │           └── FaceDepartment.cs
│   │
│   ├── SchedulingApp.Application/
│   │   ├── Common/
│   │   │   ├── ApiResponse.cs                      ← Response wrapper { success, data, errors }
│   │   │   └── AlgorithmNames.cs                   ← Constants cho tên thuật toán (greedy, ga, sa, hybrid)
│   │   ├── DTOs/
│   │   │   ├── CompanyDto.cs
│   │   │   ├── DepartmentDto.cs
│   │   │   ├── PositionDto.cs
│   │   │   ├── CompanyPositionDto.cs
│   │   │   ├── EmployeeDto.cs
│   │   │   ├── EmployeeAssignmentDto.cs
│   │   │   ├── EmployeeLeaveDto.cs
│   │   │   ├── EmployeePreferenceDto.cs
│   │   │   ├── ShiftDto.cs
│   │   │   ├── ScheduleResultDto.cs
│   │   │   ├── SolverInputDto.cs
│   │   │   ├── AlgorithmRunDto.cs
│   │   │   ├── DashboardSummaryDto.cs
│   │   │   ├── HealthCheckDto.cs
│   │   │   └── UserDtos.cs                         ← UserDto, PagedResult<T>
│   │   ├── Interfaces/
│   │   │   ├── ISolver.cs                          ← AlgorithmName + Run(SolverInput)
│   │   │   ├── ISolverFactory.cs                   ← Resolve(algorithmName), GetAvailableAlgorithms()
│   │   │   └── IConstraintValidator.cs
│   │   ├── Mappings/
│   │   │   └── MappingProfile.cs                   ← AutoMapper Profile
│   │   └── Solver/                                 ← MỖI THÀNH VIÊN CODE 1 FILE RIÊNG TẠI ĐÂY
│   │       ├── SolverFactory.cs                    ← Resolve solver động theo tên thuật toán
│   │       ├── FakeGreedySolver.cs
│   │       ├── GaSolver.cs
│   │       ├── SaSolver.cs
│   │       ├── HybridSolver.cs
│   │       └── FakeValidator.cs
│   │
│   ├── SchedulingApp.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs                     ← DbContext chính + Global Query Filters (Soft-Delete)
│   │   │   ├── FaceAttendanceDbContext.cs          ← DbContext kết nối CSDL FaceAttendanceSystem
│   │   │   └── Migrations/                         ← EF Core migrations (InitialCreate, AddAlgorithmRuns, AddSoftDeleteSupport)
│   │   └── SeedData/
│   │       └── SeedData.cs                         ← Dữ liệu mẫu khởi động
│   │
│   └── SchedulingApp.Api/
│       ├── Controllers/
│       │   ├── CompaniesController.cs
│       │   ├── DepartmentsController.cs
│       │   ├── PositionsController.cs
│       │   ├── CompanyPositionsController.cs
│       │   ├── EmployeesController.cs              ← Phân công, nghỉ phép & nguyện vọng
│       │   ├── ShiftsController.cs
│       │   ├── UsersController.cs                  ← Quản lý người dùng FaceAttendanceSystem
│       │   ├── ScheduleController.cs               ← POST /run, GET /latest, POST /run-all
│       │   ├── AlgorithmRunsController.cs          ← Thống kê & xuất CSV lịch sử thực nghiệm
│       │   ├── DashboardController.cs              ← Thống kê tổng hợp GET /summary
│       │   └── HealthController.cs                 ← Kiểm tra kết nối CSDL GET /health
│       ├── Middleware/
│       │   └── ExceptionHandlingMiddleware.cs
│       ├── Properties/
│       │   └── launchSettings.json
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── appsettings.Development.json.example
│       └── Program.cs
│
└── tests/
    └── SchedulingApp.Tests/
        ├── ConstraintValidatorTests.cs
        ├── GreedySolverTests.cs / GaSolverTests.cs / SaSolverTests.cs / HybridSolverTests.cs
        └── SchedulingApp.Tests.csproj
```

---

## Danh sách Endpoints API

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/v1/health` | Kiểm tra kết nối CSDL & trạng thái ứng dụng |
| GET | `/api/v1/dashboard/summary` | Lấy dữ liệu tổng hợp cho Dashboard (Tổng công ty, phòng ban, nhân sự, ca làm, tỉ lệ chính xác...) |
| GET / POST / PUT / DELETE | `/api/v1/companies` | CRUD Công ty (áp dụng Soft-delete cho DELETE) |
| GET / POST / PUT / DELETE | `/api/v1/departments?companyId=` | CRUD Phòng ban (áp dụng Soft-delete cho DELETE) |
| GET / POST / PUT / DELETE | `/api/v1/positions` | CRUD Vị trí |
| GET / POST / PUT / DELETE | `/api/v1/companypositions` | CRUD Vị trí thuộc Công ty |
| GET / POST / PUT / DELETE | `/api/v1/employees` | CRUD Nhân viên (áp dụng Soft-delete cho DELETE) |
| GET / POST / PUT / DELETE | `/api/v1/employees/{id}/assignments` | Phân công nhân viên (áp dụng Soft-delete cho DELETE) |
| GET / POST / DELETE | `/api/v1/employees/{id}/leaves` | Quản lý nghỉ phép nhân viên |
| GET / POST / DELETE | `/api/v1/employees/{id}/preferences` | Quản lý nguyện vọng nhân viên |
| GET / POST / PUT / DELETE | `/api/v1/shifts?date=&companyId=&deptId=` | CRUD Ca làm việc (áp dụng Soft-delete cho DELETE) |
| GET | `/api/v1/users?search=&companyId=&departmentId=` | Tìm kiếm và phân trang người dùng FaceAttendanceSystem |
| POST | `/api/v1/schedule/run?algorithm=greedy` | Chạy xếp lịch cho thuật toán chọn |
| POST | `/api/v1/schedule/run-all` | **Batch Runner**: Chạy tuần tự tất cả thuật toán để so sánh thực nghiệm |
| GET | `/api/v1/schedule/latest` | Kết quả xếp lịch gần nhất |
| GET | `/api/v1/algorithmruns` | Lấy danh sách lịch sử tất cả các lượt chạy thuật toán |
| GET | `/api/v1/algorithmruns/summary` | Lấy bảng tổng hợp chỉ số trung bình theo thuật toán |
| GET | `/api/v1/algorithmruns/export` | **Xuất Báo cáo CSV**: Tải file báo cáo tổng hợp lịch sử chạy |
