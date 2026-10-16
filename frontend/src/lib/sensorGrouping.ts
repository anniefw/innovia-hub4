import type { Sensor } from "../api/types";

export function groupByRoomType(sensors: Sensor[]): Map<string, Sensor[]> {
  const sorted = [...sensors].sort((a, b) =>
    a.roomName.localeCompare(b.roomName, "sv", { numeric: true }),
  );

  const grouped = new Map<string, Sensor[]>();
  for (const sensor of sorted) {
    const list = grouped.get(sensor.roomType) ?? [];
    list.push(sensor);
    grouped.set(sensor.roomType, list);
  }
  return grouped;
}
