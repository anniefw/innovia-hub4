using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;
using System.Text;

var factory = new MqttFactory();
var client = factory.CreateMqttClient();

var options = new MqttClientOptionsBuilder()
    .WithTcpServer("localhost", 1883)
    .Build();

Console.WriteLine("Edge.Simulator starting… connecting to MQTT at localhost:1883");
try
{
    await client.ConnectAsync(options);
    Console.WriteLine("✅ Connected to MQTT broker.");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Failed to connect to MQTT broker: {ex.Message}");
    throw;
}

var rand = new Random();
while (true)
{
    var payload = new
    {
        deviceId = "dev-101",
        apiKey = "dev-101-key",
        timestamp = DateTimeOffset.Now,
        metrics = new object[]
        {
            new { type = "temperature", value = 21.5 + rand.NextDouble(), unit = "C" },
            new { type = "co2", value = 900 + rand.Next(0, 700), unit = "ppm" }
        }
    };

    var topic = "tenants/innovia/devices/dev-101/measurements";
    var json = JsonSerializer.Serialize(payload);

    var message = new MqttApplicationMessageBuilder()
        .WithTopic(topic)
        .WithPayload(Encoding.UTF8.GetBytes(json))
        .Build();

    await client.PublishAsync(message);
    Console.WriteLine($"[{DateTimeOffset.UtcNow:o}] Published to '{topic}': {json}");
    await Task.Delay(TimeSpan.FromSeconds(10));
}
