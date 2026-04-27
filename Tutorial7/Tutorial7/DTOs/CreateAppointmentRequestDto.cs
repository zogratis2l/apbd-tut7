namespace Tutorial7.DTOs;

public class CreateAppointmentRequestDto
{
    public int idPatient { get; set; }
    public int idDoctor { get; set; }
    public DateTime appointmentDate { get; set; }
    public string reason { get; set; }
}