using System.Text;
using System.Text.Json;
using EmployeeWellnessAPI.Models;
using RabbitMQ.Client;

namespace EmployeeWellnessAPI.Services
{
    public class RabbitMqService
    {
        private readonly IConnection _connection;

        public RabbitMqService(IConnection connection) // Inject the shared DI RabbitMQ connection
        {
            _connection = connection;
        }

        public void Publish(ProgressMessage message)
        {
            using var channel = _connection.CreateModel();
            channel.QueueDeclare(queue: "progress_queue", durable: true, exclusive: false, autoDelete: false);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            channel.BasicPublish(exchange: "", routingKey: "progress_queue", basicProperties: null, body: body);
        }
    }
}