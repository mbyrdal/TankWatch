import axios from 'axios';
import type { Station, Price } from '../types';

const apiClient = axios.create({
  baseURL: 'http://localhost:5122/api', // Adjust if your backend runs on another port
  headers: {
    'Content-Type': 'application/json',
  },
});

export function useApi() {
  const getAllStations = async (): Promise<Station[]> => {
    const response = await apiClient.get('/gasstations');
    return response.data;
  };
  const getNearbyPrices = async (lat: number, lon: number, radiusKm: number, fuelTypeId?: number): Promise<Price[]> => {
    const params: any = {
      Latitude: lat,
      Longitude: lon,
      RadiusKm: radiusKm
    };
    if (fuelTypeId) params.FuelTypeId = fuelTypeId;
    const response = await apiClient.get('/prices/nearby', { params });
    return response.data;
  };
  const reportPrice = async (stationId: number, fuelTypeId: number, amount: number) => {
    await apiClient.post('/prices/report', { stationId, fuelTypeId, amount });
  };
  const getFuelTypes = async (): Promise<FuelType[]> => {
    const response = await apiClient.get('/fueltypes');
    return response.data;
  };
  const getPriceHistory = async (stationId: number, fuelTypeId: number, days: number): Promise<{ date: string; price: number }[]> => {
    const response = await apiClient.get('/prices/history', {
      params: { stationId, fuelTypeId, days }
    });
    return response.data;
  };

  const getBrandPriceHistory = async (brand: string, fuelTypeId: number, days: number) => {
    const response = await apiClient.get('/prices/brand-history', {
      params: { brand, fuelTypeId, days }
    });
    return response.data;
  };

  return { getAllStations, getNearbyPrices, reportPrice, getFuelTypes, getPriceHistory, getBrandPriceHistory };
}
