using Microsoft.EntityFrameworkCore;
using SchedulingApp.Domain.Entities;
using SchedulingApp.Infrastructure.Persistence;

namespace SchedulingApp.Infrastructure.SeedData;

public static class SeedData
{
    public static void Initialize(AppDbContext context)
    {
        // No longer seeding mock data since we rely on FaceAttendanceSystem DB.
    }
}
