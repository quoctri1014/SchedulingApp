using System.Text.Json.Serialization;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SchedulingApp.Api.Middleware;
using SchedulingApp.Application.Interfaces;
using SchedulingApp.Application.Mappings;
using SchedulingApp.Application.Solver;
using SchedulingApp.Infrastructure.Persistence;
using SchedulingApp.Infrastructure.SeedData;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Model.Validation", LogEventLevel.Error)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/scheduling-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Controllers + JSON options (ReferenceHandler.IgnoreCycles as safety net)
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS — cho phép FE gọi API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy.WithOrigins("http://localhost:5173")
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

// DbContext — SQLite (for local dev) or SQL Server (production)
var useDbType = builder.Configuration.GetValue<string>("UseDatabase") ?? "SQLite";
var sqliteDirectory = AppContext.BaseDirectory;
var sqliteAppConnection = $"Data Source={Path.Combine(sqliteDirectory, "SchedulingApp.db")}";
var sqliteFaceConnection = $"Data Source={Path.Combine(sqliteDirectory, "FaceAttendance.db")}";
if (useDbType == "SqlServer")
{
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
        options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    });
    builder.Services.AddDbContext<FaceAttendanceDbContext>(options =>
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("FaceAttendanceConnection"));
        options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    });
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseSqlite(sqliteAppConnection);
        options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    });
    builder.Services.AddDbContext<FaceAttendanceDbContext>(options =>
    {
        options.UseSqlite(sqliteFaceConnection);
        options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    });
}

// AutoMapper — đăng ký MappingProfile
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

builder.Services.AddScoped<ISolver, GreedySolver>();      // Greedy
builder.Services.AddScoped<ISolver, GaSolver>();          // GA
builder.Services.AddScoped<ISolver, SaSolver>();          // SA
builder.Services.AddScoped<ISolver, HybridSolver>();      // Hybrid
builder.Services.AddScoped<ISolverFactory, SolverFactory>();

builder.Services.AddScoped<IConstraintValidator, ConstraintValidator>();

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Exception middleware (phải đặt trước CORS và routing)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Seed Data — tự động tạo DB nếu chưa có
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();   // Tạo DB từ model nếu chưa có (bỏ qua migrations)
        SeedData.Initialize(context);       // Chèn dữ liệu mẫu nếu DB chưa có dữ liệu
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning($"AppDbContext initialization failed: {ex.Message}");
        // Continue startup even if DB init fails - for development/testing
    }
}

// Initialize FaceAttendanceDbContext nếu cần
using (var scope = app.Services.CreateScope())
{
    try
    {
        var faceContext = scope.ServiceProvider.GetRequiredService<FaceAttendanceDbContext>();
        faceContext.Database.EnsureCreated();
        FaceAttendanceSeedData.Initialize(faceContext);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning($"FaceAttendanceDbContext initialization failed: {ex.Message}");
    }
}

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
