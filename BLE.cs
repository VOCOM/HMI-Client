using System.Diagnostics;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace BLE;

public class BLE {
  // Events
  public event Action? DeviceConnected;
  public event Action? DeviceDisconnected;
  public event Action<byte[]>? DataReceived;

  // Public API
  public bool Scanning { get; private set; }
  public void StartScan() {
    Scanning = true;
    watcher.Start();
  }
  public void StopScan() {
    watcher.Stop();
    Scanning = false;
  }

  public BLE() {
    devices = [];
    watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };

    watcher.Received += OnAdvertisementReceived;
  }

  uint lastTime = 0;

  readonly BluetoothLEAdvertisementWatcher watcher;
  readonly List<BluetoothLEDevice> devices;

  // Event Handlers
  async void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher s, BluetoothLEAdvertisementReceivedEventArgs e) {
    string name = e.Advertisement.LocalName;
    ulong address = e.BluetoothAddress;

    if(name != "HMI Glove") return;

    bool connected = await Connect(address);
    Debug.WriteLine((connected ? "Connected" : "Failed to connect") + $" to {name}");
    if(connected) DeviceConnected?.Invoke();
  }
  void OnConnectionStatusChanged(BluetoothLEDevice s, object o) {
    if(s.ConnectionStatus is BluetoothConnectionStatus.Disconnected) DeviceDisconnected?.Invoke();
  }

  void OnDataNotify(GattCharacteristic c, GattValueChangedEventArgs e) {
    var reader = DataReader.FromBuffer(e.CharacteristicValue);
    byte[] input = new byte[e.CharacteristicValue.Length];
    reader.ReadBytes(input);

    uint time = ((uint)input[3] << 24) | ((uint)input[2] << 16) | ((uint)input[1] << 8) | input[0];
    if(time < lastTime) return;
    lastTime = time;

    DataReceived?.Invoke(input);
  }

  // Methods
  async Task<bool> Connect(ulong address) {
    // Existing Device
    if(devices.Find(d => d.BluetoothAddress == address) is not null) return true;

    BluetoothLEDevice device = await BluetoothLEDevice.FromBluetoothAddressAsync(address);
    devices.Add(device);
    device.ConnectionStatusChanged += OnConnectionStatusChanged;

    GattDeviceServicesResult gattResult = await device.GetGattServicesAsync();
    if(gattResult.Status is not GattCommunicationStatus.Success) return false;

    List<GattDeviceService> gattServices = [.. gattResult.Services];
    GattDeviceService? dataService = gattServices.Find(s => s.Uuid.ToString() == DATA_SERVICE);
    if(dataService is null) return false;
    dataService.Session.MaintainConnection = true;

    GattCharacteristicsResult characteristicsResult = await dataService.GetCharacteristicsAsync();
    foreach(GattCharacteristic characteristic in characteristicsResult.Characteristics) {
      if(!characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify)) continue;
      characteristic.ValueChanged += OnDataNotify;

      GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
      if(status is GattCommunicationStatus.Success) continue;
      Debug.WriteLine("Failed to enable notifications.");
    }
    return true;
  }

  const string GAP_SERVICE = "00001800";
  const string GATT_SERVICE = "00001801";
  const string DATA_SERVICE = "00000000-0000-1000-8000-00805f9b34fb";
}