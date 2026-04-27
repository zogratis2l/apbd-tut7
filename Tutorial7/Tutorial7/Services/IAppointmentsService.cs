using Tutorial7.DTOs;

namespace Tutorial7.Services;

public interface IAppointmentsService
{
    Task<IEnumerable<AppointmentListDto>> GetAllAppointmentsAsync(string? status, string? patientLastName);
    
    Task<AppointmentDetailsDto?> GetAppointmentDetailsAsync(int idAppointment);

    Task<int> CreateAppointmentAsync(CreateAppointmentRequestDto dto);
    
    Task UpdateAppointmentAsync(int idAppointment, UpdateAppointmentRequestDto dto);
    
    Task DeleteAppointmentAsync(int idAppointment);
    
}