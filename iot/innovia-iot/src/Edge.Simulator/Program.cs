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

var serials = Enumerable.Range(101,10)
    .Select(n => $"dev-{n}")
    .ToArray();

const string serverRoomSerial = "dev-105";

var interval = TimeSpan.FromSeconds(9);

var rand = new Random();
while (true)
{
    foreach (var serial in serials)
    {
        var temperature = serial == serverRoomSerial
        ? 26.0 + rand.NextDouble()*4.0 //AI-serverrum 26-30 c
        : 26.0 + rand.NextDouble(); //Övriga rum 21,5 - 22,5 c

    var payload = new
    {
        deviceId = serial,
        apiKey = $"{serial}-key", 
        timestamp = DateTimeOffset.Now,
        metrics = new object[]
        {
            new { type = "temperature", value = 21.5 + rand.NextDouble(), unit = "C" },
            new { type = "co2", value = 900 + rand.Next(0, 700), unit = "ppm" }
        }
    
};
    //topic måste innehålla rätt serial (ingest.gateway läser ur topic)
    var topic = $"tenants/innovia/devices/{serial}/measurements";
    var json = JsonSerializer.Serialize(payload);

    var message = new MqttApplicationMessageBuilder()
        .WithTopic(topic)
        .WithPayload(Encoding.UTF8.GetBytes(json))
        .Build();

    await client.PublishAsync(message);
    // Kortare loggrad än originalet – 10 hela JSON-rader var 5:e sekund blir oläsligt
    Console.WriteLine($"[{DateTimeOffset.UtcNow:HH:mm:ss}] {serial} → {topic}");
    // Vänta EFTER att alla 10 skickats – inte mellan varje enhet
    await Task.Delay(interval);
    }

}
