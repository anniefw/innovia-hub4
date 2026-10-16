import { api } from "./client";
import type { Sensor } from "./types";

export const sensorsApi = {
  list: () => api.get<Sensor[]>("/sensors"),
};
