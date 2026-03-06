<template>
  <div class="location-picker">
    <button @click="getGeolocation" :disabled="isLoading" class="location-button" title="Use my current location">
      <svg v-if="!isLoading" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 490.328" width="20" height="20" fill="currentColor" style="margin-right: 8px;">
        <path fill-rule="nonzero" d="M109.525 318.077c6.788 0 12.291 5.502 12.291 12.29 0 6.788-5.503 12.291-12.291 12.291H71.371L33.266 465.747H477.89l-41.374-123.089h-28.585c-6.788 0-12.291-5.503-12.291-12.291s5.503-12.29 12.291-12.29h46.17L512 490.328H0l53.325-172.251h56.2zm109.962-178.388a7.653 7.653 0 015.13-6.943c-.352-5.996-1.084-15.037.542-20.811a25.469 25.469 0 0111.358-14.427 33.062 33.062 0 016.167-2.948c3.931-1.417 1.984-7.99 6.262-8.077 10.001-.258 26.436 8.891 32.849 15.834a25.384 25.384 0 016.548 16.449l-.407 14.66a5.747 5.747 0 014.205 3.587c2.132 8.631-18.891 29.001-18.891 31.186.05.749.345 1.465.838 2.033 14.587 20.049 53.895 11.106 53.895 50.984H183.596c0-39.89 39.319-30.921 53.895-50.972.714-1.048 1.048-1.627 1.036-2.082 0-1.951-19.04-21.425-19.04-28.473zm41.053 261.619c20.888-13.489 39.699-29.699 56.157-47.739 36.576-40.092 61.224-88.961 71.123-136.951 9.737-47.208 5.196-93.365-16.38-128.955a125.636 125.636 0 00-22.848-27.775c-26.077-23.795-55.949-35.148-85.98-35.4-31.601-.267-63.688 11.681-92.062 34.289-12.088 9.633-21.982 20.737-29.751 32.966-19.921 31.356-25.924 70.29-19.344 111.069 6.715 41.627 26.514 85.134 58.012 124.668 21.641 27.161 48.813 52.402 81.073 73.828zm74.303-31.224c-19.593 21.474-42.426 40.641-68.131 56.223l-6.533 3.959-6.382-4.055c-37.421-23.773-68.795-52.417-93.533-83.465-34.114-42.817-55.619-90.324-63.003-136.094-7.523-46.622-.435-91.493 22.799-128.064 9.185-14.456 20.9-27.599 35.223-39.013C188.035 13.476 225.512-.309 262.804.005c36.001.304 71.564 13.688 102.303 41.736 10.784 9.837 19.848 21.016 27.263 33.247 24.938 41.137 30.383 93.527 19.452 146.526-10.77 52.22-37.451 105.241-76.979 148.57z"/>
      </svg>
      <span v-if="isLoading" class="spinner" style="margin-right: 8px;"></span>
      {{ isLoading ? 'Getting location...' : 'My Location' }}
    </button>

    <div class="manual-input">
      <p>Or enter location manually:</p>
      <div class="input-row">
        <input v-model.number="manualLat" placeholder="Lat" type="number" step="any" />
        <input v-model.number="manualLng" placeholder="Lon" type="number" step="any" />
        <button @click="submitManualCoords" :disabled="!manualLat || !manualLng">Set</button>
      </div>
      <div class="input-row">
        <input v-model="addressQuery" placeholder="Address" />
        <button @click="geocodeAddress" :disabled="!addressQuery">Search</button>
      </div>
    </div>

    <div v-if="geocodeError" class="error">{{ geocodeError }}</div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import axios from 'axios';

const emit = defineEmits<{
  (e: 'location-selected', location: { lat: number; lng: number }): void;
  (e: 'location-error', message: string): void;
}>();

const isLoading = ref(false);
const manualLat = ref<number | null>(null);
const manualLng = ref<number | null>(null);
const addressQuery = ref('');
const geocodeError = ref('');

// Geolocation
function getGeolocation() {
  isLoading.value = true;
  if (!navigator.geolocation) {
    emit('location-error', 'Geolocation not supported by your browser.');
    isLoading.value = false;
    return;
  }
  navigator.geolocation.getCurrentPosition(
    (pos) => {
      const lat = pos.coords.latitude;
      const lng = pos.coords.longitude;
      if (lat < 54 || lat > 58 || lng < 8 || lng > 15) {
        emit('location-error', 'Geolocation returned coordinates outside Denmark – using default');
        // Fallback to center of Denmark
        emit('location-selected', { lat: 56.0, lng: 10.0 });
      } else {
        emit('location-selected', { lat, lng });
      }
      isLoading.value = false;
    },
    (err) => {
      let msg = 'Failed to get location.';
      if (err.code === 1) msg = 'Location permission denied.';
      else if (err.code === 2) msg = 'Position unavailable.';
      else if (err.code === 3) msg = 'Location request timed out.';
      emit('location-error', msg);
      isLoading.value = false;
    },
    { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }
  );
}

// Manual coordinates
function submitManualCoords() {
  const lat = parseFloat(manualLat.value as any);
  const lng = parseFloat(manualLng.value as any);
  if (isNaN(lat) || isNaN(lng)) {
    emit('location-error', 'Invalid coordinates – must be numbers');
    return;
  }
  // Rough bounds for Denmark
  if (lat < 54 || lat > 58 || lng < 8 || lng > 15) {
    emit('location-error', 'Coordinates outside Denmark – please enter coordinates within Denmark');
    return;
  }
  emit('location-selected', { lat, lng });
}

// Geocoding with Nominatim
async function geocodeAddress() {
  if (!addressQuery.value.trim()) return;
  geocodeError.value = '';
  isLoading.value = true;
  try {
    const response = await axios.get('https://nominatim.openstreetmap.org/search', {
      params: {
        q: addressQuery.value,
        format: 'json',
        limit: 1,
      },
      headers: {
        'User-Agent': 'TankWatch/1.0', // Required by Nominatim policy
      },
    });
    if (response.data && response.data.length > 0) {
      const { lat, lon } = response.data[0];
      const latNum = parseFloat(lat);
      const lonNum = parseFloat(lon);

      // Validate numbers and rough Denmark bounds
      if (isNaN(latNum) || isNaN(lonNum)) {
        geocodeError.value = 'Geocoding returned invalid coordinates.';
      } else if (latNum < 54 || latNum > 58 || lonNum < 8 || lonNum > 15) {
        geocodeError.value = 'Location outside Denmark – please try a Danish address.';
      } else {
        emit('location-selected', { lat: latNum, lng: lonNum });
      }
    } else {
      geocodeError.value = 'Address not found.';
    }
  } catch (error) {
    geocodeError.value = 'Geocoding service error.';
    console.error(error);
  } finally {
    isLoading.value = false;
  }
}
</script>

<style scoped>
.location-picker {
  border: 1px solid #ccc;
  width: 100%;
  box-sizing: border-box;
  padding: 15px;
  border-radius: 8px;
  margin-bottom: 20px;
}

.manual-input {
  margin-top: 15px;
}

/* Input rows (for lat/lng and address) */
.input-row {
  display: flex;
  gap: 8px;
  margin-top: 10px;
  align-items: center;
}
.input-row input {
  flex: 1;
  min-width: 0;
  padding: 0.6rem 1rem;
  height: 40px;
  box-sizing: border-box;
}
.input-row button {
  height: 40px;
  min-width: 80px;
  padding: 0 1rem;
  white-space: nowrap;
  flex-shrink: 0;
  line-height: 1;
  box-sizing: border-box;
}

/* Location button – full width with icon */
.location-button {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 42px;               /* slightly taller for emphasis */
  padding: 0 1.5rem;
  box-sizing: border-box;
  /* The gradient, border-radius, etc. are inherited from MapView's .location-picker button */
}

/* Input fields base style */
input {
  flex: 1;
  padding: 6px;
  border: 1px solid #ddd;
  border-radius: 40px;
  font-size: 0.95rem;
  transition: border-color 0.2s, box-shadow 0.2s;
}
input:focus {
  outline: none;
  border-color: #42b983;
  box-shadow: 0 0 0 3px rgba(66, 185, 131, 0.2);
}

/* Spinner */
.spinner {
  display: inline-block;
  width: 20px;
  height: 20px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-radius: 50%;
  border-top-color: white;
  animation: spin 0.8s linear infinite;
}
@keyframes spin {
  to { transform: rotate(360deg); }
}

/* Error message */
.error {
  color: red;
  margin-top: 10px;
}

/* Remove old classes no longer used */
/* .geo-button, .coords, .address are gone */
</style>
