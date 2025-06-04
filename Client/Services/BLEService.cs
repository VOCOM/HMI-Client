using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Client.Services;

public class BLEService {
  public event Action? Disconnected;
  public event Action<string, ulong>? DeviceDiscovered;

  public BLEService() {
    _watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };

    _watcher.Received += OnAdvertisementReceived;
  }

  public void StartScan() => _watcher.Start();
  public void StopScan() => _watcher.Stop();

  public async Task<List<GattDeviceService>> ConnectAsync(ulong address) {
    BluetoothLEDevice device = await BluetoothLEDevice.FromBluetoothAddressAsync(address);
    device.ConnectionStatusChanged += OnConnectionStatusChanged;

    GattDeviceServicesResult services = await device.GetGattServicesAsync();
    return services.Status is GattCommunicationStatus.Success ? [.. services.Services] : [];
  }

  readonly BluetoothLEAdvertisementWatcher _watcher;

  void OnConnectionStatusChanged(BluetoothLEDevice device, object args) {
    if(device.ConnectionStatus is BluetoothConnectionStatus.Disconnected) Disconnected?.Invoke();
  }
  void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher s, BluetoothLEAdvertisementReceivedEventArgs e) {
    string name = e.Advertisement.LocalName;
    ulong address = e.BluetoothAddress;

    if(string.IsNullOrEmpty(name) is false) {
      DeviceDiscovered?.Invoke(name, address);
    }
  }

}
