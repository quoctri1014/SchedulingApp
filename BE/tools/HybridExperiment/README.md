# Thực nghiệm Hybrid

Chương trình chạy Ablation Study cho Hybrid trên ba bộ dữ liệu tổng hợp Small Medium và Large. Mỗi cấu hình chạy 30 lần khi tắt Local Search và 30 lần khi bật Local Search.

Chạy từ thư mục `BE`:

```powershell
dotnet run --project .\tools\HybridExperiment\HybridExperiment.csproj -- ..\outputs\hybrid-ga-report\hybrid_experiment_results.json
```

Các tham số thực nghiệm nằm trong `Program.cs`. Trường `randomSeed` giúp lặp lại đúng từng lần chạy để so sánh hai cấu hình công bằng.
