using Business.Models;
using Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Mappers
{
    public static class NotificationMapper
    {
        public static Notification MapToEntity(NotificationDTO notificationDTO, int id = 0)
        {
            return new Notification
            {
                Id = id,
                Message = notificationDTO.Message,
                Channel = notificationDTO.Channel,
                Recipient = notificationDTO.Recipient,
                Timezone = notificationDTO.Timezone,
                Priority = notificationDTO.Priority,
                Scheduled = notificationDTO.Scheduled.HasValue
                    ? DateTime.SpecifyKind(notificationDTO.Scheduled.Value, DateTimeKind.Utc)
                    : null,
                Status = notificationDTO.Status
            };
        }

        public static NotificationDTO MapToDTO(Notification notification)
        {
            return new NotificationDTO
            (
                notification.Message,
                notification.Channel,
                notification.Recipient,
                notification.Timezone,
                notification.Priority,
                notification.Scheduled,
                notification.Status
            );
        }
    }
}
