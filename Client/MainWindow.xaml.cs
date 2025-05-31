using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Client.Model;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace Client;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow: Window {
  public ObservableCollection<string> Devices { get; } = [];
  public ObservableCollection<ulong> Addresses { get; } = [];

  public ObservableCollection<string> ServiceNames { get; } = [];
  public ObservableCollection<Guid> Services { get; } = [];

  public TextBlock IMUData { get; }

  public MainWindow() {
    InitializeComponent();
    DataContext = this;

    DeviceList.ItemsSource = Devices;
    AddressList.ItemsSource = Addresses;

    ServiceNameList.ItemsSource = ServiceNames;
    ServiceList.ItemsSource = Services;

    IMUData = IMU_Data;

    _bleService.DeviceDiscovered += OnDeviceDiscovered;
    _bleService.StartScan();
  }

  private void OnDeviceDiscovered(string name, ulong address) {
    if(Addresses.Contains(address)) return;
    Dispatcher.Invoke(() => {
      Devices.Add(name);
      Addresses.Add(address);
    });

    if(name == "HMI Glove") {
      List<GattDeviceService>? services = BLEService.Connect(address);
      if(services is null) return;

      Dispatcher.Invoke(() => {
        ServiceNames.Clear();
        Services.Clear();

        foreach(GattDeviceService service in services) {
          Services.Add(service.Uuid);
          if(service.Uuid.ToString().StartsWith("00001800")) ServiceNames.Add("GAP Service");
          if(service.Uuid.ToString().StartsWith("00001801")) ServiceNames.Add("GATT Service");

          if(service.Uuid.ToString().StartsWith("00000000")) {
            ServiceNames.Add("IMU Data Service");
            IAsyncOperation<GattCharacteristicsResult> op = service.GetCharacteristicsAsync();
            op.Wait();
            foreach(GattCharacteristic? characteristic in op.GetResults().Characteristics) {
              if(characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify)) {
                SetupNotifications(characteristic);
              }
            }
          }
        }
      });
    }
  }

  private void SetupNotifications(GattCharacteristic characteristic) {
    characteristic.ValueChanged += OnValueChanged;

    IAsyncOperation<GattCommunicationStatus> op = characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
    op.Wait();
    GattCommunicationStatus status = op.GetResults();
    if(status is not GattCommunicationStatus.Success) {
      Console.WriteLine("Failed to enable notifications.");
    }
  }

  private void OnValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args) {
    var reader = DataReader.FromBuffer(args.CharacteristicValue);
    byte[] input = new byte[args.CharacteristicValue.Length];
    reader.ReadBytes(input);

    Dispatcher.Invoke(() => IMUData.Text = Encoding.UTF8.GetString(input, 0, input.Length));
  }

  private readonly BLEService _bleService = new();
}

public class ObservableString: INotifyPropertyChanged {
  private string _value = string.Empty;

  public string Value {
    get => _value;
    set {
      if(_value == value) return;
      _value = value;
      OnPropertyChanged(nameof(Value));
    }
  }

  public override string ToString() => Value;

  public event PropertyChangedEventHandler? PropertyChanged;
  protected void OnPropertyChanged(string name) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}