using System.Diagnostics;
using BluetoothLE.Peripheral;
using BluetoothLE.Structs;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace BluetoothLE.Services;

public class Scanner {

  // Events
  public event Action<string>? DeviceConnected;
  public event Action<string>? DeviceDisconnected;
  public event Action<Packet>? DataReceived;

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

  public Scanner() {
    connectionLock = new();

    deviceNames = [];
    devices = [];
    dataStreams = [];

    watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };
    watcher.Received += OnAdvertisementReceived;
  }

  uint lastTime = 0;

  readonly Lock connectionLock;
  readonly List<string> deviceNames;
  readonly List<BluetoothLEDevice> devices;
  readonly Dictionary<string, GattCharacteristic> dataStreams;
  readonly BluetoothLEAdvertisementWatcher watcher;

  // Event Handlers
  async void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher s, BluetoothLEAdvertisementReceivedEventArgs e) {
    string name = e.Advertisement.LocalName;
    ulong address = e.BluetoothAddress;

    if(name != "HMI Glove R") return;

    lock(connectionLock) {
      if(deviceNames.Contains(name)) return;
      deviceNames.Add(name);
    }

    BluetoothLEDevice device = await BluetoothLEDevice.FromBluetoothAddressAsync(address);

    GattDeviceServicesResult gattResult = await device.GetGattServicesAsync();
    if(gattResult.Status is not GattCommunicationStatus.Success) {
      deviceNames.Remove(name);
      Debug.WriteLine($"BLE: Peripheral {gattResult.Status}");
      return;
    }

    devices.Add(device);
    DeviceConnected?.Invoke(name);

    GattDeviceService service = device.GetGattService(UUIDs.DATA);
    service.Session.MaintainConnection = true;

    GattCharacteristicsResult characteristicResult = await service.GetCharacteristicsAsync();
    GattCharacteristic notification = characteristicResult.Characteristics.First(
     c => c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify));
    notification.ValueChanged += OnDataNotify;
    dataStreams.Add(name, notification);

    // Enable Data Notification
    GattCommunicationStatus status = await notification.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);

    // Attach Handlers
    device.ConnectionStatusChanged += OnConnectionStatusChanged;
  }
  void OnConnectionStatusChanged(BluetoothLEDevice s, object o) {
    switch(s.ConnectionStatus) {
      case BluetoothConnectionStatus.Disconnected:
        dataStreams.Remove(s.Name);
        deviceNames.Remove(s.Name);
        devices.Remove(s);
        DeviceDisconnected?.Invoke(s.Name);
        s.Dispose();
        lastTime = 0;
        break;
      case BluetoothConnectionStatus.Connected:
        break;
      default:
        break;
    }
  }
  void OnDataNotify(GattCharacteristic c, GattValueChangedEventArgs e) {
    var reader = DataReader.FromBuffer(e.CharacteristicValue);
    byte[] input = new byte[e.CharacteristicValue.Length];
    reader.ReadBytes(input);

    uint time = ((uint)input[3] << 24) | ((uint)input[2] << 16) | ((uint)input[1] << 8) | input[0];
    if(time < lastTime) return;
    lastTime = time;

    Packet packet = ExtractData(c.Service.Device.Name, input);
    packet.Time = time;
    DataReceived?.Invoke(packet);
  }

  // Helper Methods
  static Packet ExtractData(string name, byte[] data) {
    Packet packet = new(name, 6);

    const uint stride = 14;
    for(uint i = 0; i < 6; i++) {
      uint offset = (i * stride) + 4;
      float w = ((((uint)data[offset + 1] << 8) | data[offset + 0]) / 65535.0f) - 0.0f;
      float x = ((((uint)data[offset + 3] << 8) | data[offset + 2]) / 32767.5f) - 1.0f;
      float y = ((((uint)data[offset + 5] << 8) | data[offset + 4]) / 32767.5f) - 1.0f;
      float z = ((((uint)data[offset + 7] << 8) | data[offset + 6]) / 32767.5f) - 1.0f;
      packet.Orientation[i] = new(x, y, z, w);

      x = ((((uint)data[offset + 09] << 8) | data[offset + 08]) / 32767.5f) - 1.0f;
      y = ((((uint)data[offset + 11] << 8) | data[offset + 10]) / 32767.5f) - 1.0f;
      z = ((((uint)data[offset + 13] << 8) | data[offset + 12]) / 32767.5f) - 1.0f;
      packet.Position[i] = new(x, y, z);
    }

    return packet;
  }
}