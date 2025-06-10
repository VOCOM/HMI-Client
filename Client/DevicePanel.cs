using System.Windows;
using System.Windows.Controls;
using Control = System.Windows.Controls.Control;

namespace Client;

public class DevicePanel: Control {
  static DevicePanel() => DefaultStyleKeyProperty.OverrideMetadata(typeof(DevicePanel), new FrameworkPropertyMetadata(typeof(DevicePanel)));

  public string SensorName {
    get => (string)GetValue(SensorNameProperty);
    set => SetValue(SensorNameProperty, value);
  }
  public ItemCollection Tabs {
    get => (ItemCollection)GetValue(TabsProperty);
    set => SetValue(TabsProperty, value);
  }
  public object ItemsSource {
    get => GetValue(ItemsSourceProperty);
    set => SetValue(ItemsSourceProperty, value);
  }
  public static readonly DependencyProperty SensorNameProperty =
      DependencyProperty.Register("SensorName", typeof(string), typeof(DevicePanel));
  public static readonly DependencyProperty TabsProperty =
      DependencyProperty.Register("Tabs", typeof(ItemCollection), typeof(DevicePanel));
  public static readonly DependencyProperty ItemsSourceProperty =
      DependencyProperty.Register("ItemsSource", typeof(object), typeof(DevicePanel));

}
