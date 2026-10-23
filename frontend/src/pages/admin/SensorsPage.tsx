import { useState } from "react";
import type { Sensor, SensorReading } from "../../api/types";
import { useSensorReadings } from "../../hooks/useSensorReadings";
import { useSensors } from "../../hooks/useSensors";
import { groupByRoomType } from "../../lib/sensorGrouping";
import { ResourceTypeFilter } from "../../components/ResourceTypeFilter";
import { useSensorAlerts } from "../../hooks/useSensorAlerts";
import { AlertBanner } from "./AlertBanner";

function SensorCard({
  sensor,
  reading,
  hasAlert = false,
}: {
  sensor: Sensor;
  reading?: SensorReading;
  hasAlert?: boolean;
}) {
  return (
    <div
      className={`flex flex-col h-full rounded-xl border bg-white dark:bg-gray-900 p-4 ${
        hasAlert
          ? "border-red-400 dark:border-red-700 ring-2 ring-red-200 dark:ring-red-900"
          : "border-gray-200 dark:border-gray-800"
      }`}
    >
      <p className="font-semibold text-gray-900 dark:text-gray-100">
        {sensor.roomName}
      </p>
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
  const { alerts, dismiss } = useSensorAlerts();

  const [selectedType, setSelectedType] = useState<string | null>(null);

  const alertedDeviceIds = new Set(alerts.map((a) => a.deviceId));

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

      {sensors && (
        <AlertBanner alerts={alerts} sensors={sensors} onDismiss={dismiss} />
      )}

      {isLoading && <p className="text-gray-500">Laddar sensorer...</p>}

      {error && (
        <p className="rounded-md bg-red-50 dark:bg-red-950 text-red-700 dark:text-red-300 p-3">
          {error.message}
        </p>
      )}

      {sensors && (
        <div className="space-y-8">
          {visibleGroups.map(([roomType, group]) => (
            <section key={roomType}>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-3">
                {roomType}{" "}
                <span className="text-sm font-normal text-gray-500 dark:text-gray-400">
                  ({group.length})
                </span>
              </h2>

              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                {group.map((sensor) => (
                  <SensorCard
                    key={sensor.id}
                    sensor={sensor}
                    reading={readings[sensor.id]}
                    hasAlert={alertedDeviceIds.has(sensor.id)}
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
