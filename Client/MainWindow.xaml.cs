using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Client.Model;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using Application = System.Windows.Application;
using ListBox = System.Windows.Controls.ListBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Drawing.Point;

namespace Client;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow: Window {
  public ObservableCollection<string> DevicesList { get; } = [];
  public ObservableCollection<ulong> Addresses { get; } = [];

  public ObservableCollection<string> ServiceNames { get; } = [];
  public ObservableCollection<Guid> Services { get; } = [];

  public MainWindow() {
    InitializeComponent();
    DataContext = this;

    TitleBar.MouseLeftButtonDown += OnMouseLeftButtonDown;
    TitleBar.MouseLeftButtonUp += OnMouseRightButtonDown;
    TitleBar.MouseMove += OnMouseMove;

    DeviceNames.ItemsSource = DevicesList;
    AddressList.ItemsSource = Addresses;

    DeviceNames.MouseDoubleClick += ConnectToDevice;

    ServiceNameList.ItemsSource = ServiceNames;
    ServiceList.ItemsSource = Services;

    _bleService.DeviceDiscovered += OnDeviceDiscovered;
    _bleService.Disconnected += OnDeviceDisconnected;
  }

  private uint _lastTime = 0;
  private readonly BLEService _bleService = new();

  // Title bar
  private double _top, _left;
  private bool _mouseDown = false;
  private Point _oldPos = new();

  private void OnMouseMove(object s, MouseEventArgs e) {
    if(_mouseDown is false) return;
    Point currentPos = System.Windows.Forms.Cursor.Position;
    Point deltaPos = currentPos - ((System.Drawing.Size)_oldPos);
    Left = _left + deltaPos.X;
    Top = _top + deltaPos.Y;
  }
  private void OnMouseLeftButtonDown(object s, MouseButtonEventArgs e) {
    _mouseDown = true;
    _oldPos = System.Windows.Forms.Cursor.Position;
    _top = Top;
    _left = Left;
  }
  private void OnMouseRightButtonDown(object s, MouseButtonEventArgs e) => _mouseDown = false;
  private void OnMinimizePress(object s, RoutedEventArgs e) => Dispatcher.Invoke(() => WindowState = WindowState.Minimized);
  private void OnMaximizePress(object s, RoutedEventArgs e) => Dispatcher.Invoke(() => WindowState = WindowState is WindowState.Maximized ? WindowState.Normal : WindowState.Maximized);
  private void OnClosePress(object s, RoutedEventArgs e) => Dispatcher.Invoke(Application.Current.Shutdown);

  // BLE Devices
  private async void OnRefreshClick(object s, RoutedEventArgs e) {
    Dispatcher.Invoke(() => {
      Devices.IsEnabled = false;
      DevicesList.Clear();
      Addresses.Clear();
    });
    _bleService.StartScan();
    await Task.Delay(10000);
    _bleService.StopScan();
    Dispatcher.Invoke(() => Devices.IsEnabled = true);
  }
  private async void ConnectToDevice(object s, MouseButtonEventArgs e) {
    if(s is not ListBox listbox) return;

    Dispatcher.Invoke(() => {
      ServiceNames.Clear();
      Services.Clear();
    });

    List<GattDeviceService> services = await _bleService.ConnectAsync(Addresses[listbox.SelectedIndex]);

    foreach(GattDeviceService service in services) {
      string name = string.Empty;

      if(service.Uuid.ToString().StartsWith("00001800")) name = "GAP Service";
      if(service.Uuid.ToString().StartsWith("00001801")) name = "GATT Service";
      if(service.Uuid.ToString().StartsWith("00000000")) {
        name = "IMU Data Service";
        GattCharacteristicsResult list = await service.GetCharacteristicsAsync();
        foreach(GattCharacteristic characteristic in list.Characteristics) {
          if(!characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify)) continue;
          characteristic.ValueChanged += OnDeviceNotify;

          GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
          if(status is GattCommunicationStatus.Success) continue;
          Console.WriteLine("Failed to enable notifications.");
        }
      }

      Dispatcher.Invoke(() => {
        Services.Add(service.Uuid);
        ServiceNames.Add(name);
      });
    }
  }

  // BLE Updates
  private void OnDeviceDiscovered(string name, ulong address) {
    if(Addresses.Contains(address)) return;
    Dispatcher.Invoke(() => {
      DevicesList.Add(name);
      Addresses.Add(address);
    });
  }
  private void OnDeviceDisconnected() {
    Dispatcher.Invoke(() => {
      ServiceNames.Clear();
      Services.Clear();
    });
  }
  private void OnDeviceNotify(GattCharacteristic s, GattValueChangedEventArgs args) {
    var reader = DataReader.FromBuffer(args.CharacteristicValue);
    byte[] input = new byte[args.CharacteristicValue.Length];
    reader.ReadBytes(input);

    uint time = ((uint)input[3] << 24) | ((uint)input[2] << 16) | ((uint)input[1] << 8) | input[0];
    if(time < _lastTime) return;
    string timestamp = time.ToString();

    float w = (((uint)input[05] << 8) | input[04]) / 65535.0f;
    float x = ((((uint)input[07] << 8) | input[06]) / 32767.5f) - 1.0f;
    float y = ((((uint)input[09] << 8) | input[08]) / 32767.5f) - 1.0f;
    float z = ((((uint)input[11] << 8) | input[10]) / 32767.5f) - 1.0f;
    string data = "EKF 0 ["
          + w.ToString("F3") + " "
          + x.ToString("F3") + " "
          + y.ToString("F3") + " "
          + z.ToString("F3") + "]";

    Dispatcher.Invoke(() => {
      Timestamp.Text = timestamp;
      Orientation.Text = data;
    });
    _lastTime = time;
  }
}
