import type { AlertRaised, Sensor } from "../../api/types";

const TYPE_LABELS: Record<string, { label: string; unit: string }> = {
  temperature: { label: "Temperatur", unit: "°C" },
  co2: { label: "CO₂", unit: "ppm" },
};

type AlertBannerProps = {
  alerts: AlertRaised[];
  sensors: Sensor[];
  onDismiss: (id: string) => void;
};

export function AlertBanner({ alerts, sensors, onDismiss }: AlertBannerProps) {
  if (alerts.length === 0) return null;

  return (
    <div className="space-y-2 mb-6">
      {alerts.map((alert) => {
        const sensor = sensors.find((s) => s.id === alert.deviceId);
        const roomName = sensor?.roomName ?? "Okänd sensor";

        const { label, unit } = TYPE_LABELS[alert.type] ?? {
          label: alert.type,
          unit: "",
        };

        const time = new Date(alert.time).toLocaleTimeString("sv-SE");

        return (
          <div
            key={alert.id}
            role="alert"
            className="flex items-center gap-3 rounded-lg border border-red-300 dark:border-red-800 bg-red-50 dark:bg-red-950 px-4 py-3"
          >
            <span aria-hidden="true">⚠️</span>

            <p className="flex-1 text-sm text-red-800 dark:text-red-200">
              <span className="font-semibold">{roomName}:</span> {label}{" "}
              {alert.value} {unit} {alert.operator} {alert.threshold} {unit}
              <span className="ml-2 text-xs text-red-600 dark:text-red-400">
                {time}
              </span>
            </p>

            <button
              onClick={() => onDismiss(alert.id)}
              aria-label="Stäng larm"
              className="text-red-600 hover:text-red-800 dark:text-red-400 dark:hover:text-red-200 text-lg leading-none"
            >
              ×
            </button>
          </div>
        );
      })}
    </div>
  );
}
