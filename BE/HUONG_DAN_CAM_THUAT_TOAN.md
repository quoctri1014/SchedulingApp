# Hướng dẫn cắm thuật toán thật vào hệ thống SchedulingApp

> **Dành cho:** 4 thành viên, mỗi người phụ trách 1 thuật toán (Greedy / GA / SA / Hybrid).  
> **Thời gian đọc:** ~15 phút.  
> **Sau khi đọc xong:** bạn có thể bắt đầu code ngay, không cần hỏi thêm ai.

---

## 1.1 — Sơ đồ luồng dữ liệu

```
┌─────────────────────────────────────────────────────────────────┐
│  FE (người dùng chọn thuật toán "ga" + nhập Options)           │
│  POST /api/v1/schedule/run?algorithm=ga                         │
│  Body: { fromDate, toDate, employeeIds, shiftIds, options:{...} }│
└──────────────────────────┬──────────────────────────────────────┘
                           │ HTTP
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│  ScheduleController.cs   ← BẠN KHÔNG CẦN BIẾT FILE NÀY        │
│  ISolverFactory.Resolve("ga")                                   │
│    → GaSolver            ← FILE CỦA BẠN (nếu bạn phụ trách GA) │
│      .Run(input)                                                │
└──────────────────────────┬──────────────────────────────────────┘
                           │ trả về ScheduleResultDto
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│  FE render bảng phân công, biểu đồ So sánh thuật toán          │
│  (dùng ExecutionTimeMs, TotalPenaltyScore, HoursByEmployee...)  │
└─────────────────────────────────────────────────────────────────┘
```

**Kết luận quan trọng:** Bạn chỉ cần biết **input đưa vào** và **output cần trả ra**. Không cần hiểu Controller, không cần hiểu FE, không cần hiểu Database.

---

## 1.2 — Checklist: Việc duy nhất bạn cần làm

```
□ Mở đúng 1 file của mình trong:
    be/src/SchedulingApp.Application/Solver/
    ├── FakeGreedySolver.cs  → Thành viên A (Greedy)
    ├── GaSolver.cs          → Thành viên B (GA)
    ├── SaSolver.cs          → Thành viên C (SA)
    └── HybridSolver.cs      → Thành viên D (Hybrid)

□ Implement interface ISolver — xoá dòng throw NotImplementedException

□ Đọc tham số riêng qua input.Options (xem Mục 1.4)
  - Dùng TryGetValue + ép kiểu, KHÔNG throw nếu thiếu key → dùng giá trị mặc định

□ Trả về ScheduleResultDto đầy đủ TẤT CẢ field (xem Mục 1.3)
  - Đặc biệt: ExecutionTimeMs và TotalPenaltyScore KHÔNG được để = 0

□ Uncomment đúng 1 dòng trong Program.cs (xem Mục cuối)
  - File Program.cs đã có sẵn dòng bị comment, chỉ cần bỏ dấu //

□ Chạy dotnet test để xác nhận:
  - Test của mình PASS
  - Test của người khác KHÔNG bị ảnh hưởng (không fail)

□ KHÔNG sửa các file sau (trừ khi cả nhóm đồng ý):
  - ISolver.cs
  - ISolverFactory.cs
  - IConstraintValidator.cs
  - ScheduleController.cs
  - Program.cs (ngoài dòng AddScoped của mình)
  - File solver của người khác
```

---

## 1.3 — Hợp đồng dữ liệu (Input/Output Contract)

> ⚠️ Đây là phần quan trọng nhất. Không tuân thủ sẽ làm hỏng FE của người khác.

### INPUT — `SolverInput`

```csharp
public class SolverInput
{
    List<int> CompanyIds         // ID các công ty cần xếp lịch
    List<int> DepartmentIds      // ID các phòng ban
    List<int> PositionIds        // ID các vị trí
    List<int> EmployeeIds        // ID các nhân viên tham gia xếp lịch
    List<int> ShiftIds           // ID các ca cần phân công
    DateTime  FromDate           // Ngày bắt đầu kỳ xếp lịch
    DateTime  ToDate             // Ngày kết thúc kỳ xếp lịch
    Dictionary<string, object> Options  // Tham số riêng từng thuật toán (xem Mục 1.4)
}
```

**Ví dụ JSON thực tế** (dựa trên dữ liệu SeedData hiện có):

```json
{
  "companyIds": [1],
  "departmentIds": [1],
  "positionIds": [1],
  "employeeIds": [1],
  "shiftIds": [1],
  "fromDate": "2025-09-01T00:00:00",
  "toDate": "2025-09-07T23:59:59",
  "options": {
    "populationSize": 50,
    "generations": 200,
    "mutationRate": 0.05,
    "crossoverRate": 0.8
  }
}
```

> **Lưu ý:** `input.EmployeeIds` và `input.ShiftIds` là danh sách ID. Solver cần tự load dữ liệu đầy đủ (giờ làm, chứng chỉ...) từ DB thông qua `IRepository` (inject qua constructor) hoặc truyền thẳng Entity vào input nếu Leader chấp nhận mở rộng SolverInput.

### OUTPUT — `ScheduleResultDto` (BẮT BUỘC đầy đủ)

```csharp
public class ScheduleResultDto
{
    // Cấu trúc phân công ca
    Dictionary<string, List<AssignedEmployeeDto>> Schedule // key = shiftId

    // Tổng số ca cần phân công
    int TotalShifts

    // Số ca ĐÃ đủ người
    int FilledShifts

    // Số ca CHƯA đủ người (Thiếu người)
    int UnfilledShifts

    // Thời gian chạy (dùng Stopwatch)
    long ExecutionTimeMs
}
```

**Ví dụ output hợp lệ tối thiểu:**

```json
{
  "schedule": {
    "1": [
      { "employeeId": 1, "employeeName": "Nguyễn Văn A" }
    ]
  },
  "totalShifts": 1,
  "filledShifts": 1,
  "unfilledShifts": 0,
  "executionTimeMs": 142
}
```

**Giá trị của trường `status`:**

| Giá trị | Ý nghĩa |
|---------|---------|
| `"full"` | Ca đủ người theo yêu cầu |
| `"understaffed"` | Ca thiếu người (vẫn có người nhưng chưa đủ) |
| `"soft_violation"` | Ca đủ người nhưng có vi phạm mềm (VD: nhân viên vừa làm xong ca đêm) |

---

## 1.4 — Bảng tham số riêng từng thuật toán (`input.Options`)

| Thuật toán | Key trong Options | Kiểu dữ liệu | Giá trị mặc định | Ghi chú |
|------------|-------------------|--------------|------------------|---------|
| **GA** | `populationSize` | `int` | `50` | Số cá thể trong quần thể |
| **GA** | `generations` | `int` | `200` | Số thế hệ tối đa |
| **GA** | `mutationRate` | `double` | `0.05` | Xác suất đột biến (0.0–1.0) |
| **GA** | `crossoverRate` | `double` | `0.8` | Xác suất lai ghép (0.0–1.0) |
| **SA** | `initialTemperature` | `double` | `1000.0` | Nhiệt độ ban đầu |
| **SA** | `coolingRate` | `double` | `0.95` | Hệ số giảm nhiệt mỗi bước |
| **SA** | `maxIterations` | `int` | `1000` | Số lần lặp tối đa |
| **Hybrid** | Kế thừa GA + SA | — | Xem trên | Tự quyết thêm key riêng trong phạm vi file mình |

**Cách đọc Options an toàn (không crash khi FE không truyền key):**

```csharp
// Cách đúng — luôn có fallback
int popSize = input.Options.TryGetValue("populationSize", out var p) ? Convert.ToInt32(p) : 50;
double mutRate = input.Options.TryGetValue("mutationRate", out var m) ? Convert.ToDouble(m) : 0.05;

// Cách SAI — crash nếu FE không truyền key
int popSize = (int)input.Options["populationSize"]; // ❌ KeyNotFoundException!
```

---

## 1.5 — Ví dụ tối thiểu: Greedy đơn giản nhất có thể chạy được

Đoạn code dưới đây là GreedySolver **thật** đơn giản nhất (không tối ưu, nhưng đúng contract và có ít nhất 1 logic nghiệp vụ thật):

```csharp
using System.Diagnostics;
using SchedulingApp.Application.DTOs;
using SchedulingApp.Application.Interfaces;

public class GreedySolver : ISolver
{
    public string AlgorithmName => "greedy";

    public ScheduleResultDto Run(SolverInput input)
    {
        var sw = Stopwatch.StartNew(); // ← Bắt đầu đo thời gian

        var result = new ScheduleResultDto();
        var hoursByEmp = new Dictionary<string, double>();

        // Duyệt từng shiftId cần phân công
        foreach (var shiftId in input.ShiftIds)
        {
            // Logic thật (đơn giản): gán nhân viên đầu tiên còn trong giới hạn giờ tuần
            // (đây là logic nghiệp vụ THẬT, dù rất đơn giản — không phải stub)
            var assigned = new List<int>();
            foreach (var empId in input.EmployeeIds)
            {
                var empKey = empId.ToString();
                var currentHours = hoursByEmp.GetValueOrDefault(empKey, 0);
                if (currentHours < 40) // MaxHoursPerWeek = 40
                {
                    assigned.Add(empId);
                    hoursByEmp[empKey] = currentHours + 9; // ca 8h-17h = 9 giờ
                    break; // Greedy: chỉ gán 1 người/ca, gán xong là dừng
                }
            }

            result.Shifts.Add(new ScheduleShiftDto
            {
                ShiftId = shiftId,
                AssignedEmployeeIds = assigned,
                Status = assigned.Count > 0 ? "full" : "understaffed"
            });
        }

        sw.Stop(); // ← Kết thúc đo thời gian
        result.HoursByEmployee = hoursByEmp;
        result.ExecutionTimeMs = sw.ElapsedMilliseconds;
        result.TotalPenaltyScore = result.Shifts.Count(s => s.Status == "understaffed") * 100.0;

        return result;
    }
}
```

**Điểm mấu chốt phân biệt "stub" và "code thật đơn giản":**
- Stub: trả dữ liệu cố định, không đọc `input` gì cả
- Code thật tối thiểu: duyệt qua `input.ShiftIds`, `input.EmployeeIds`, có ít nhất 1 điều kiện nghiệp vụ (VD check giờ tuần), ghi đầy đủ `ExecutionTimeMs`

---

## 1.6 — Chạy thử độc lập, không cần cả hệ thống

Bạn **không cần** chạy API + Frontend + Database để test solver của mình. Dùng trực tiếp xUnit test:

### Bước 1 — Chuẩn bị dữ liệu test inline (không cần DB)

```csharp
// Trong file GaSolverTests.cs — xoá [Skip] và viết nội dung:
[Fact]
public void Run_WithValidInput_ReturnsNonEmptySchedule()
{
    // Arrange — tạo input trực tiếp, không cần DB
    var solver = new GaSolver();
    var input = new SolverInput
    {
        CompanyIds     = new List<int> { 1 },
        DepartmentIds  = new List<int> { 1 },
        PositionIds    = new List<int> { 1 },
        EmployeeIds    = new List<int> { 1, 2, 3 },
        ShiftIds       = new List<int> { 1, 2, 3, 4, 5 },
        FromDate       = new DateTime(2025, 9, 1),
        ToDate         = new DateTime(2025, 9, 7),
        Options        = new Dictionary<string, object>
        {
            ["populationSize"] = 10,    // nhỏ để test nhanh
            ["generations"]    = 5,
            ["mutationRate"]   = 0.05
        }
    };

    // Act
    var result = solver.Run(input);

    // Assert
    Assert.NotNull(result);
    Assert.NotEmpty(result.Shifts);
    Assert.True(result.ExecutionTimeMs >= 0);  // phải có giá trị
    Assert.True(result.TotalPenaltyScore >= 0);
}
```

### Bước 2 — Chạy test chỉ cho solver của mình

```bash
cd be

# Chạy toàn bộ test của bạn
dotnet test --filter "FullyQualifiedName~GaSolverTests"

# Hoặc chạy cụ thể 1 test
dotnet test --filter "FullyQualifiedName~GaSolverTests.Run_WithValidInput_ReturnsNonEmptySchedule"

# Chạy tất cả để đảm bảo không làm vỡ test người khác
dotnet test
```

### Bước 3 — Xem kết quả nhanh

```bash
# Nếu muốn thấy output chi tiết hơn
dotnet test --logger "console;verbosity=detailed"
```

> **Lợi ích:** Vòng phản hồi từ "sửa code" đến "biết kết quả" chỉ mất **3–10 giây** thay vì phải chạy cả API rồi bấm nút trên Swagger.

---

## 1.7 — Câu hỏi thường gặp (FAQ)

### "Tôi cần thêm 1 field mới vào SolverInput, làm sao?"
Không tự thêm. Báo **Leader (Thành viên phụ trách Greedy)** để thêm 1 lần cho cả 4 người dùng chung. Lý do: SolverInput là DTO dùng chung — nếu 2 người thêm cùng lúc sẽ gây merge conflict và làm hỏng test người khác.

Trong thời gian chờ, dùng `input.Options["myCustomKey"]` để truyền tạm tham số riêng của mình.

### "Solver Hybrid của tôi cần dùng GaSolver để khởi tạo population ban đầu?"
Được phép. Inject qua constructor:

```csharp
public class HybridSolver : ISolver
{
    private readonly ISolverFactory _factory;

    public HybridSolver(ISolverFactory factory) // DI inject tự động
    {
        _factory = factory;
    }

    public ScheduleResultDto Run(SolverInput input)
    {
        var gaSolver = _factory.Resolve("ga");
        var initialResult = gaSolver.Run(input); // dùng GA để khởi tạo
        // ... tiếp tục với Local Search ...
    }
}
```

### "Làm sao biết code của mình đúng mà không cần hỏi cả nhóm?"
Chạy bộ Unit Test đã có sẵn (Mục 1.6). Điều kiện tối thiểu để merge:
1. ✅ Tất cả test trong file `XxxSolverTests.cs` của mình đều PASS (không Skip)
2. ✅ `dotnet test` toàn bộ solution không có test nào FAIL (chỉ được Skipped)
3. ✅ `ExecutionTimeMs > 0` và `TotalPenaltyScore >= 0` trong output
4. ✅ `dotnet build` không có Error (Warning tạm chấp nhận)

### "Tôi có được sửa IConstraintValidator không?"
**Chỉ được sửa nếu cả nhóm đồng ý.** Lý do: tất cả 4 solver đều dùng chung `IConstraintValidator` — sửa sai 1 chỗ sẽ làm hỏng validation của tất cả. Quy trình: tạo issue/thảo luận trên nhóm chat, Leader duyệt rồi 1 người sửa, chạy lại toàn bộ test.

### "Tôi có được sửa ScheduleResultDto không?"
Tương tự — phải trao đổi cả nhóm trước. ScheduleResultDto là contract giữa BE và FE; thêm/xóa field phải đồng bộ cả hai phía.

### "Uncomment dòng nào trong Program.cs?"
Tìm đoạn sau trong `be/src/SchedulingApp.Api/Program.cs`:
```csharp
builder.Services.AddScoped<ISolver, FakeGreedySolver>();
// builder.Services.AddScoped<ISolver, GaSolver>();      // ← uncomment dòng này nếu bạn là TV GA
// builder.Services.AddScoped<ISolver, SaSolver>();      // ← uncomment nếu bạn là TV SA
// builder.Services.AddScoped<ISolver, HybridSolver>();  // ← uncomment nếu bạn là TV Hybrid
```
Bỏ `//` ở dòng của mình. Không sửa gì khác trong file này.

---

## Tóm tắt nhanh (TL;DR)

```
1. Mở file solver của mình trong Application/Solver/
2. Xoá throw NotImplementedException, viết logic thật
3. Đọc Options an toàn: TryGetValue + Convert.ToInt32/ToDouble
4. Trả về ScheduleResultDto với ĐẦY ĐỦ field (đặc biệt ExecutionTimeMs, TotalPenaltyScore)
5. Uncomment 1 dòng trong Program.cs
6. Chạy dotnet test --filter "XxxSolverTests" → phải PASS
## 1.8 — Quy tắc giải quyết Ưu tiên (Priority Framework)

> **QUAN TRỌNG:** 4 thuật toán phải dùng **CHUNG 1 tiêu chí ưu tiên** dưới đây để đảm bảo tính công bằng khi so sánh ở Báo cáo tốt nghiệp (Chương 5).

### A. Ưu tiên khi CHỌN NHÂN VIÊN cho 1 CA (nhiều người tranh 1 ca)
Áp dụng sau khi ứng viên đã thoạt mãn mọi ràng buộc cứng (HC1–HC8). Tính điểm ưu tiên theo thứ tự (chỉ dùng tiêu chí sau để phá hoà tiêu chí trước):
1. Khớp vị trí chính (`IsPrimary = true`) **>** Vị trí phụ.
2. Cùng phòng ban với ca (`assignment.DepartmentId == shift.DepartmentId`) **>** Đi chéo (`AllowCrossDept = true`).
3. Tổng giờ đã xếp trong tuần hiện tại **THẤP HƠN** (cân bằng tải).
4. Khớp nguyện vọng đăng ký rảnh (`EmployeePreference`).
5. Thâm niên (`SeniorityYears`) cao hơn.

Trong log `CandidateScoreLog`, bạn phải ghi nhận đủ điểm/thuộc tính của **TẤT CẢ** ứng viên được xét duyệt để giải trình lý do chọn.

### B. Ưu tiên khi NHÂN VIÊN rảnh cho NHIỀU CA cùng lúc (1 người tranh nhiều ca)
Đây không phải là lỗi trùng lịch, mà là bước chọn 1 ca tốt nhất cho nhân viên.
Sử dụng trường `PriorityOrder` trong `EmployeeAssignment` (do HR cài đặt, 1 = cao nhất):
1. Chọn ca thuộc Assignment có `PriorityOrder` nhỏ hơn.
2. Nếu hoà (hoặc null): áp dụng quy tắc A (nhưng đổi vai trò thành so sánh giữa 2 ca).
3. Nếu vẫn hoà: ưu tiên ca thuộc `IsPrimary = true`.
4. Nếu vẫn hoà: ưu tiên ca đang **THIẾU NGƯỜI NHIỀU HƠN** (cứu ca khó trước).

Trong log `ConflictResolutionLog`, ghi rõ lý do đã chọn ca nào.

---

## 1.9 — Vị trí theo từng công ty & Đa phòng ban

1. **CompanyPosition (HC8):** Công ty có thể không dùng toàn bộ danh sách `Position`. Ca làm chỉ hợp lệ nếu cặp `(CompanyId, PositionId)` có cấu hình `IsActive = true` trong bảng `CompanyPositions`.
2. **Đa phòng ban cùng công ty:** 1 nhân viên có thể có nhiều `EmployeeAssignment` ở cùng 1 công ty nhưng khác `DepartmentId` (VD: vừa làm Kho vừa làm Thu ngân). Tuy nhiên, chỉ được phép có **DUY NHẤT 1** dòng `IsPrimary = true` trong 1 công ty.
3. **Đa công ty:** 1 nhân viên có thể làm cho 3+ công ty. Tổng số giờ giới hạn `MaxHoursPerWeek` luôn được tính **GỘP chung** cho toàn bộ nhân viên (thuộc bảng `Employee`), không tách riêng theo công ty.

---

## 1.10 — Validation ở tầng CRUD (Dành cho FE & Controller)

Các quy tắc sau được validate ngay khi user thao tác, không đợi chạy Solver:
- **Thêm/Sửa Assignment:** Chặn lưu nếu tạo ra ≥ 2 assignment có `IsPrimary = true` trong cùng 1 công ty.
- **Thêm/Sửa Ca làm:** CẢNH BÁO MỀM (hiện Dialog) nếu ca trùng giờ với ca khác cùng phòng ban/vị trí. Không chặn cứng.
- **Thêm Nghỉ phép:** Nếu nhân viên đã được phân công ca trong ngày xin nghỉ → CẢNH BÁO "Nhân viên đang có N ca xếp lịch, thêm nghỉ phép sẽ cần chạy lại xếp lịch".
- **Sửa Assignment/Chứng chỉ:** Nếu làm mất hiệu lực ca đã xếp → CẢNH BÁO "Thay đổi làm N ca đã xếp không còn hợp lệ, cần xếp lại lịch".

---

## 1.11 — Nguyên tắc quan trọng: Log là việc CỦA BẠN, không phải của FE
Khi code thuật toán, bạn TỰ QUYẾT ghi log gì (dùng ILogger) để debug và để giải trình khi bảo vệ đồ án. Scaffold KHÔNG dựng sẵn UI hiển thị log chi tiết hay bảng so sánh thuật toán — vì số liệu đó chỉ có Ý NGHĨA sau khi bạn code xong thuật toán thật và tự chạy thực nghiệm (xem File 3 — Bảng phân công, mục 3.1/3.2). Muốn xem log khi debug: mở file be/logs/scheduling-{ngày}.log hoặc xem trực tiếp Console khi chạy `dotnet run`.

---

## Cách xem so sánh đầy đủ cả 4 thuật toán

Dashboard chỉ hiển thị so sánh đầy đủ khi TRÊN CÙNG 1 MÁY đã chạy đủ cả 4 thuật toán. Trong lúc code, bạn chỉ thấy cột của mình — điều đó BÌNH THƯỜNG, không cần lo. Số liệu so sánh chính thức cho báo cáo sẽ được chạy 1 lần duy nhất SAU KHI merge code (Giai đoạn 3, task 3.1), dùng nút 'Chạy tất cả thuật toán' hoặc endpoint /schedule/run-all để không phải bấm tay từng cái.

