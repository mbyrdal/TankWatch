<template>
  <div class="live-feed">
    <h3>Seneste prisændringer</h3>
    <ul v-if="recentUpdates.length">
      <li v-for="update in recentUpdates" :key="update.id">
        <strong>{{ update.stationName }}</strong> – {{ update.fuelType }}:
        {{ update.amount }} DKK
        <small>{{ formatTime(update.updatedAt) }}</small>
      </li>
    </ul>
    <p v-else class="no-updates">Ingen nye priser endnu.</p>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useSignalR } from '@/composables/useSignalR';

const recentUpdates = ref<any[]>([]);
const signalR = useSignalR();

onMounted(async () => {
  await signalR.startConnection((price) => {
    // Add new update to the top, keep last 10
    recentUpdates.value = [price, ...recentUpdates.value].slice(0, 10);
  });
});

function formatTime(iso: string) {
  return new Date(iso).toLocaleTimeString('da-DK', {
    hour: '2-digit',
    minute: '2-digit',
  });
}
</script>

<style scoped>
.live-feed {
  margin-top: 30px;
  padding: 15px;
  background: #f8f9fa;
  border-radius: 8px;
}
.live-feed ul {
  list-style: none;
  padding: 0;
}
.live-feed li {
  padding: 8px 0;
  border-bottom: 1px solid #eee;
}
.live-feed li:last-child {
  border-bottom: none;
}
.no-updates {
  color: #6c757d;
  font-style: italic;
}
</style>
