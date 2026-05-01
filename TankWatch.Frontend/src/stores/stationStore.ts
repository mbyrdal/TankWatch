import { defineStore } from 'pinia';
import type { Station, Price, FuelType } from '../types';
import { useApi } from '@/composables/useApi';

export const useStationStore = defineStore('station', {
  state: () => ({
    stations: [] as Station[],
    fuelTypes: [] as FuelType[],
    nearbyPrices: [] as Price[],
    selectedStation: null as Station | null,
  }),
  actions: {
    setStations(stations: Station[]) {
      this.stations = stations;
    },
    async fetchStations() {
      const api = useApi();
      const stations = await api.getAllStations();
      this.setStations(stations);
    },
    async fetchFuelTypes() {
      const api = useApi();
      const fuelTypes = await api.getFuelTypes();
      this.fuelTypes = fuelTypes;
    },
    setNearbyPrices(prices: Price[]) {
      this.nearbyPrices = prices;
    },
    selectStation(station: Station) {
      this.selectedStation = station;
    },
    updatePrice(updatedPrice: Price) {
      const index = this.nearbyPrices.findIndex(
        p => p.gasStationId === updatedPrice.gasStationId && p.fuelType === updatedPrice.fuelType
      );
      if (index !== -1) {
        this.nearbyPrices[index] = updatedPrice;
      }
    },
  },
});
