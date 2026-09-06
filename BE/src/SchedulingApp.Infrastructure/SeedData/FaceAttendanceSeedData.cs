using SchedulingApp.Domain.Entities.FaceAttendance;
using SchedulingApp.Infrastructure.Persistence;

namespace SchedulingApp.Infrastructure.SeedData;

public static class FaceAttendanceSeedData
{
    public static void Initialize(FaceAttendanceDbContext context)
    {
        if (context.Companies.Any())
            return;

        var companyId = Guid.NewGuid();

        context.Companies.Add(new FaceCompany
        {
            Id = companyId,
            Code = "DEMO",
            Name = "Công ty mẫu",
            ShortName = "Demo",
            IsActive = true
        });

        context.Departments.Add(new FaceDepartment
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = "OPS",
            Name = "Phòng vận hành",
            IsActive = true
        });

        context.JobTitles.AddRange(
            new FaceJobTitle
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Code = "STAFF",
                Name = "Nhân viên",
                IsActive = true
            },
            new FaceJobTitle
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Code = "LEAD",
                Name = "Trưởng ca",
                IsActive = true
            });

        context.CompanyShifts.AddRange(
            new FaceCompanyShift
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = "Ca ngày",
                ShiftStart = new TimeSpan(8, 0, 0),
                ShiftEnd = new TimeSpan(17, 0, 0),
                LunchStart = new TimeSpan(12, 0, 0),
                LunchEnd = new TimeSpan(13, 0, 0),
                IsDefault = true,
                IsActive = true,
                IsOvernight = false,
                MaxOvertimeBufferMinutes = 60
            },
            new FaceCompanyShift
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = "Ca đêm",
                ShiftStart = new TimeSpan(22, 0, 0),
                ShiftEnd = new TimeSpan(6, 0, 0),
                IsDefault = false,
                IsActive = true,
                IsOvernight = true,
                BreakIsOvernight = true,
                MaxOvertimeBufferMinutes = 60
            });

        context.SaveChanges();
    }
}
