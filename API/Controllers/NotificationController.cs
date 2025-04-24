using Business.Models;
using Business.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NotificationDTO>> Get(int id)
        {
            var notification = await _notificationService.GetNotificationAsync(id);
            return Ok(notification);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationDTO>>> GetAll()
        {
            var notifications = await _notificationService.GetAllNotificationsAsync();
            return Ok(notifications);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] NotificationDTO dto)
        {
            await _notificationService.CreateNotificationAsync(dto);
            return CreatedAtAction(nameof(Get), new { id = 0 }, null);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] NotificationDTO dto)
        {
            await _notificationService.UpdateNotificationAsync(id, dto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _notificationService.DeleteNotificationAsync(id);
            return NoContent();
        }

        [HttpPost("process-pending")]
        public async Task<IActionResult> ProcessPendingNotifications()
        {
            await _notificationService.ProcessPendingNotificationsAsync();
            return Ok(new { Message = "Pending notifications processed successfully." });
        }
    }
}
