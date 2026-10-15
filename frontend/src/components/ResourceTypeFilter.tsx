import type { ResourceType } from "../api/types";

export function ResourceTypeFilter({
  resourceTypes,
  selectedTypeId,
  onChange,
  className = "mb-6",
}: {
  // resourceTypes: ResourceType[];
  resourceTypes: { id: string; name: string }[];
  selectedTypeId: string | null;
  onChange: (typeId: string | null) => void;
  className?: string;
}) {
  return (
    <div className={`flex flex-wrap gap-2 ${className}`}>
      <button
        onClick={() => onChange(null)}
        className={`rounded-full px-3 py-1 text-sm font-medium border transition-colors ${
          selectedTypeId === null
            ? "border-indigo-600 bg-indigo-600 text-white"
            : "border-gray-300 text-gray-600 hover:bg-gray-100 dark:border-gray-700 dark:text-gray-300 dark:hover:bg-gray-800"
        }`}
      >
        Alla
      </button>
      {resourceTypes.map((type) => (
        <button
          key={type.id}
          onClick={() => onChange(type.id)}
          className={`rounded-full px-3 py-1 text-sm font-medium border transition-colors ${
            selectedTypeId === type.id
              ? "border-indigo-600 bg-indigo-600 text-white"
              : "border-gray-300 text-gray-600 hover:bg-gray-100 dark:border-gray-700 dark:text-gray-300 dark:hover:bg-gray-800"
          }`}
        >
          {type.name}
        </button>
      ))}
    </div>
  );
}
