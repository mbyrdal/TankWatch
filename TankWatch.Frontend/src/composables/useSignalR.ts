import * as signalR from '@microsoft/signalr';

export function useSignalR() {
  let connection: signalR.HubConnection | null = null;

  const startConnection = async (onPriceUpdate: (price: any) => void) => {
    connection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5122/priceHub')
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
