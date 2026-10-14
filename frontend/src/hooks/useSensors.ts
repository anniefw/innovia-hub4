import { useQuery } from "@tanstack/react-query";
import { sensorsApi } from "../api/sensors";

//Syfte: hämta, spara och och rapportera status på sensorerna

export function useSensors() {
  // Be useQuery hämta och hålla koll på sensorerna
  // Returnera allt useQuery ger tillbaka (data, isLoading, error, …) till sidan
  return useQuery({
    queryKey: ["sensors"], // VAD: Namnet på datan i cachen dvs "lådan heter sensors"
    queryFn: sensorsApi.list, // HUR: Funktionen som hämtar dvs "såhär fyller du lådan"
  });
}
