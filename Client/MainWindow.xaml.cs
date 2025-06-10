using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using BluetoothLE.Services;
using BluetoothLE.Structs;
using Client.Shaders;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Renderer.Entities;
using Application = System.Windows.Application;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Drawing.Point;

namespace Client;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow: Window {
  public ObservableCollection<string> DeviceList { get; } = [];
  public ObservableCollection<string> ServiceList { get; } = [];
  public List<DevicePanel> DevicePanels { get; } = [];
  public List<ObservableCollection<TabItem>> SensorTabs { get; } = [];

  public MainWindow() {
    InitializeComponent();
    DataContext = this;

    TitleBar.MouseLeftButtonDown += OnMouseLeftButtonDown;
    TitleBar.MouseLeftButtonUp += OnMouseRightButtonDown;
    MouseMove += OnMouseMove;

    DeviceNames.ItemsSource = DeviceList;

    _bleService = new();
    _bleService.DeviceConnected += OnDeviceConnected;
    _bleService.DeviceDisconnected += OnDeviceDisconnected;
    _bleService.DataReceived += OnDataReceived;
    _bleService.StartScan();

    Viewport.Loaded += OnLoaded;
    Viewport.Render += OnRender;
    Viewport.Start();
  }

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

  // BLE
  readonly Scanner _bleService;

  void OnDeviceConnected(string name) {
    Debug.WriteLine($"{name} Connected.");

    Dispatcher.Invoke(() => {
      SensorTabs.Add([]);
      DevicePanel panel = new() {
        SensorName = name,
        ItemsSource = SensorTabs[^1]
      };

      // #TODO: Add Sensor Count Request
      for(int i = 0; i < 6; i++) {
        TabItem sensor = new() {
          Header = $"S{i + 1}"
        };
        SensorTabs[^1].Add(sensor);
      }

      DevicePanels.Add(panel);
      Panels.Children.Add(panel);
      DeviceList.Add(name);
    }, DispatcherPriority.Render);
  }
  void OnDeviceDisconnected(string name) {
    Debug.WriteLine($"{name} Disconnected.");

    Dispatcher.Invoke(() => {
      DevicePanel p = DevicePanels.First(p => p.SensorName == name);
      int i = DevicePanels.IndexOf(p);

      Panels.Children.Remove(p);
      DevicePanels.RemoveAt(i);
      SensorTabs.RemoveAt(i);
      DeviceList.Remove(name);
    }, DispatcherPriority.Render);
  }
  void OnDataReceived(Packet data) {
    DevicePanel p = Dispatcher.Invoke(() => p = DevicePanels.First(p => p.SensorName == data.Name));
    int idx = DevicePanels.IndexOf(p);

    for(int i = 0; i < data.Orientation.Length; i++) {
      poses[i].Position = new(
        data.Position[i].X,
        data.Position[i].Y,
        data.Position[i].Z);
      poses[i].Orientation = new(
        data.Orientation[i].X,
        data.Orientation[i].Y,
        data.Orientation[i].Z,
        data.Orientation[i].W);

      string displayText =
        $"Position\n" +
        $"X: {data.Position[i].X:0.000}\n" +
        $"Y: {data.Position[i].Y:0.000}\n" +
        $"Z: {data.Position[i].Z:0.000}\n" +
        $"Orientation\n" +
        $"W: {data.Orientation[i].W:0.000}\n" +
        $"X: {data.Orientation[i].X:0.000}\n" +
        $"Y: {data.Orientation[i].Y:0.000}\n" +
        $"Z: {data.Orientation[i].Z:0.000}\n";

      Dispatcher.Invoke(() => SensorTabs[idx][i].Content = displayText);
    }

    //TabItem p = SensorTabs.First(t => t.Name == data.Name.Replace(" ", null));
    //p.Content = displayText;
  }

  // OpenGL
  Shader? _shader;
  Axis3D? _axis3D;
  readonly Pose[] poses = new Pose[6];
  readonly Matrix4 ENU2GL = new(Vector4.UnitX, Vector4.UnitZ, Vector4.UnitY, Vector4.UnitW);

  void OnLoaded(object sender, RoutedEventArgs e) {
    _shader = new();
    _axis3D = new(0.5f);

    for(int i = 0; i < poses.Length; i++) poses[i].Orientation = Quaternion.Identity;

    poses[0].Position = new(0, 0, 0); // Palm
    poses[1].Position = new(0, 0, 0); // Thumb
    poses[2].Position = new(0, 0, 0); // Index
    poses[3].Position = new(0, 0, 0); // Middle
    poses[4].Position = new(0, 0, 0); // Ring
    poses[5].Position = new(0, 0, 0); // Little

    GL.ClearColor(Color.FromArgb(0x3C, 0x41, 0x42));
  }
  void OnRender(TimeSpan obj) {
    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

    Matrix4 model
      = Matrix4.CreateFromQuaternion(poses[0].Orientation)
      * ENU2GL
      * Matrix4.CreateTranslation(poses[0].Position);
    var view = Matrix4.LookAt(new Vector3(1, 1, 1), Vector3.Zero, Vector3.UnitY);
    var proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90), (float)(Width / Height), 0.1f, 100f);
    Matrix4 mvp = model * view * proj;

    _shader?.Use();
    _shader?.SetUniform("uMVP", ref mvp);

    _axis3D?.Draw();
  }
}
