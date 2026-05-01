using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace WarehouseIntegrationAPI.Services
{
    public class MessageProducer
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public MessageProducer()
        {
            // Connects to RabbitMQ container
            var factory = new ConnectionFactory() { HostName = "rabbitmq" };
            
            try 
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"RabbitMQ connection failed on startup (producer): {ex.Message}");
                // In a production app, use robust retry policies (e.g. Polly)
            }
        }

        public void SendMessage<T>(string queueName, T message)
        {
            if (_channel == null || _channel.IsClosed) return;

            _channel.QueueDeclare(queue: queueName,
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            _channel.BasicPublish(exchange: "",
                                 routingKey: queueName,
                                 basicProperties: null,
                                 body: body);

            Console.WriteLine($" [x] Sent to {queueName}: {json}");
        }
    }
}
