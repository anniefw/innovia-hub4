import { api } from "./client";
import type { Sensor } from "./types";

//Syfte: samlar ihop alla anrop från frontend till backendens /sensor på ett ställe
export const sensorsApi = {
  //GET/sensors - alla sensorer med rumsnamn (adminonly)
  list: () => api.get<Sensor[]>("/sensors"),
};
