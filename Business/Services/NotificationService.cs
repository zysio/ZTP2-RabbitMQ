using Business.Mappers;
using Business.Models;
using Data.Models;
using Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(NotificationDTO dto);
        Task<NotificationDTO> GetNotificationAsync(int id);
        Task UpdateNotificationAsync(int id, NotificationDTO dto);
        Task<IEnumerable<NotificationDTO>> GetAllNotificationsAsync();
        Task DeleteNotificationAsync(int id);
    }

    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repository;

        public NotificationService(INotificationRepository repository)
        {
            _repository = repository;
        }

        public async Task CreateNotificationAsync(NotificationDTO dto)
        {
            ValidateNotificationDTO(dto);

            var notification = NotificationMapper.MapToEntity(dto);
            await _repository.AddNotificationAsync(notification);
        }

        public async Task<NotificationDTO> GetNotificationAsync(int id)
        {
            var notification = await _repository.GetNotificationAsync(id)
                              ?? throw new KeyNotFoundException($"Notification with id {id} not found.");
            return NotificationMapper.MapToDTO(notification);
        }

        public async Task UpdateNotificationAsync(int id, NotificationDTO dto)
        {
            ValidateNotificationDTO(dto);

            var notification = NotificationMapper.MapToEntity(dto, id);
            await _repository.UpdateNotificationAsync(notification);
        }

        public async Task<IEnumerable<NotificationDTO>> GetAllNotificationsAsync()
        {
            var notifications = await _repository.GetNotificationsAsync();
            return notifications.Select(NotificationMapper.MapToDTO);
        }

        public async Task DeleteNotificationAsync(int id)
        {
            await _repository.DeleteNotificationAsync(id);
        }

        private void ValidateNotificationDTO(NotificationDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Message))
                throw new ArgumentException("Message cannot be empty.");

            if (string.IsNullOrWhiteSpace(dto.Recipient))
                throw new ArgumentException("Recipient cannot be empty.");

            if (string.IsNullOrWhiteSpace(dto.Channel))
                throw new ArgumentException("Channel cannot be empty.");

            if (string.IsNullOrWhiteSpace(dto.Timezone))
                throw new ArgumentException("Timezone cannot be empty.");
        }
    }

}
