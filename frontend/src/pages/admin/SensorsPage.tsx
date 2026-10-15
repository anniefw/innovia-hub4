import { useState } from "react";
import type { Sensor, SensorReading } from "../../api/types";
import { useSensorReadings } from "../../hooks/useSensorReadings";
import { useSensors } from "../../hooks/useSensors";
import { groupByRoomType } from "../../lib/sensorGrouping";
import { ResourceTypeFilter } from "../../components/ResourceTypeFilter";

function SensorCard({
  sensor,
  reading,
}: {
  sensor: Sensor;
  reading?: SensorReading;
}) {
  //{ sensor }: variabeln du använder inne i komponenten
  // { sensor: … }: fältnamnet i typbeskrivningen
  //Sensor (stor bokstav): typen från types.ts
  return (
    <div className="flex flex-col h-full rounded-xl border border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-900 p-4">
      {/*1. Rumsnamn*/}
      <p className="font-semibold text-gray-900 dark:text-gray-100">
        {sensor.roomName}
      </p>

      {/*2. Mätvärden*/}
      <div className="flex justify-center gap-8 my-4">
        <div className="text-center">
          <p className="text-xs text-gray-500 dark:text-gray-400">Temperatur</p>
          <p className="text-2xl font-semibold text-gray-900 dark:text-gray-100">
            {reading?.temperature !== undefined
              ? `${reading.temperature.toFixed(1)} °C`
              : "– °C"}
          </p>
        </div>
        <div className="text-center">
          <p className="text-xs text-gray-500 dark:text-gray-400">CO₂</p>
          <p className="text-2xl font-semibold text-gray-900 dark:text-gray-100">
            {reading?.co2 !== undefined ? `${reading.co2} ppm` : "– ppm"}
          </p>
        </div>
      </div>

      <div className="mt-auto border-t border-gray-100 dark:border-gray-800 pt-2 text-xs text-gray-500 dark:text-gray-400 flex justify-between">
        <span>
          {sensor.serial} · {sensor.model}
        </span>
        {/* ▶ UTC från servern → svensk lokal tid i webbläsaren */}
        {reading?.time && (
          <span>{new Date(reading.time).toLocaleTimeString("sv-SE")}</span>
        )}
      </div>
    </div>
  );
}

const SensorsPage = () => {
  const { data: sensors, isLoading, error } = useSensors();
  const { readings, isLive } = useSensorReadings();
  const [selectedType, setSelectedType] = useState<string | null>(null);
  const grouped = groupByRoomType(sensors ?? []);
  const typeOptions = [...grouped.keys()].map((type) => ({
    id: type,
    name: type,
  }));
  const visibleGroups = [...grouped].filter(
    ([roomType]) => selectedType === null || roomType === selectedType,
  );
  return (
    <div>
      {/* ▶ Rubrik + live-indikator på samma rad */}
      <div className="flex items-center gap-3 mb-4">
        <h1 className="text-2xl font-semibold text-gray-900 dark:text-gray-100">
          Sensorer
        </h1>
        <span
          className={`flex items-center gap-1.5 text-xs font-medium ${isLive ? "text-green-600" : "text-gray-400"}`}
        >
          <span
            className={`h-2 w-2 rounded-full ${isLive ? "bg-green-500 animate-pulse" : "bg-gray-400"}`}
          />
          {isLive ? "Live" : "Ej ansluten"}
        </span>
      </div>

      {sensors && (
        <ResourceTypeFilter
          resourceTypes={typeOptions}
          selectedTypeId={selectedType}
          onChange={setSelectedType}
        />
      )}

      {isLoading && <p className="text-gray-500">Laddar sensorer...</p>}

      {/* error.message kommer från ApiError i client.ts → backendens "detail"-text */}
      {error && (
        <p className="rounded-md bg-red-50 dark:bg-red-950 text-red-700 dark:text-red-300 p-3">
          {error.message}
        </p>
      )}

      {sensors && (
        // ▶ En sektion per grupp, med luft mellan (space-y-8)
        <div className="space-y-8">
          {/* ▶ Map → array av [typnamn, sensorer] → en sektion per grupp */}
          {visibleGroups.map(([roomType, group]) => (
            <section key={roomType}>
              {/* ▶ Rubrik med antal, t.ex. "Mötesrum (4)" */}
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-3">
                {roomType}{" "}
                <span className="text-sm font-normal text-gray-500 dark:text-gray-400">
                  ({group.length})
                </span>
              </h2>

              {/* ▶ Samma rutnät som innan, men bara för den här gruppen */}
              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                {group.map((sensor) => (
                  <SensorCard
                    key={sensor.id}
                    sensor={sensor}
                    reading={readings[sensor.id]}
                  />
                ))}
              </div>
            </section>
          ))}
        </div>
      )}
    </div>
  );
};

export default SensorsPage;
