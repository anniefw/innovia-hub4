import type { Sensor } from "../../api/types";
import { useSensors } from "../../hooks/useSensors";
import { groupByRoomType } from "../../lib/sensorGrouping";

function SensorCard({ sensor }: { sensor: Sensor }) {
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
            – °C
          </p>
        </div>
        <div className="text-center">
          <p className="text-xs text-gray-500 dark:text-gray-400">CO₂</p>
          <p className="text-2xl font-semibold text-gray-900 dark:text-gray-100">
            – ppm
          </p>
        </div>
      </div>

      {/* 3. Teknisk info*/}
      <p className="mt-auto border-t border-gray-100 dark:border-gray-800 pt-2 text-xs text-gray-500 dark:text-gray-400">
        {sensor.serial} · {sensor.model}
      </p>
    </div>
  );
}

const SensorsPage = () => {
  // Hämta sensorerna via useSensors()  → ger data, isLoading, error
  // Visa rubrik
  // OM laddar   → visa "Laddar sensorer..."
  // OM fel      → visa felmeddelandet från backend (t.ex. 503-texten)
  // OM data     → visa ett rutnät med ett SensorCard per sensor
  const { data: sensors, isLoading, error } = useSensors();
  return (
    <div>
      <h1 className="text-2xl font-semibold text-gray-900 dark:text-gray-100 mb-4">
        Sensorer
      </h1>

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
          {[...groupByRoomType(sensors)].map(([roomType, group]) => (
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
                  <SensorCard key={sensor.id} sensor={sensor} />
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
