using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;

public class Notification
{
    public string Message { get; set; }
    public string Recipient { get; set; }
    public string Channel { get; set; }
    public string Timezone { get; set; }
    public DateTime Scheduled { get; set; }
    public string Status { get; set; }
    public string CorrelationId { get; set; }
}

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly string _queueName = "email_notification_queue";

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        var args = new Dictionary<string, object>
        {
            { "x-max-priority", 10 }
        };

        await channel.QueueDeclareAsync(queue: _queueName,
                                        durable: true,
                                        exclusive: false,
                                        autoDelete: false,
                                        arguments: args);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (sender, e) =>
        {
            var body = e.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var notification = JsonConvert.DeserializeObject<Notification>(message);

            _logger.LogInformation($"Received Notification: {notification.Message} to {notification.Recipient}");

            var random = new Random();
            var isFailed = random.NextDouble() < 0.5;

            if (isFailed)
            {
                notification.Status = "Failed";
                _logger.LogWarning($"Message failed: {notification.Message}");
            }
            else
            {
                notification.Status = "Success";
                _logger.LogInformation($"Message delivered: {notification.Message}");
            }

            await SendResponse(notification, e.BasicProperties.CorrelationId, e.BasicProperties.ReplyTo);

            await channel.BasicAckAsync(e.DeliveryTag, false);
        };

        await channel.BasicConsumeAsync(queue: _queueName,
                                        autoAck: false,
                                        consumer: consumer);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    public async Task SendResponse(Notification notification, string correlationId, string replyTo)
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        var message = JsonConvert.SerializeObject(notification);
        var body = Encoding.UTF8.GetBytes(message);

        var properties = new BasicProperties
        {
            CorrelationId = correlationId,
            ReplyTo = replyTo
        };

        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: replyTo,
            mandatory: false,
            basicProperties: properties,
            body: body
        );

        _logger.LogInformation($"Sent response for: {notification.Message} with status: {notification.Status}");
    }
}
