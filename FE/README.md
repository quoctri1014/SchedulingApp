# Frontend — SchedulingApp

> React 19 + Vite 8 + TypeScript + Tailwind CSS v4 + Axios + Recharts

---

## Cấu trúc thư mục Chuẩn (fe/)

```
fe/
├── public/
├── src/
│   ├── assets/                    ← Hình ảnh, icon tĩnh
│   ├── components/
│   │   ├── layout/
│   │   │   ├── Sidebar.tsx        ← Thanh điều hướng chính
│   │   │   ├── Header.tsx         ← Tiêu đề trang
│   │   │   └── MainLayout.tsx     ← Wrapper layout bọc ngoài toàn bộ app
│   │   ├── common/                ← Reusable common components
│   │   │   ├── StatusBadge.tsx    ← Badge màu trạng thái
│   │   │   ├── EmptyState.tsx     ← UI khi dữ liệu rỗng
│   │   │   ├── ErrorState.tsx     ← UI khi API báo lỗi
│   │   │   ├── AlgorithmBadge.tsx ← Badge màu thuật toán (Greedy, GA, SA, Hybrid)
│   │   │   └── ElapsedTimer.tsx   ← Timer đếm thời gian chạy thực tế (100ms)
│   │   ├── charts/                ← Recharts wrappers
│   │   └── ui/
│   │       ├── Button.tsx
│   │       ├── Card.tsx
│   │       ├── Badge.tsx
│   │       └── Table.tsx
│   ├── pages/
│   │   ├── Dashboard.tsx          ← Recharts charts (Execution time, Penalty score) + Aggregate Runs table + CSV Export + Copy Quote Report
│   │   ├── Categories.tsx         ← 3 tab: Công ty / Phòng ban / Vị trí
│   │   ├── Users.tsx              ← Quản lý người dùng FaceAttendanceSystem & Phân công
│   │   ├── Employees.tsx          ← Bảng nhân viên + assignments/leaves/preferences
│   │   ├── Shifts.tsx             ← Bảng ca làm việc
│   │   ├── Schedule.tsx           ← Run single / Run all batch algorithms + Real-time timer
│   │   └── ValidatorLog.tsx       ← Hướng dẫn log server
│   ├── services/
│   │   └── api.ts                 ← Axios client với 30s timeout + interceptor unwrap ApiResponse
│   ├── types/                     ← TypeScript interfaces
│   │   ├── company.types.ts       ← Company, Department, Position, CompanyPosition
│   │   ├── employee.types.ts      ← Employee, EmployeeAssignment, EmployeeLeave, EmployeePreference
│   │   ├── shift.types.ts         ← Shift
│   │   ├── schedule.types.ts      ← SolverInput, ScheduleResult, AssignedEmployee
│   │   ├── algorithmRun.types.ts  ← AlgorithmRun, AlgorithmSummary
│   │   ├── constants.ts           ← ALGORITHM_NAMES & AlgorithmType
│   │   └── index.ts               ← Re-export toàn bộ types
│   ├── lib/
│   │   └── utils.ts               ← cn() helper cho Tailwind CSS
│   ├── App.tsx                    ← Root component + Sidebar + BrowserRouter
│   ├── main.tsx                   ← Entry point
│   └── index.css                  ← Styling system
├── .env.example
├── .env                           ← Biến môi trường
├── .gitignore
├── index.html
├── package.json
└── vite.config.ts
```

---

## Danh sách Màn hình

| Route | Trang | Nguồn dữ liệu |
|-------|-------|---------------|
| `/` | Dashboard | `GET /api/v1/dashboard/summary`, `GET /api/v1/algorithmruns/summary` |
| `/categories` | Danh mục (3 tab) | `GET /api/v1/companies`, `GET /api/v1/departments`, `GET /api/v1/positions` |
| `/users` | Người dùng & Phân công | `GET /api/v1/users` (FaceAttendanceSystem CSDL) |
| `/employees` | Nhân viên | `GET /api/v1/employees`, `GET /api/v1/employees/{id}/assignments` |
| `/shifts` | Ca làm việc | `GET /api/v1/shifts` |
| `/schedule` | Lịch phân công | `POST /api/v1/schedule/run`, `POST /api/v1/schedule/run-all` |
| `/validator-log` | Log Validator | Hướng dẫn xem log server `be/logs/scheduling-{ngày}.log` |
