<template>
  <div class="home">
    <h1>TankWatch – Fuel Price Overview</h1>
    <p>Latest prices from Q8 and F24 stations (updated in real time)</p>

    <div v-if="loading" class="loading">Loading prices...</div>
    <div v-else>
      <!-- Brand summary cards -->
      <div class="brand-summary">
        <div v-for="brand in brandSummary" :key="brand.name" class="brand-card">
          <h3>{{ brand.name }}</h3>
          <p>Stations: {{ brand.stationCount }}</p>
          <p v-if="brand.diesel">
            <strong>Diesel:</strong> {{ brand.diesel }} DKK
            <small>({{ brand.dieselTime }})</small>
          </p>
          <p v-if="brand.benzin95">
            <strong>Benzin 95:</strong> {{ brand.benzin95 }} DKK
            <small>({{ brand.benzin95Time }})</small>
          </p>
          <p v-if="!brand.diesel && !brand.benzin95">No current prices</p>
        </div>
      </div>

      <!-- Live feed component -->
      <LiveFeed />
    </div>

    <p class="map-link">
      <router-link to="/map">Find stations near you →</router-link>
    </p>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useApi } from '@/composables/useApi';
import { useStationStore } from '@/stores/stationStore';
import { storeToRefs } from 'pinia';
import LiveFeed from '@/components/LiveFeed.vue';
import type { Price } from '@/types';

const api = useApi();
const store = useStationStore();
const { nearbyPrices } = storeToRefs(store);
const loading = ref(true);

// Default location (center of Denmark) with large radius to capture most prices
const DEFAULT_LOCATION = { lat: 56.0, lng: 10.0 };
const DEFAULT_RADIUS = 200; // km

// Helper: extract brand from station name
function getBrand(stationName: string): string {
  if (stationName.includes('F24')) return 'F24';
  if (stationName.includes('Q8')) return 'Q8';
  return 'Andet';
}

// Brand summary computed from nearbyPrices
const brandSummary = computed(() => {
  const map = new Map<string, {
    diesel: { amount: number; time: Date } | null;
    benzin95: { amount: number; time: Date } | null;
    stations: Set<number>;
  }>();

  nearbyPrices.value.forEach((p: Price) => {
    const brand = getBrand(p.stationName);
    if (!map.has(brand)) {
      map.set(brand, { diesel: null, benzin95: null, stations: new Set() });
    }
    const entry = map.get(brand)!;
    entry.stations.add(p.gasStationId);

    const priceTime = new Date(p.updatedAt);
    if (p.fuelType === 'Diesel') {
      // Keep the most recent diesel price
      if (!entry.diesel || priceTime > entry.diesel.time) {
        entry.diesel = { amount: p.amount, time: priceTime };
      }
    } else if (p.fuelType === 'Benzin 95') {
      if (!entry.benzin95 || priceTime > entry.benzin95.time) {
        entry.benzin95 = { amount: p.amount, time: priceTime };
      }
    }
  });

  return Array.from(map.entries())
    .filter(([brand]) => brand === 'F24' || brand === 'Q8') // only show these two brands
    .map(([name, data]) => ({
      name,
      stationCount: data.stations.size,
      diesel: data.diesel?.amount.toFixed(2),
      dieselTime: data.diesel?.time.toLocaleString(),
      benzin95: data.benzin95?.amount.toFixed(2),
      benzin95Time: data.benzin95?.time.toLocaleString(),
    }));
});

onMounted(async () => {
  try {
    // Fetch prices (stations are not needed for summary, but may be used elsewhere)
    console.log('Fetching prices for', DEFAULT_LOCATION, 'radius', DEFAULT_RADIUS);
    const prices = await api.getNearbyPrices(
      DEFAULT_LOCATION.lat,
      DEFAULT_LOCATION.lng,
      DEFAULT_RADIUS
    );
    console.log('Received prices:', prices);
    store.setNearbyPrices(prices);
  } catch (error) {
    console.error('Failed to load prices', error);
  } finally {
    loading.value = false;
  }
});
</script>

<style scoped>
.home {
  max-width: 800px;
  margin: 0 auto;
  padding: 20px;
}
.brand-summary {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: 1.5rem;
  margin: 2rem 0;
}
.brand-card {
  background: white;
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 4px 12px rgba(0,0,0,0.05);
  transition: transform 0.2s, box-shadow 0.2s;
}
.brand-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 12px 24px rgba(0,0,0,0.1);
}
.brand-card h3 {
  margin-top: 0;
  color: #2c3e50;
  font-size: 1.5rem;
  border-bottom: 2px solid #42b983;
  padding-bottom: 0.5rem;
}
.brand-card p {
  margin: 8px 0;
}
.loading {
  text-align: center;
  color: #666;
  margin: 40px 0;
}
.map-link {
  margin-top: 30px;
  text-align: center;
}
.map-link a {
  display: inline-block;
  padding: 0.75rem 2rem;
  background: #42b983;
  color: white;
  border-radius: 40px;
  font-weight: bold;
  text-decoration: none;
  transition: background 0.2s;
}
.map-link a:hover {
  background: #2c8e6b;
}
</style>
