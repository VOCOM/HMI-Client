using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Client.Services;
using Client.Shaders;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
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
    DeviceNames.MouseDoubleClick += OnConnectClick;

    ServiceNameList.ItemsSource = ServiceNames;

    _bleService.DeviceDiscovered += OnDeviceDiscovered;
    _bleService.Disconnected += OnDeviceDisconnected;

    Viewport.Loaded += OnLoaded;
    Viewport.Render += OnRender;
    Viewport.Start();
  }

  uint _lastTime = 0;
  readonly BLEService _bleService = new();
  Shader? _shader;
  Axis3D? _axis3D;
  Quaternion orientation = Quaternion.Identity;
  Matrix4 toENU = new(
    new Vector4(1, 0, 0, 0),
    new Vector4(0, 0, 1, 0),
    new Vector4(0, 1, 0, 0),
    new Vector4(0, 0, 0, 1));

  // Title bar
  double _top, _left;
  bool _mouseDown = false;
  Point _oldPos = new();

  void OnMouseMove(object s, MouseEventArgs e) {
    if(_mouseDown is false) return;
    Point currentPos = System.Windows.Forms.Cursor.Position;
    Point deltaPos = currentPos - ((System.Drawing.Size)_oldPos);
    Left = _left + deltaPos.X;
    Top = _top + deltaPos.Y;
  }
  void OnMouseLeftButtonDown(object s, MouseButtonEventArgs e) {
    _mouseDown = true;
    _oldPos = System.Windows.Forms.Cursor.Position;
    _top = Top;
    _left = Left;
  }
  void OnMouseRightButtonDown(object s, MouseButtonEventArgs e) => _mouseDown = false;
  void OnMinimizePress(object s, RoutedEventArgs e) => Dispatcher.Invoke(() => WindowState = WindowState.Minimized);
  void OnMaximizePress(object s, RoutedEventArgs e) => Dispatcher.Invoke(() => WindowState = WindowState is WindowState.Maximized ? WindowState.Normal : WindowState.Maximized);
  void OnClosePress(object s, RoutedEventArgs e) => Dispatcher.Invoke(Application.Current.Shutdown);

  // BLE Devices
  async void OnRefreshClick(object s, RoutedEventArgs e) {
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
  async void OnConnectClick(object s, MouseButtonEventArgs e) {
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

    _lastTime = 0;
  }

  // BLE Updates
  void OnDeviceDiscovered(string name, ulong address) {
    if(Addresses.Contains(address)) return;
    Dispatcher.Invoke(() => {
      DevicesList.Add(name);
      Addresses.Add(address);
    });
  }
  void OnDeviceDisconnected() {
    Dispatcher.Invoke(() => {
      ServiceNames.Clear();
      Services.Clear();
    });
  }
  void OnDeviceNotify(GattCharacteristic s, GattValueChangedEventArgs args) {
    var reader = DataReader.FromBuffer(args.CharacteristicValue);
    byte[] input = new byte[args.CharacteristicValue.Length];
    reader.ReadBytes(input);

    // Timestamp
    uint time = ((uint)input[3] << 24) | ((uint)input[2] << 16) | ((uint)input[1] << 8) | input[0];
    if(time < _lastTime) return;
    string timestamp = time.ToString();

    // EKF 0
    float w = (((uint)input[05] << 8) | input[04]) / 65535.0f;
    float x = ((((uint)input[07] << 8) | input[06]) / 32767.5f) - 1.0f;
    float y = ((((uint)input[09] << 8) | input[08]) / 32767.5f) - 1.0f;
    float z = ((((uint)input[11] << 8) | input[10]) / 32767.5f) - 1.0f;
    orientation = new(x, y, z, w);
    _lastTime = time;

    string data = "EKF 0 ["
          + w.ToString("F3") + " "
          + x.ToString("F3") + " "
          + y.ToString("F3") + " "
          + z.ToString("F3") + "]";

    Dispatcher.InvokeAsync(() => {
      Timestamp.Text = timestamp;
      Orientation.Text = data;
    }, DispatcherPriority.Render);
  }

  // OpenGL
  void OnLoaded(object sender, RoutedEventArgs e) {
    _shader = new();
    _axis3D = new();

    GL.ClearColor(Color.FromArgb(0x3C, 0x41, 0x42));
  }
  void OnRender(TimeSpan obj) {
    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

    var model = Matrix4.CreateFromQuaternion(orientation);
    var view = Matrix4.LookAt(new Vector3(2, 2, 2), Vector3.Zero, Vector3.UnitY);
    var proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90), (float)(Width / Height), 0.1f, 100f);
    Matrix4 mvp = model * toENU * view * proj;

    _shader?.Use();
    _shader?.SetUniform("uMVP", ref mvp);

    _axis3D?.Draw();
  }
}
