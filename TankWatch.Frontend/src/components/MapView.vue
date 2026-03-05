<template>
  <div id="map" ref="mapContainer" class="map-container"></div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';

// Fix for missing marker images in Leaflet with Webpack/Vite
delete (L.Icon.Default.prototype as any)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
  iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
  shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
});

const mapContainer = ref<HTMLElement>();
let map: L.Map | null = null; // ensure map is loaded first
let markers: L.Marker[] = [];

const props = defineProps<{
  stations: any[]; // We'll define a proper type later
}>();

const emit = defineEmits<{
  (e: 'station-selected', stationId: number): void;
}>();

onMounted(() => {
  // Initialize map centered on Denmark
  map = L.map(mapContainer.value!).setView([56.0, 10.0], 7);

  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
  }).addTo(map);
});

// Watch for station changes and update markers
watch(() => props.stations, (newStations) => {
  if (!map) return; // null guard: map not ready yet


  // Remove old markers
  markers.forEach(marker => map!.removeLayer(marker));
  markers = [];

  // Add new markers
  newStations.forEach(station => {
    if (station.latitude && station.longitude) {
      const marker = L.marker([station.latitude, station.longitude]).addTo(map!);
      marker.bindPopup(`
        <b>${station.name}</b><br>
        ${station.address}<br>
        <small>Brand: ${station.brand}</small>
      `);
      marker.on('click', () => {
        emit('station-selected', station.id);
      });
      markers.push(marker);
    }
  });
}, { deep: true, immediate: true });
</script>

<style scoped>
.map-container {
  width: 100%;
  height: 600px;
}
</style>
