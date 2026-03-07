<template>
  <div id="map" ref="mapContainer" class="map-container"></div>
</template>

<script setup lang="ts">
import { toRaw, shallowRef } from "vue";
import { ref, onMounted, watch } from 'vue';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import 'leaflet.markercluster/dist/MarkerCluster.css';
import 'leaflet.markercluster/dist/MarkerCluster.Default.css';
import 'leaflet.markercluster/dist/leaflet.markercluster.js';

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

// Zoom state flags
let isZooming = false;
let pendingUpdate = false;

const props = defineProps<{ stations: any[]; }>();

const emit = defineEmits<{
  (e: 'map-ready', map: L.Map): void;
  (e: 'bounds-changed'): void;
}>();

let markerCluster: L.MarkerClusterGroup | null = null;

// Define custom icons
const greenIcon = L.icon({
  iconUrl: '/fuel-marker-green.svg',
  iconSize: [30, 40],
  iconAnchor: [15, 40],
  popupAnchor: [0, -35]
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
  iconUrl: '/fuel-marker-gray-selected.svg',
  iconSize: [30, 40],
  iconAnchor: [15, 40],
  popupAnchor: [0, -35]
});

let selectedMarker: L.Marker | null = null;

onMounted(() => {
  map = L.map(mapContainer.value!).setView([56.0, 10.0], 7);

  // Track zoom state
  map.on('zoomstart', () => { isZooming = true; });
  map.on('zoomend', () => {
    isZooming = false;
    if (pendingUpdate) {
      pendingUpdate = false;
      updateMarkers(toRaw(props.stations));
    }
  });

  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
  }).addTo(map);

  emit('map-ready', map);
  map.on('moveend', () => emit('bounds-changed'));
  map.on('zoomend', () => emit('bounds-changed'));
});

// Extract marker update logic into a reusable function
function updateMarkers(newStations: any[]) {
  const rawMap = toRaw(map);
  const rawStations = toRaw(newStations); // unproxy stations array

  console.log('StationMap received stations:', rawStations.length);
  if (!rawMap) return;

  if (!markerCluster) {
    markerCluster = L.markerClusterGroup({
      polygonOptions: {
        fillColor: '#42b983',
        color: '#2c8e6b',
        weight: 2,
        opacity: 0.8,
        fillOpacity: 0.3
      },
      spiderLegPolylineOptions: {
        weight: 2,
        color: '#42b983',
        opacity: 0.7
      }
    });
    rawMap.addLayer(markerCluster);
  } else {
    markerCluster.clearLayers();
  }

  selectedMarker = null;
  markers = [];

  const seen = new Set<number>();
  rawStations.forEach(station => {
    if (station.latitude && station.longitude && !seen.has(station.id)) {
      seen.add(station.id);

      const hasPrices = station.prices && station.prices.length > 0;
      const icon = hasPrices ? greenIcon : grayIcon;
      const marker = L.marker([station.latitude, station.longitude], { icon });

      // Build popup content (unchanged)
      let popupContent = `<b>${station.name}</b><br>${station.address}<br><small>Brand: ${station.brand}</small>`;
      if (hasPrices) {
        popupContent += '<br><br><b>Prices</b><br>';
        station.prices.forEach((price: any) => {
          const date = new Date(price.updatedAt).toLocaleString();
          popupContent += `${price.fuelType}: ${price.amount} DKK <small>(${date})</small><br>`;
        });
      }
      marker.bindPopup(popupContent);

      (marker as any).originalHasPrices = hasPrices;

      marker.on('click', () => {
        if (selectedMarker !== marker) {
          if (selectedMarker) {
            const original = (selectedMarker as any).originalHasPrices;
            selectedMarker.setIcon(original ? greenIcon : grayIcon);
          }
          marker.setIcon(hasPrices ? greenIconSelected : grayIconSelected);
          selectedMarker = marker;
        }
      });

      marker.on('popupclose', () => {
        if (selectedMarker === marker) {
          marker.setIcon(hasPrices ? greenIcon : grayIcon);
          selectedMarker = null;
        }
      });

      toRaw(markerCluster).addLayer(marker);
      markers.push(marker);
    }
  });
}

// Watch for station changes with zoom deferral
watch(() => props.stations, (newStations) => {
  if (!map) {
    console.warn('Map not ready yet');
    return;
  }
  if (isZooming) {
    pendingUpdate = true;
  } else {
    updateMarkers(toRaw(newStations));
  }
}, { deep: true, immediate: true });
</script>

<style scoped>
.map-container {
  width: 100%;
  height: 100%;
}
</style>
