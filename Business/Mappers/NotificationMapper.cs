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
                Timezone = notificationDTO.Timezone
            };
        }

        public static NotificationDTO MapToDTO(Notification notification)
        {
            return new NotificationDTO
            (
                notification.Message,
                notification.Channel,
                notification.Recipient,
                notification.Timezone
                );
        }
    }
}
