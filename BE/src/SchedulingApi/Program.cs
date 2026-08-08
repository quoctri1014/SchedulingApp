using System.Text.Json.Serialization;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
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

// DbContext — SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<FaceAttendanceDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("FaceAttendanceConnection")));

// AutoMapper — đăng ký MappingProfile
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

builder.Services.AddScoped<ISolver, GreedySolver>();      // Greedy
builder.Services.AddScoped<ISolver, GaSolver>();          // GA
builder.Services.AddScoped<ISolver, SaSolver>();          // SA
builder.Services.AddScoped<ISolver, HybridSolver>();      // Hybrid
builder.Services.AddScoped<ISolverFactory, SolverFactory>();

// IConstraintValidator — Fake luôn trả về valid, nhóm thay bằng ConstraintValidator thật sau
builder.Services.AddScoped<IConstraintValidator, FakeValidator>();

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Exception middleware (phải đặt trước CORS và routing)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Seed Data — tự động migrate và chèn dữ liệu mẫu khi khởi động
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();         // Tự động chạy migrations nếu chưa apply
    
    var faceContext = scope.ServiceProvider.GetRequiredService<FaceAttendanceDbContext>();
    faceContext.Database.Migrate();

    SeedData.Initialize(context);       // Chèn dữ liệu mẫu nếu DB chưa có dữ liệu
}

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
