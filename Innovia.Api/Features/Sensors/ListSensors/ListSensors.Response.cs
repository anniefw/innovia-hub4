namespace Innovia.Api.Features.Sensors.ListSensors;

public sealed record Response(
Guid Id,
string Serial,
string Model,
string Status,
Guid? RoomId,
string RoomName,
string RoomType
);