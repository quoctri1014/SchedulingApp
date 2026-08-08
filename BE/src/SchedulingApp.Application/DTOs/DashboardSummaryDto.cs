namespace SchedulingApp.Application.DTOs;

public class DashboardSummaryDto
{
    public int TotalCompanies { get; set; }
    public int TotalDepartments { get; set; }
    public int TotalEmployees { get; set; }
    public int TotalShifts { get; set; }
    public int TotalFaceUsers { get; set; }
    public int TotalAlgorithmRuns { get; set; }
    public double LatestAccuracyPercentage { get; set; }
}
