using Microsoft.EntityFrameworkCore;
using SchedulingApp.Domain.Entities;
using SchedulingApp.Infrastructure.Persistence;

namespace SchedulingApp.Infrastructure.SeedData;

public static class SeedData
{
    public static void Initialize(AppDbContext context)
    {
        var companyId = "demo-company";
        var departmentId = "demo-department";
        var positionId = "demo-position";

        var existingEmployeeIds = context.Employees
            .Select(employee => employee.Id)
            .ToHashSet();

        var employees = Enumerable.Range(1, 200)
            .Where(index => !existingEmployeeIds.Contains($"demo-employee-{index:00}"))
            .Select(index => new Employee
            {
                Id = $"demo-employee-{index:00}",
                FullName = $"Nhân viên mẫu {index:00}",
                Email = $"employee{index:00}@example.local",
                MaxHoursPerWeek = 40
            })
            .ToList();

        var existingAssignmentEmployeeIds = context.EmployeeAssignments
            .Where(assignment => assignment.CompanyId == companyId && assignment.DepartmentId == departmentId)
            .Select(assignment => assignment.EmployeeId)
            .ToHashSet();

        var assignments = employees
            .Where(employee => !existingAssignmentEmployeeIds.Contains(employee.Id))
            .Select(employee => new EmployeeAssignment
        {
            EmployeeId = employee.Id,
            CompanyId = companyId,
            DepartmentId = departmentId,
            PositionId = positionId,
            IsPrimary = true,
            CertificateExpiryDate = DateTime.Today.AddYears(1),
            EfficiencyMultiplier = 1.0f
        }).ToList();

        var existingShiftCount = context.Shifts.Count();
        var firstDay = DateTime.Today.AddDays(1).AddHours(8);
        var shifts = Enumerable.Range(existingShiftCount, Math.Max(0, 200 - existingShiftCount))
            .Select(index => new Shift
            {
                CompanyId = companyId,
                DepartmentId = departmentId,
                PositionId = positionId,
                StartDate = firstDay.AddDays(index / 2).AddHours((index % 2) * 8),
                EndDate = firstDay.AddDays(index / 2).AddHours((index % 2) * 8 + 8),
                RequiredEmployeeCount = 2
            })
            .ToList();

        context.Employees.AddRange(employees);
        context.EmployeeAssignments.AddRange(assignments);
        context.Shifts.AddRange(shifts);
        context.SaveChanges();
    }
}
