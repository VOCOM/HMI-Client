using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;

namespace Client.Model;

public class BLEService {
  public BLEService() {
    _watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };

    _watcher.Received += OnAdvertisementReceived;
  }

  public void StartScan() => _watcher.Start();
  public void StopScan() => _watcher.Stop();

  public static List<GattDeviceService>? Connect(ulong address) {
    IAsyncOperation<BluetoothLEDevice>? op = BluetoothLEDevice.FromBluetoothAddressAsync(address);
    op.Wait();
    BluetoothLEDevice? device = op.GetResults();
    if(device is null) return null;

    IAsyncOperation<GattDeviceServicesResult> res_op = device.GetGattServicesAsync();
    res_op.Wait();
    GattDeviceServicesResult result = res_op.GetResults();

    return result.Status is not GattCommunicationStatus.Success ? null : [.. result.Services];
  }

  public event Action<string, ulong>? DeviceDiscovered;

  private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher s, BluetoothLEAdvertisementReceivedEventArgs e) {
    string name = e.Advertisement.LocalName;
    ulong address = e.BluetoothAddress;
    if(string.IsNullOrEmpty(name) is false) {
      DeviceDiscovered?.Invoke(name, address);
    }
  }

  private readonly BluetoothLEAdvertisementWatcher _watcher;
}
