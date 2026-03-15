using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using OrderFlow.Domain.Events;
using OrderFlow.Infrastructure.Messaging;

namespace OrderFlow.Worker.Consumers;

public sealed class OrderCreatedConsumer : BackgroundService
{
    private readonly ILogger<OrderCreatedConsumer> _logger;
    private readonly RabbitMqOptions _options;
    private IConnection? _connection;
    private IModel? _channel;

    public OrderCreatedConsumer(IOptions<RabbitMqOptions> options, ILogger<OrderCreatedConsumer> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.Username,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        _channel.QueueDeclare(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        _channel.QueueBind(
            queue: _options.QueueName,
            exchange: _options.ExchangeName,
            routingKey: "orders.created");

        // Ensures the worker processes one message at a time - prevents overloading
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        _logger.LogInformation("OrderCreatedConsumer started. Listening on queue '{Queue}'.", _options.QueueName);

        return base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (_, ea) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.Span);
                var @event = JsonSerializer.Deserialize<OrderCreatedEvent>(body);

                if (@event is null)
                {
                    _logger.LogWarning("Received null or undeserializable message. Rejecting.");
                    _channel!.BasicReject(ea.DeliveryTag, requeue: false);
                    return;
                }

                await ProcessAsync(@event, stoppingToken);

                _channel!.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message DeliveryTag={Tag}. Requeueing.", ea.DeliveryTag);
                _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel!.BasicConsume(
            queue: _options.QueueName,
            autoAck: false,
            consumer: consumer);

        return Task.CompletedTask;
    }

    private Task ProcessAsync(OrderCreatedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing OrderCreatedEvent. OrderId={OrderId}, CustomerId={CustomerId}, Total={Total:C}, Items={Items}",
            @event.OrderId,
            @event.CustomerId,
            @event.Total,
            @event.ItemCount);

        // Extension point: send confirmation email, update inventory, trigger fulfillment, etc.

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}
