import { defineStore } from 'pinia';
import type { Station, Price } from '../types';

export const useStationStore = defineStore('station', {
  state: () => ({
    stations: [] as Station[],
    nearbyPrices: [] as Price[],
    selectedStation: null as Station | null,
  }),
  actions: {
    setStations(stations: Station[]) {
      this.stations = stations;
    },
    setNearbyPrices(prices: Price[]) {
      console.log('Setting nearbyPrices:', prices);
      this.nearbyPrices = prices;
    },
    selectStation(station: Station) {
      this.selectedStation = station;
    },
    updatePrice(updatedPrice: Price) {
      // Update in nearbyPrices
      const index = this.nearbyPrices.findIndex(
        p => p.gasStationId === updatedPrice.gasStationId && p.fuelType === updatedPrice.fuelType
      );
      if (index !== -1) {
        this.nearbyPrices[index] = updatedPrice;
      }
      // Optionally update in stations list if you have prices embedded
    },
  },
});
