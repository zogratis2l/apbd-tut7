using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tutorial7.DTOs;
using Tutorial7.Services;

namespace Tutorial7.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentsService _appointmentsService;

        public AppointmentsController(IAppointmentsService appointmentsService)
        {
            _appointmentsService = appointmentsService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string? status, string? patientLastName)
        {
            var appointments = await _appointmentsService.GetAllAppointmentsAsync(status, patientLastName);
            return Ok(appointments);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var appointment = await _appointmentsService.GetAppointmentDetailsAsync(id);

            if (appointment == null)
            {
                return NotFound();
            }

            return Ok(appointment);
        }


        [HttpPost]
        public async Task<IActionResult> CreateAppointment(CreateAppointmentRequestDto dto)
        {
            try
            {
                var id = await _appointmentsService.CreateAppointmentAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id }, new {id});
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ErrorResponseDto
                {
                    message = ex.Message,
                    StatusCode = 404,
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ErrorResponseDto
                {
                    message = ex.Message,
                    StatusCode = 400,
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ErrorResponseDto
                {
                    message = ex.Message,
                    StatusCode = 409,
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAppointment(int id, UpdateAppointmentRequestDto dto)
        {
            try
            {
                await _appointmentsService.UpdateAppointmentAsync(id, dto);
                return NoContent();
            } catch (KeyNotFoundException ex)
            {
                return NotFound(new ErrorResponseDto
                {
                    message = ex.Message,
                    StatusCode = 404,
                }); }
            catch (ArgumentException ex)
            {
                return BadRequest(new ErrorResponseDto
                {
                    message = ex.Message,
                    StatusCode = 400,
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ErrorResponseDto
                {
                    message = ex.Message,
                    StatusCode = 409,
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            try
            {
                await _appointmentsService.DeleteAppointmentAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ErrorResponseDto
                {
                    message = ex.Message,
                    StatusCode = 404,
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ErrorResponseDto
                {
                    message = ex.Message,
                    StatusCode = 409,
                });
            }
        }
    }
}
