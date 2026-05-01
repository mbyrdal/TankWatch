import * as signalR from '@microsoft/signalr';

export function useSignalR() {
  let connection: signalR.HubConnection | null = null;

  const startConnection = async (onPriceUpdate: (price: any) => void) => {
    // Get the API base URL from environment (set by Vite)
    const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5122/api';
    // Remove trailing '/api' to get the base domain, then add '/priceHub'
    const signalRUrl = apiBaseUrl.replace(/\/api$/, '') + '/priceHub';

    connection = new signalR.HubConnectionBuilder()
      .withUrl(signalRUrl)
      .withAutomaticReconnect()
      .build();

    try {
      await connection.start();
      console.log('SignalR connected');
      connection.on('PriceUpdated', (price) => {
        onPriceUpdate(price);
      });
    } catch (err) {
      console.error('SignalR connection error:', err);
    }
  };

  const subscribeToStation = async (stationId: number) => {
    if (connection) {
      await connection.invoke('SubscribeToStation', stationId);
    }
  };

  const stopConnection = async () => {
    if (connection) {
      await connection.stop();
    }
  };

  return { startConnection, subscribeToStation, stopConnection };
}
