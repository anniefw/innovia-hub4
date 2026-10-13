namespace Innovia.Api.Features.Sensors.ListSensors;

public sealed record Response (
Guid Id, //DeviceRegistrys Guid - matchas mot deviceId i signalR-event
string Serial, //dev-101
string Model, //Acme C02-Temp
string Status, //active
Guid? RoomId, //Resource.Id i Innovias databas (kan saknas)
string RoomName //Mötesrum 1, slås upp i AbbDbContext
);