import type { Sensor } from "../api/types";

// Grupperar sensorer efter rummets typ, t.ex. "Mötesrum" → [sensor, sensor, …].
// Inom varje grupp sorteras sensorerna på rumsnamn i "naturlig" ordning
// (Skrivbord 2 före Skrivbord 10).
export function groupByRoomType(sensors: Sensor[]): Map<string, Sensor[]> {
  // 1. Sortera alla sensorer på rumsnamn, med siffror som tal (2 < 10)
  const sorted = [...sensors].sort((a, b) =>
    a.roomName.localeCompare(b.roomName, "sv", { numeric: true }),
  );

  // 2. Lägg varje sensor i rätt "låda" (grupp) efter roomType
  const grouped = new Map<string, Sensor[]>();
  for (const sensor of sorted) {
    const list = grouped.get(sensor.roomType) ?? []; // hämta lådan, eller en ny tom
    list.push(sensor); // lägg i sensorn
    grouped.set(sensor.roomType, list); // ställ tillbaka lådan
  }
  return grouped;
}
