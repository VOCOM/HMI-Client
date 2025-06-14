using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using BluetoothLE.Services;
using BluetoothLE.Structs;
using OpenTK.Graphics.OpenGL4;
using Renderer.Entities;
using Renderer.Interfaces;
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

      if(rightHand is null) continue;
      rightHand[i].Position = new(
        data.Position[i].X,
        data.Position[i].Y,
        data.Position[i].Z);
      rightHand[i].Orientation = new(
        data.Orientation[i].X,
        data.Orientation[i].Y,
        data.Orientation[i].Z,
        data.Orientation[i].W);
    }

    //TabItem p = SensorTabs.First(t => t.Name == data.Name.Replace(" ", null));
    //p.Content = displayText;
  }

  // OpenGL
  Camera? camera;
  RightHand? rightHand;
  readonly Pose[] poses = new Pose[6];

  void OnLoaded(object sender, RoutedEventArgs e) {
    float scale = (float)(Math.Max(Width, Height) / 3f);
    camera = new() {
      Position = new(0, 0, 1),
      Offset = new(0f, 0.7f, 0f),
      Width = (float)(Width / scale),
      Height = (float)(Height / scale),
      TogglePerspective = false
    };

    rightHand = new();
    IRenderable.Spawn(rightHand);

    // Background [Charcoal Gray]
    GL.ClearColor(Color.FromArgb(0x3C, 0x41, 0x42));
  }
  void OnRender(TimeSpan obj) {
    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

    camera?.Render();
  }
}
