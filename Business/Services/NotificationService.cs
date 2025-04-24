using Business.Mappers;
using Business.Models;
using Data.Models;
using Data.Repositories;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
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
        Task ProcessPendingNotificationsAsync();
    }

    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;

        private readonly INotificationRepository _repository;

        public NotificationService(INotificationRepository repository, ILogger<NotificationService> logger)
        {
            _repository = repository;
            _logger = logger;  // Wstrzykujemy logger
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


        public async Task ProcessPendingNotificationsAsync()
        {
            var notifications = await _repository.GetNotificationsAsync();
            var notificationsToSend = notifications
                .Where(n => n.Status == "Pending")
                .ToList();

            var tasks = new List<Task>();

            foreach (var notification in notificationsToSend)
            {
                var notificationDTO = NotificationMapper.MapToDTO(notification);
                var task = SendNotificationToRabbitMQ(notificationDTO);
                tasks.Add(task);
                await _repository.UpdateNotificationAsync(notification);
            }

            await Task.WhenAll(tasks);
        }

        public async Task SendNotificationToRabbitMQ(NotificationDTO notification)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            string queueName = notification.Channel?.ToLower() switch
            {
                "email" => "email_notification_queue",
                "push" => "push_notification_queue",
                _ => "default_notification_queue"
            };

            var arguments = new Dictionary<string, object>
    {
        { "x-max-priority", 10 }
    };

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: arguments
            );

            var message = JsonConvert.SerializeObject(notification);
            var body = Encoding.UTF8.GetBytes(message);

            byte priorityValue = 1;
            if (!string.IsNullOrEmpty(notification.Priority) && byte.TryParse(notification.Priority, out byte parsedPriority))
            {
                priorityValue = parsedPriority;
            }

            var properties = new BasicProperties
            {
                Persistent = true,
                Priority = priorityValue,
                CorrelationId = Guid.NewGuid().ToString(),
                ReplyTo = "amq.rabbitmq.reply-to"
            };

            var consumer = new AsyncEventingBasicConsumer(channel);
            var tcs = new TaskCompletionSource<Notification>();

            consumer.ReceivedAsync += async (sender, e) =>
            {
                var responseBody = e.Body.ToArray();
                var responseMessage = Encoding.UTF8.GetString(responseBody);
                var responseNotification = JsonConvert.DeserializeObject<Notification>(responseMessage);

                if (e.BasicProperties.CorrelationId == properties.CorrelationId)
                {
                    tcs.SetResult(responseNotification);
                }
            };

            await channel.BasicConsumeAsync(
                queue: "amq.rabbitmq.reply-to",
                autoAck: true,
                consumer: consumer
            );

            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: queueName,
                mandatory: false,
                basicProperties: properties,
                body: body
            );

            var responseTask = Task.WhenAny(tcs.Task, Task.Delay(500));
            var completedTask = await responseTask;

            if (completedTask == tcs.Task)
            {
                var response = await tcs.Task;
                _logger.LogInformation($"Received response: {response.Status} for message: {response.Message}");
            }
            else
            {
                _logger.LogWarning("Response timeout occurred, no response received.");
            }
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
