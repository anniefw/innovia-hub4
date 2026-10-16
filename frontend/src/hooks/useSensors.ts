import { useQuery } from "@tanstack/react-query";
import { sensorsApi } from "../api/sensors";

export function useSensors() {
  return useQuery({
    queryKey: ["sensors"],
    queryFn: sensorsApi.list,
  });
}
