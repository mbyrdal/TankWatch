<template>
  <div class="price-development">
    <h2>Price development</h2>

    <div v-if="loadingStations" class="loading">Loading stations…</div>

    <div v-else>
      <div class="controls">
        <div class="control-group">
          <label>Brand</label>
          <select v-model="selectedBrand">
            <option value="F24">F24</option>
            <option value="Q8">Q8</option>
          </select>
        </div>

        <div class="control-group">
          <label>Fuel</label>
          <select v-model="selectedFuelTypeId">
            <option v-for="fuel in fuelTypes" :key="fuel.id" :value="fuel.id">
              {{ fuel.name }}
            </option>
          </select>
        </div>

        <div class="control-group">
          <label>Period</label>
          <select v-model="days">
            <option value="7">7 days</option>
            <option value="30">30 days</option>
            <option value="90">90 days</option>
          </select>
        </div>
      </div>

      <div v-if="loadingHistory" class="loading">Loading price history…</div>
      <div v-else-if="historyData.length === 0" class="no-data">
        No price history for the selected brand and fuel type.
      </div>
      <canvas v-else ref="chartCanvas" style="max-height: 400px; width: 100%; display: block;"></canvas>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, nextTick } from 'vue';
import { Chart, registerables } from 'chart.js';
import { useApi } from '@/composables/useApi';
import { useStationStore } from '@/stores/stationStore';
import { storeToRefs } from 'pinia';

Chart.register(...registerables);

const api = useApi();
const stationStore = useStationStore();
const { fuelTypes } = storeToRefs(stationStore);

const selectedBrand = ref<'F24' | 'Q8'>('F24');
const selectedFuelTypeId = ref<number | null>(null);
const days = ref('30');
const historyData = ref<{ date: string; price: number }[]>([]);
const loadingStations = ref(false); // we only load fuel types, not stations
const loadingHistory = ref(false);
const chartCanvas = ref<HTMLCanvasElement | null>(null);
let chartInstance: Chart | null = null;

onMounted(async () => {
  try {
    if (fuelTypes.value.length === 0) {
      await stationStore.fetchFuelTypes();
    }
    if (fuelTypes.value.length && fuelTypes.value[0]) {
      selectedFuelTypeId.value = fuelTypes.value[0].id;
    }
  } catch (err) {
    console.error('Failed to load fuel types', err);
  } finally {
    loadingStations.value = false;
  }
});

watch([selectedBrand, selectedFuelTypeId, days], async () => {
  if (!selectedFuelTypeId.value) return;
  loadingHistory.value = true;
  try {
    const data = await api.getBrandPriceHistory(
      selectedBrand.value,
      selectedFuelTypeId.value,
      parseInt(days.value)
    );
    historyData.value = data.map((d: { date: string; price: number }) => ({
      date: new Date(d.date).toLocaleDateString(),
      price: d.price,
    }));
    await nextTick();
    renderChart();
  } catch (err) {
    console.error('Failed to load price history', err);
    historyData.value = [];
  } finally {
    loadingHistory.value = false;
  }
});

watch(chartCanvas, (canvas) => {
  if (canvas && historyData.value.length > 0) {
    renderChart();
  }
});

function renderChart() {
  if (!chartCanvas.value) return;
  if (chartInstance) chartInstance.destroy();

  chartInstance = new Chart(chartCanvas.value, {
    type: 'line',
    data: {
      labels: historyData.value.map(d => d.date),
      datasets: [{
        label: `Price (DKK) - ${selectedBrand.value}`,
        data: historyData.value.map(d => d.price),
        borderColor: selectedBrand.value === 'F24' ? '#42b983' : '#3498db',
        backgroundColor: 'rgba(66, 185, 131, 0.05)',
        tension: 0.2,
        fill: true,
        pointRadius: 3,
        pointHoverRadius: 6
      }]
    },
    options: {
      responsive: true,
      maintainAspectRatio: true,
      plugins: {
        tooltip: { callbacks: { label: ctx => `${ctx.raw} DKK` } },
        legend: { position: 'top' }
      },
      scales: {
        y: { title: { display: true, text: 'Price (DKK)' }, beginAtZero: false }
      }
    }
  });
}
</script>

<style scoped>
.price-development {
  background: #f9f9f9;
  border-radius: 16px;
  padding: 1.5rem;
  margin-top: 2rem;
}
.price-development h2 {
  margin-top: 0;
  color: #2c3e50;
  font-size: 1.4rem;
}
.controls {
  display: flex;
  gap: 1rem;
  flex-wrap: wrap;
  margin-bottom: 1.5rem;
}
.control-group {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}
.control-group label {
  font-weight: 600;
  color: #2c3e50;
}
.control-group select {
  padding: 0.4rem 0.8rem;
  border-radius: 8px;
  border: 1px solid #ccc;
  background: white;
}
.loading, .no-data {
  text-align: center;
  padding: 2rem;
  color: #666;
}
canvas {
  max-height: 400px;
  width: 100%;
}
</style>
