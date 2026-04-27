using System.Runtime.InteropServices.JavaScript;
using Microsoft.Data.SqlClient;
using Tutorial7.DTOs;

namespace Tutorial7.Services;

public class AppointmentsService : IAppointmentsService
{
    private readonly string _connectionString;

    public AppointmentsService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }
    
    public async Task<IEnumerable<AppointmentListDto>> GetAllAppointmentsAsync(string? status, string? patientLastName)
    {
        var appointments = new List<AppointmentListDto>();
        
        await using var connection = new SqlConnection(_connectionString);

        var command = new SqlCommand("SELECT a.IdAppointment, a.AppointmentDate, a.Status, a.Reason, p.FirstName + N' ' + p.LastName AS PatientFullName, p.Email AS PatientEmail FROM dbo.Appointments a JOIN dbo.Patients p ON p.IdPatient = a.IdPatient WHERE (@Status IS NULL OR a.Status = @Status)  AND (@PatientLastName IS NULL OR p.LastName = @PatientLastName) ORDER BY a.AppointmentDate;", connection);

        command.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);
        command.Parameters.AddWithValue("@PatientLastName", (object?)patientLastName ?? DBNull.Value);
        
        await connection.OpenAsync();
        
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            appointments.Add(new AppointmentListDto()
            {
                IdAppointment = reader.GetInt32(0),
                AppointmentDate = reader.GetDateTime(1),
                Status = reader.GetString(2),
                Reason = reader.GetString(3),
                PatientFullName = reader.GetString(4),
                PatientEmail = reader.GetString(5),
            });
            
        }
        
        return appointments;
    }

    public async Task<AppointmentDetailsDto?> GetAppointmentDetailsAsync(int idAppointment)
    {
        await using var connection = new SqlConnection( _connectionString );
        
        var command = new SqlCommand("Select p.Email, p.PhoneNumber, d.LicenseNumber, a.InternalNotes, a.CreatedAt FROM dbo.Patients p JOIN dbo.Appointments a ON p.IdPatient = a.IdPatient JOIN dbo.Doctors d on d.IdDoctor = a.IdDoctor WHERE a.IdAppointment = @idAppointment", connection);
        
        command.Parameters.AddWithValue("@idAppointment", idAppointment);
        
        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new AppointmentDetailsDto()
            {
                patientEmail = reader.GetString(0),
                patientNumber = reader.GetString(1),
                doctorLicenseNumber = reader.GetString(2),
                internalNotes = reader.IsDBNull((3)) ? null : reader.GetString(3),
                recordCreationDate = reader.GetDateTime(4),
            };
        }

        return null;
    }

    public async Task<int> CreateAppointmentAsync(CreateAppointmentRequestDto dto)
    {
        await using var connection = new SqlConnection(_connectionString);

        if (string.IsNullOrWhiteSpace(dto.reason) || dto.reason.Length > 250)
            throw new ArgumentException("Invalid reason");
        
        if (dto.appointmentDate < DateTime.Now)
        {
            throw new ArgumentException("Date cannot be in the past");
        }
        
        await connection.OpenAsync();

        var patientCmd =
            new SqlCommand("Select IsActive FROM dbo.Patients WHERE IdPatient = @id", connection);
        
        patientCmd.Parameters.AddWithValue("@id", dto.idPatient);

        var patientExis = await patientCmd.ExecuteScalarAsync();
        if (patientExis == null)
        {
            throw new KeyNotFoundException("Patient not found");
        }
        if ((bool)patientExis == false)
            throw new ArgumentException("Patient is inactive");
        
        var doctorCmd = new SqlCommand("SELECT IsActive FROM dbo.Doctors WHERE IdDoctor = @id", connection);

        doctorCmd.Parameters.AddWithValue("@id", dto.idDoctor);
        var doctorExis = await doctorCmd.ExecuteScalarAsync();
        if (doctorExis == null)
            throw new KeyNotFoundException("Doctor not found");
        if ((bool)doctorExis == false)
            throw new ArgumentException("Doctor is inactive");

        
        var conflictCmd = new SqlCommand("Select 1 FROM dbo.Appointments WHERE IdDoctor = @id AND AppointmentDate = @date AND Status = 'Scheduled'", connection);
        conflictCmd.Parameters.AddWithValue("@id", dto.idDoctor);
        conflictCmd.Parameters.AddWithValue("@date" , dto.appointmentDate);
        
        var conflictExis = await conflictCmd.ExecuteScalarAsync();

        if (conflictExis != null)
        {
            throw new InvalidOperationException("Appointment conflict");
        }

        var insertCmd =
            new SqlCommand(
                "INSERT INTO dbo.Appointments (IdPatient, IdDoctor, AppointmentDate, Reason, Status) VALUES (@p, @d, @date, @reason, 'Scheduled')" +
                "SELECT SCOPE_IDENTITY();", connection);
        
        
        insertCmd.Parameters.AddWithValue("@p", dto.idPatient);
        insertCmd.Parameters.AddWithValue("@d", dto.idDoctor);
        insertCmd.Parameters.AddWithValue("@date", dto.appointmentDate);
        insertCmd.Parameters.AddWithValue("@reason", dto.reason);

        object? newId = await insertCmd.ExecuteScalarAsync();
        
        int insertedId = Convert.ToInt32(newId);
        
        return insertedId;
    }

    public async Task UpdateAppointmentAsync(int idAppointment, UpdateAppointmentRequestDto dto)
    {
        await using var connection = new SqlConnection(_connectionString);

        await connection.OpenAsync();
        
        var appointmentCmd = new SqlCommand("SELECT Status, AppointmentDate FROM dbo.Appointments WHERE IdAppointment = @id", connection);
        
        appointmentCmd.Parameters.AddWithValue("@id", idAppointment);
        await using var reader = await appointmentCmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            throw new KeyNotFoundException("Appointment not found");
        
        var currentStatus = reader.GetString(0);
        var currentDate = reader.GetDateTime(1);
        
        var allowedStatuses = new[] { "Scheduled", "Completed", "Cancelled" };
        if (!allowedStatuses.Contains(dto.status))
            throw new ArgumentException("Invalid status");
        
        if (currentStatus == "Completed" && dto.appointmentDate != currentDate)
            throw new InvalidOperationException("Cannot change date of completed appointment");
        
        var patientCmd = new SqlCommand("SELECT IsActive FROM dbo.Patients WHERE IdPatient = @id", connection);
        patientCmd.Parameters.AddWithValue("@id", dto.idPatient);
        
        var patientExis = await patientCmd.ExecuteScalarAsync();

        if (patientExis == null)
        {
            throw new KeyNotFoundException("Patient not found");
        }

        if ((bool)patientExis == false)
        {
            throw new ArgumentException("Patient is inactive");
        }

        var doctorCmd = new SqlCommand("SELECT IsActive FROM dbo.Doctors WHERE IdDoctor = @id", connection);

        doctorCmd.Parameters.AddWithValue("@id", dto.idDoctor);

        var doctorExis = await doctorCmd.ExecuteScalarAsync();

        if (doctorExis == null)
        {
            throw new KeyNotFoundException("Doctor not found");
        }

        if ((bool)doctorExis == false)
        {
            throw new ArgumentException("Doctor is inactive");
        }
        
        if (dto.appointmentDate != currentDate)
        {
            var conflictCmd = new SqlCommand("SELECT 1 FROM dbo.Appointments WHERE IdDoctor = @doctorId AND AppointmentDate = @date AND Status = 'Scheduled' AND IdAppointment != @id", connection);

            conflictCmd.Parameters.AddWithValue("@doctorId", dto.idDoctor);
            conflictCmd.Parameters.AddWithValue("@date", dto.appointmentDate);
            conflictCmd.Parameters.AddWithValue("@id", idAppointment);

            var conflict = await conflictCmd.ExecuteScalarAsync();

            if (conflict != null)
                throw new InvalidOperationException("Appointment time conflict");
        }
        
        
        var updateCmd = new SqlCommand("UPDATE dbo.Appointments SET IdPatient = @p, IdDoctor = @d, AppointmentDate = @date, Status = @status, Reason = @reason, InternalNotes = @notes WHERE IdAppointment = @id", connection);

        updateCmd.Parameters.AddWithValue("@p", dto.idPatient);
        updateCmd.Parameters.AddWithValue("@d", dto.idDoctor);
        updateCmd.Parameters.AddWithValue("@date", dto.appointmentDate);
        updateCmd.Parameters.AddWithValue("@status", dto.status);
        updateCmd.Parameters.AddWithValue("@reason", dto.reason);
        updateCmd.Parameters.AddWithValue("@notes", (object?)dto.internalNotes ?? DBNull.Value);
        updateCmd.Parameters.AddWithValue("@id", idAppointment);

        await updateCmd.ExecuteNonQueryAsync();
        
    }

    public async Task DeleteAppointmentAsync(int idAppointment)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        var checkCmd = new SqlCommand("SELECT Status FROM dbo.Appointments WHERE IdAppointment = @id", connection);

        checkCmd.Parameters.AddWithValue("@id", idAppointment);
        
        var exists = await checkCmd.ExecuteScalarAsync();

        if (exists == null)
        {
            throw new KeyNotFoundException("Appointment not found");
        }

        var status = Convert.ToString(exists);

        if (status == "Completed")
        {
            throw new InvalidOperationException("Appointment is completed");
        }
        
        var deleteCmd = new SqlCommand("DELETE FROM dbo.Appointments WHERE IdAppointment = @id", connection);
        deleteCmd.Parameters.AddWithValue("@id", idAppointment);
        
        await deleteCmd.ExecuteNonQueryAsync();
    }
    
}