# SchedulingApp — Hệ thống Xếp ca làm việc

> Bộ scaffold hoàn chỉnh cho hệ thống "Tối ưu hóa bài toán xếp ca làm việc đa công ty, đa phòng ban, đa vị trí".

---

## Cấu trúc tổng thể Chuẩn

```
SchedulingApp/
├── BE/          → Backend ASP.NET Core 10 (Clean Architecture + EF Core + SQL Server)
│   ├── README.md
│   ├── HUONG_DAN_CAM_THUAT_TOAN.md
│   ├── SchedulingApp.sln
│   ├── src/
│   │   ├── SchedulingApp.Domain/
│   │   │   └── Entities/ (Company, Department, Position, CompanyPosition, Employee, Shift, AlgorithmRun, Common/BaseEntity...)
│   │   ├── SchedulingApp.Application/
│   │   │   ├── Common/ (ApiResponse.cs, AlgorithmNames.cs)
│   │   │   ├── DTOs/ (CompanyDto, DepartmentDto, EmployeeDto, ShiftDto, ScheduleResultDto, SolverInputDto...)
│   │   │   ├── Interfaces/ (ISolver, ISolverFactory, IConstraintValidator)
│   │   │   ├── Mappings/ (MappingProfile.cs)
│   │   │   └── Solver/ (SolverFactory.cs, FakeGreedySolver, GaSolver, SaSolver, HybridSolver)
│   │   ├── SchedulingApp.Infrastructure/
│   │   │   ├── Persistence/ (AppDbContext.cs, FaceAttendanceDbContext.cs, Migrations/)
│   │   │   └── SeedData/ (SeedData.cs)
│   │   └── SchedulingApp.Api/
│   │       ├── Controllers/ (Companies, Departments, Positions, CompanyPositions, Employees, Shifts, Users, Schedule, AlgorithmRuns, Dashboard, Health)
│   │       ├── Middleware/ (ExceptionHandlingMiddleware.cs)
│   │       ├── appsettings.json, appsettings.Development.json, appsettings.Development.json.example
│   │       └── Program.cs
│   └── tests/
│       └── SchedulingApp.Tests/
├── FE/          → Frontend React 19 + Vite 8 + TypeScript + Tailwind CSS v4 + Recharts
│   ├── README.md
│   └── src/
│       ├── components/ (layout/, common/, charts/, ui/)
│       ├── pages/ (Dashboard, Categories, Users, Employees, Shifts, Schedule, ValidatorLog)
│       ├── services/ (api.ts)
│       ├── types/ (company.types.ts, employee.types.ts, shift.types.ts, schedule.types.ts, algorithmRun.types.ts, constants.ts, index.ts)
│       ├── lib/ (utils.ts)
│       ├── App.tsx
│       └── main.tsx
├── HUONG_DAN_CHAY_HE_THONG.md
├── .gitignore
└── README.md
```

---

## Khởi động nhanh (Quick Start)

> 📋 Hướng dẫn chi tiết từ đầu đến cuối (cài đặt, chạy, đọc log): **[HUONG_DAN_CHAY_HE_THONG.md](HUONG_DAN_CHAY_HE_THONG.md)**

Cần 2 terminal song song:

**Terminal 1 — Backend:**
```bash
cd BE/src/SchedulingApi
dotnet run
# → http://localhost:5000 | Health: http://localhost:5000/api/v1/health
```

**Terminal 2 — Frontend:**
```bash
cd FE
npm run dev
# → http://localhost:5173
```

---

## Tài liệu chi tiết

- **Backend** → xem [`BE/README.md`](BE/README.md)
- **Frontend** → xem [`FE/README.md`](FE/README.md)
- **Hướng dẫn Cắm thuật toán cho 4 thành viên** → xem [`BE/HUONG_DAN_CAM_THUAT_TOAN.md`](BE/HUONG_DAN_CAM_THUAT_TOAN.md)
