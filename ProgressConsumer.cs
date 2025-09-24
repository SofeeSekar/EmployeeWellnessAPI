using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using EmployeeWellnessAPI.Data;
using EmployeeWellnessAPI.Models;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace EmployeeWellnessAPI.Services
{
    public class ProgressConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly IConnectionMultiplexer _redis;

        public ProgressConsumer(IServiceScopeFactory scopeFactory, IConnectionMultiplexer redis, IConnection connection)
        {
            _scopeFactory = scopeFactory;
            _redis = redis;
            _connection = connection; // Use the DI-injected RabbitMQ connection
            _channel = _connection.CreateModel();
            _channel.QueueDeclare("progress_queue", durable: true, exclusive: false, autoDelete: false);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (sender, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = JsonSerializer.Deserialize<ProgressMessage>(body);

                    Console.WriteLine($"Received message: {JsonSerializer.Serialize(message)}");

                    if (message != null)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var db = _redis.GetDatabase();

                        var participant = await context.Participants
                            .Include(p => p.Challenge)
                            .FirstOrDefaultAsync(p => p.Challenge.Id == message.ChallengeId && p.UserId == message.UserId);

                        if (participant != null)
                        {
                            var progress = new ProgressEntry
                            {
                                Id = Guid.NewGuid(),
                                Challenge = participant.Challenge,
                                UserId = participant.UserId,
                                Value = message.Value,
                                Timestamp = message.Timestamp
                            };

                            context.ProgressEntries.Add(progress);
                            await context.SaveChangesAsync();

                            var leaderboard = await context.ProgressEntries
                                .Where(pe => pe.Challenge.Id == message.ChallengeId)
                                .GroupBy(pe => pe.UserId)
                                .Select(g => new LeaderboardDto
                                {
                                    UserId = g.Key,
                                    TotalValue = g.Sum(pe => pe.Value)
                                })
                                .OrderByDescending(x => x.TotalValue)
                                .Take(10)
                                .ToListAsync();

                            await db.StringSetAsync(
                                $"leaderboard:{message.ChallengeId}",
                                JsonSerializer.Serialize(leaderboard),
                                TimeSpan.FromSeconds(10));
                        }
                    }

                    // ✅ Acknowledge after successful processing
                    _channel.BasicAck(ea.DeliveryTag, false);
                    Console.WriteLine("Message acknowledged");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing message: {ex.Message}");
                    // Optional: nack the message to requeue it
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(queue: "progress_queue", autoAck: false, consumer: consumer);

            // Keep BackgroundService alive
            return Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}