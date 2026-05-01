using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WarehouseIntegrationAPI.Services
{
    public class IntegrationWorker : BackgroundService
    {
        private IConnection _connection;
        private IModel _channel;

        public IntegrationWorker()
        {
            InitRabbitMQ();
        }

        private void InitRabbitMQ()
        {
            var factory = new ConnectionFactory { HostName = "rabbitmq" };

            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.QueueDeclare(queue: "bin_allocation_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
                _channel.QueueDeclare(queue: "order_assignment_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
                
                Console.WriteLine("RabbitMQ Background Worker Initialized.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RabbitMQ connection failed on startup (worker): {ex.Message}");
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel == null || _channel.IsClosed) return Task.CompletedTask;

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                // Simulate processing time
                Thread.Sleep(500);

                Console.WriteLine($" [x] Processed message from {ea.RoutingKey}: {message}");
            };

            _channel.BasicConsume(queue: "bin_allocation_queue", autoAck: true, consumer: consumer);
            _channel.BasicConsume(queue: "order_assignment_queue", autoAck: true, consumer: consumer);

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
