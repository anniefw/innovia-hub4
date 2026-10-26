import { useQuery } from "@tanstack/react-query";
import { sensorsApi } from "../api/sensors";
import { ApiError } from "../api/client";

export function useSensors() {
  return useQuery({
    queryKey: ["sensors"],
    queryFn: sensorsApi.list,
    retry: (failureCount, error) =>
      error instanceof ApiError && error.status === 503
        ? false
        : failureCount < 3,
  });
}
