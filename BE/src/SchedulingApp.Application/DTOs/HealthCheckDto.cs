namespace SchedulingApp.Application.DTOs;

public class HealthCheckDto
{
    public string Status { get; set; } = "Healthy";
    public bool DatabaseConnected { get; set; } = true;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Version { get; set; } = "1.0.0";
}
