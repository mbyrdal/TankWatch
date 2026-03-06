<template>
  <div class="map-page">
    <div class="sidebar">
      <h2>Find stations near you</h2>

      <!-- Location picker component -->
      <LocationPicker
        @location-selected="onLocationSelected"
        @location-error="onLocationError"
      />

      <!-- Radius slider -->
      <div class="radius-control">
        <label>Radius: {{ radius }} km (Max 200)</label>
        <input type="range" v-model.number="radius" min="1" max="200" step="1" />
      </div>

      <!-- Station counter (only visible when map is ready) -->
      <div v-if="map" class="station-counter">
        <h3>Stations within radius by fuel type</h3>
        <ul>
          <li v-for="(count, fuelType) in visibleStationCounts" :key="fuelType">
            {{ fuelType }}: {{ count }}
          </li>
        </ul>
      </div>

      <!-- Error display -->
      <div v-if="locationError" class="error">{{ locationError }}</div>
    </div>

    <!-- The Leaflet map component -->
    <StationMap
      ref="mapComponent"
      :stations="stationsWithPrices"
      @map-ready="onMapReady"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import StationMap from '@/components/StationMap.vue';
import LocationPicker from '@/components/LocationPicker.vue';
import { useApi } from '@/composables/useApi';
import { useSignalR } from '@/composables/useSignalR';
import { useStationStore } from '@/stores/stationStore';
import { storeToRefs } from 'pinia';
import type { Station, Price } from '@/types';

const api = useApi();
const signalR = useSignalR();
const store = useStationStore();
const { stations, nearbyPrices } = storeToRefs(store);

const mapComponent = ref<InstanceType<typeof StationMap> | null>(null);
const map = ref<L.Map | null>(null);
const radius = ref(10);
const userLocation = ref<{ lat: number; lng: number } | null>(null);
const locationError = ref<string | null>(null);
const visibleStationCounts = ref<Record<string, number>>({});

// Watch for userLocation changes (debugging)
watch(userLocation, (newLoc) => {
  console.log('userLocation updated:', newLoc);
}, { immediate: true });

const stationsWithPrices = computed(() => {
  const priceMap = new Map<number, Price[]>();
  nearbyPrices.value.forEach(p => {
    if (!priceMap.has(p.gasStationId)) priceMap.set(p.gasStationId, []);
    priceMap.get(p.gasStationId)!.push(p);
  });

  // Start with all stations that have valid coordinates
  let stationsToShow = stations.value.filter(s => s.latitude && s.longitude);

  // Apply radius filter only if user location exists
  if (userLocation.value) {
    const center = userLocation.value;
    const maxDist = radius.value;
    stationsToShow = stationsToShow.filter(s => {
      const dist = getDistanceFromLatLonInKm(center.lat, center.lng, s.latitude, s.longitude);
      return dist <= maxDist;
    });
  } // else show all stations with coordinates

  // Deduplicate by ID and attach prices
  const uniqueStations = new Map<number, any>();
  stationsToShow.forEach(s => {
    uniqueStations.set(s.id, {
      ...s,
      prices: priceMap.get(s.id) || [],
      hasPrices: (priceMap.get(s.id)?.length ?? 0) > 0,
    });
  });
  return Array.from(uniqueStations.values());
});

// ---- Event Handlers ----
function onMapReady(leafletMap: L.Map) {
  map.value = leafletMap;
  // If we already have a user location, center map on it
  if (userLocation.value) {
    leafletMap.setView([userLocation.value.lat, userLocation.value.lng], 12);
  }
}

function updateStationCounts() {
  if (!map.value) return; // map ready guard (optional)
  const stationsToCount = stationsWithPrices.value; // all stations that passed radius filter
  const counts: Record<string, number> = {};
  let noPriceCount = 0;
  stationsToCount.forEach(station => {
    if (station.hasPrices) {
      station.prices.forEach((price: Price) => {
        counts[price.fuelType] = (counts[price.fuelType] || 0) + 1;
      });
    } else {
      noPriceCount++;
    }
  });
  if (noPriceCount > 0) {
    counts['No prices'] = noPriceCount;
  }
  visibleStationCounts.value = counts;
}

function onLocationSelected(location: { lat: number; lng: number }) {
  // Basic bounds for Denmark (roughly)
  if (location.lat < 54 || location.lat > 58 || location.lng < 8 || location.lng > 15) {
    console.warn('Location outside Denmark, using default', location);
    locationError.value = 'Location outside Denmark – using default (center of Denmark)';
    // Fallback to center of Denmark
    location = { lat: 56.0, lng: 10.0 };
  }
  userLocation.value = location;
  locationError.value = null;
  if (map.value) {
    map.value.setView([location.lat, location.lng], 12);
  }
  searchNearby();
}

function onLocationError(message: string) {
  locationError.value = message;
}

async function searchNearby() {
  if (!userLocation.value) return;
  console.log('Fetching nearby prices with radius', radius.value);
  try {
    const prices = await api.getNearbyPrices(
      userLocation.value.lat,
      userLocation.value.lng,
      radius.value
    );
    console.log('Received prices:', prices);
    store.setNearbyPrices(prices);
  } catch (error) {
    console.error('Failed to fetch nearby prices', error);
  }
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleString();
}

function getDistanceFromLatLonInKm(lat1: number, lon1: number, lat2: number, lon2: number): number {
  const R = 6371; // Earth's radius in km
  const dLat = (lat2 - lat1) * Math.PI / 180;
  const dLon = (lon2 - lon1) * Math.PI / 180;
  const a = Math.sin(dLat/2) * Math.sin(dLat/2) +
    Math.cos(lat1 * Math.PI/180) * Math.cos(lat2 * Math.PI/180) *
    Math.sin(dLon/2) * Math.sin(dLon/2);
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
  return R * c;
}

// ---- Watchers ----
watch(radius, () => {
  searchNearby();
});

watch(stationsWithPrices, () => {
  updateStationCounts();
}, { deep: true });

// ---- Lifecycle ----
onMounted(async () => {
  // Load all stations for the map (needed to display markers even without prices)
  try {
    const allStations = await api.getAllStations();
    store.setStations(allStations);
  } catch (error) {
    console.error('Failed to load stations', error);
  }

  // Set up SignalR for real-time updates
  await signalR.startConnection((updatedPrice) => {
    store.updatePrice(updatedPrice);
  });
});
</script>

<style scoped>
.map-page {
  display: flex;
  gap: 20px;
  padding: 20px;
  height: calc(100vh - 80px); /* adjust based on your layout */
}

/* Sidebar – modern card look */
.sidebar {
  background: #ffffff;
  border-radius: 16px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.08);
  padding: 1.5rem;
  width: 340px;
  transition: box-shadow 0.2s;
  box-sizing: border-box;
}

.sidebar:hover {
  box-shadow: 0 12px 32px rgba(0, 0, 0, 0.12);
}

/* Sidebar heading */
.sidebar h2 {
  font-size: 1.5rem;
  font-weight: 600;
  color: #2c3e50;
  margin-top: 0;
  margin-bottom: 1.5rem;
  border-bottom: 2px solid #42b983;
  padding-bottom: 0.5rem;
}

/* Prevent overflow in all sidebar children */
.sidebar * {
  max-width: 100%;
  box-sizing: border-box;
}

/* Inputs and buttons – consistent sizing */
.sidebar input,
.sidebar button {
  width: 100%;
  box-sizing: border-box;
}

/* Input fields */
.sidebar input[type="text"],
.sidebar input[type="number"] {
  border: 1px solid #ddd;
  border-radius: 40px;
  padding: 0.6rem 1rem;
  font-size: 0.95rem;
  transition: border-color 0.2s, box-shadow 0.2s;
}

.sidebar input[type="text"]:focus,
.sidebar input[type="number"]:focus {
  outline: none;
  border-color: #42b983;
  box-shadow: 0 0 0 3px rgba(66, 185, 131, 0.2);
}

/* ===== NEW BUTTON STYLES (gradient, shadow, refined hover) ===== */
.location-picker button,
.sidebar button {
  background: linear-gradient(145deg, #42b983, #2c8e6b);
  color: white;
  border: none;
  border-radius: 40px;
  padding: 0.7rem 1.5rem;
  font-weight: 600;
  font-size: 0.95rem;
  letter-spacing: 0.3px;
  cursor: pointer;
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
  transition: all 0.2s ease;
  width: 100%;
  box-sizing: border-box;
  text-align: center;
}

.location-picker button:hover,
.sidebar button:hover {
  background: linear-gradient(145deg, #2c8e6b, #1f6e4f);
  box-shadow: 0 6px 12px rgba(0, 0, 0, 0.15);
  transform: translateY(-1px);
}

.location-picker button:active,
.sidebar button:active {
  transform: translateY(1px);
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.location-picker button:disabled,
.sidebar button:disabled {
  background: #ccc;
  background-image: none;
  box-shadow: none;
  transform: none;
  cursor: not-allowed;
  opacity: 0.7;
}

/* Optional: make "Set" and "Search" buttons slightly smaller (remove if you want them full width) */
.location-picker .coords button,
.location-picker .address button {
  padding: 0.5rem 1rem;
  font-size: 0.9rem;
  width: auto;
  flex: 0 0 auto;
}
/* ===== END BUTTON STYLES ===== */

/* Radius control */
.radius-control {
  margin: 1.5rem 0;
}

.radius-control label {
  display: block;
  font-weight: 500;
  margin-bottom: 0.5rem;
  color: #2c3e50;
}

.radius-control input {
  width: 100%;
  margin: 0.5rem 0;
  -webkit-appearance: none;
  height: 6px;
  background: #ddd;
  border-radius: 3px;
}

.radius-control input::-webkit-slider-thumb {
  -webkit-appearance: none;
  width: 20px;
  height: 20px;
  background: #42b983;
  border-radius: 50%;
  cursor: pointer;
}

/* Station counter pills */
.station-counter ul {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-top: 0.5rem;
  padding-left: 0;
}

.station-counter li {
  background: #e7f3ed;
  padding: 0.4rem 1rem;
  border-radius: 40px;
  font-size: 0.9rem;
  color: #2c3e50;
  transition: background 0.2s;
  list-style: none;
}

.station-counter li:hover {
  background: #d0e8dc;
}

/* Error message */
.error {
  background: #ffebee;
  color: #c62828;
  padding: 0.75rem 1rem;
  border-radius: 40px;
  font-size: 0.9rem;
  margin-top: 1rem;
}

/* Map container (applied to the map inside StationMap) */
.map-container {
  border-radius: 16px;
  overflow: hidden;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.08);
  border: 1px solid #f0f0f0;
  height: 100%;
  width: 100%;
}

/* Responsive adjustments */
@media (max-width: 768px) {
  .map-page {
    flex-direction: column;
    height: auto;
  }
  .sidebar {
    width: 100%;
    margin-bottom: 1rem;
  }
}
</style>
