namespace Tutorial7.DTOs;

public class AppointmentDetailsDto
{
    public string patientEmail { get; set; }
    public string patientNumber { get; set; }
    public string doctorLicenseNumber  { get; set; }
    public string? internalNotes { get; set; }
    public DateTime recordCreationDate { get; set; }
    
}