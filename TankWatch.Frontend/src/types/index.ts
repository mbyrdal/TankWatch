export interface Station {
  id: number;
  externalId: string | null;
  name: string;
  brand: string;
  address: string;
  city: string;
  postalCode: string;
  latitude: number;
  longitude: number;
  isActive: boolean;
  lastUpdated: string;
}

export interface Price {
  gasStationId: number;
  stationName: string;
  fuelType: string;
  amount: number;
  updatedAt: string;
  latitude: number;
  longitude: number;
}
