<template>
  <div id="map" ref="mapContainer" class="map-container"></div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';

// Fix for missing marker images (not needed for custom icons but keep if you want fallback)
delete (L.Icon.Default.prototype as any)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
  iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
  shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
});

const mapContainer = ref<HTMLElement>();
let map: L.Map | null = null;
let markers: L.Marker[] = [];

const props = defineProps<{
  stations: any[];
}>();

const emit = defineEmits<{
  (e: 'map-ready', map: L.Map): void;
  (e: 'bounds-changed'): void;
}>();

// Define custom icons
const greenIcon = L.icon({
  iconUrl: '/fuel-marker-green.svg',
  iconSize: [30, 40],
  iconAnchor: [15, 40],   // tip of the teardrop
  popupAnchor: [0, -35]   // popup above the icon
});

const grayIcon = L.icon({
  iconUrl: '/fuel-marker-gray.svg',
  iconSize: [30, 40],
  iconAnchor: [15, 40],
  popupAnchor: [0, -35]
});

const greenIconSelected = L.icon({
  iconUrl: '/fuel-marker-green-selected.svg',
  iconSize: [30, 40],
  iconAnchor: [15, 40],
  popupAnchor: [0, -35]
});

const grayIconSelected = L.icon({
  iconUrl: '/fuel-marker-gray-selected.svg', // create this file similarly
  iconSize: [30, 40],
  iconAnchor: [15, 40],
  popupAnchor: [0, -35]
});

let selectedMarker: L.Marker | null = null;

onMounted(() => {
  map = L.map(mapContainer.value!).setView([56.0, 10.0], 7);

  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
  }).addTo(map);

  emit('map-ready', map);
  map.on('moveend', () => emit('bounds-changed'));
  map.on('zoomend', () => emit('bounds-changed'));
});

// Watch for station changes and update markers
watch(() => props.stations, (newStations) => {
  if (!map) return;
  markers.forEach(marker => map!.removeLayer(marker));
  selectedMarker = null; // clear selection when stations change
  markers = [];

  const seen = new Set<number>();
  newStations.forEach(station => {
    if (station.latitude && station.longitude && !seen.has(station.id)) {
      seen.add(station.id);

      const hasPrices = station.prices && station.prices.length > 0;
      const icon = hasPrices ? greenIcon : grayIcon;
      const marker = L.marker([station.latitude, station.longitude], { icon }).addTo(map!);

      // Build popup content
      let popupContent = `<b>${station.name}</b><br>${station.address}<br><small>Brand: ${station.brand}</small>`;
      if (hasPrices) {
        popupContent += '<br><br><b>Prices</b><br>';
        station.prices.forEach((price: any) => {
          const date = new Date(price.updatedAt).toLocaleString();
          popupContent += `${price.fuelType}: ${price.amount} DKK <small>(${date})</small><br>`;
        });
      }
      marker.bindPopup(popupContent);

      // Store whether this station has prices
      (marker as any).originalHasPrices = hasPrices;

      // Click handler – selects the marker and changes icon
      marker.on('click', () => {
        if (selectedMarker !== marker) {
          // Reset previous selected marker
          if (selectedMarker) {
            const original = (selectedMarker as any).originalHasPrices;
            selectedMarker.setIcon(original ? greenIcon : grayIcon);
          }
          // Set current marker as selected
          marker.setIcon(hasPrices ? greenIconSelected : grayIconSelected);
          selectedMarker = marker;
        }
        // If same marker, do nothing – popup toggle will handle closing
      });

      // Popup close handler – revert icon when popup is closed
      marker.on('popupclose', () => {
        if (selectedMarker === marker) {
          marker.setIcon(hasPrices ? greenIcon : grayIcon);
          selectedMarker = null;
        }
      });

      markers.push(marker);
    }
  });
}, { deep: true, immediate: true });
</script>

<style scoped>
.map-container {
  width: 100%;
  height: 100%;
}
</style>
