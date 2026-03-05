<template>
  <div class="home">
    <h1>TankWatch – Find the best fuel prices near you</h1>
    <div class="controls">
      <input v-model.number="radius" type="number" placeholder="Radius (km)" />
      <button @click="searchNearby" :disabled="!userLocation">Search nearby</button>
    </div>
    <MapView
      :stations="stationsWithPrices"
      @station-selected="selectStation"
    />
    <div v-if="selectedStation" class="station-details">
      <h3>{{ selectedStation.name }}</h3>
      <p>{{ selectedStation.address }}, {{ selectedStation.city }} {{ selectedStation.postalCode }}</p>
      <h4>Prices</h4>
      <ul>
        <li v-for="price in pricesForSelected" :key="price.fuelType">
          {{ price.fuelType }}: {{ price.amount }} DKK
          <small>(updated {{ new Date(price.updatedAt).toLocaleString() }})</small>
        </li>
      </ul>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import MapView from '../components/MapView.vue';
import { useApi } from '../composables/useApi';
import { useSignalR } from '../composables/useSignalR';
import { useStationStore } from '../stores/stationStore';
import { storeToRefs } from 'pinia';

const api = useApi();
const signalR = useSignalR();
const store = useStationStore();
const { stations, nearbyPrices, selectedStation } = storeToRefs(store);

const radius = ref(10);
const userLocation = ref<{ lat: number; lon: number } | null>(null);

// Combine stations with their current prices for map markers
const stationsWithPrices = computed(() => {
  // Group nearbyPrices by stationId to attach to stations
  const priceMap = new Map<number, Price[]>();
  nearbyPrices.value.forEach(p => {
    if (!priceMap.has(p.gasStationId)) priceMap.set(p.gasStationId, []);
    priceMap.get(p.gasStationId)!.push(p);
  });
  return stations.value.map(s => ({
    ...s,
    prices: priceMap.get(s.id) || [],
  }));
});

const pricesForSelected = computed(() => {
  if (!selectedStation.value) return [];
  return nearbyPrices.value.filter(p => p.gasStationId === selectedStation.value!.id);
});

// Get user's location
onMounted(async () => {
  if (navigator.geolocation) {
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        userLocation.value = {
          lat: pos.coords.latitude,
          lon: pos.coords.longitude,
        };
        searchNearby();
      },
      (err) => {
        console.warn('Geolocation failed, using default (Denmark center)');
        userLocation.value = { lat: 56.0, lon: 10.0 };
        searchNearby();
      }
    );
  } else {
    userLocation.value = { lat: 56.0, lon: 10.0 };
    searchNearby();
  }

  // Load all stations (for map markers)
  try {
    const allStations = await api.getAllStations();
    store.setStations(allStations);
  } catch (error) {
    console.error('Failed to load stations', error);
  }

  // Set up SignalR for real‑time updates
  await signalR.startConnection((updatedPrice) => {
    store.updatePrice(updatedPrice);
  });
});

async function searchNearby() {
  if (!userLocation.value) return;
  try {
    const prices = await api.getNearbyPrices(
      userLocation.value.lat,
      userLocation.value.lon,
      radius.value
    );
    store.setNearbyPrices(prices);
  } catch (error) {
    console.error('Failed to fetch nearby prices', error);
  }
}

function selectStation(stationId: number) {
  const station = stations.value.find(s => s.id === stationId);
  if (station) store.selectStation(station);
}
</script>

<style scoped>
.controls {
  margin: 20px 0;
}
.station-details {
  margin-top: 20px;
  padding: 15px;
  border: 1px solid #ccc;
  border-radius: 8px;
}
</style>
