using AutoMapper;
using SchedulingApp.Application.DTOs;
using SchedulingApp.Domain.Entities;

namespace SchedulingApp.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Employee, EmployeeDto>();
        CreateMap<EmployeeAssignment, EmployeeAssignmentDto>();
        CreateMap<EmployeeLeave, EmployeeLeaveDto>();
        CreateMap<EmployeePreference, EmployeePreferenceDto>();
        CreateMap<Shift, ShiftDto>();
    }
}
