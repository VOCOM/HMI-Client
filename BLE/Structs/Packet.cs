using System.Numerics;

namespace BluetoothLE.Structs;

public struct Packet(string name, int size) {
  public uint Time;
  public string Name = name;
  public Vector3[] Position = new Vector3[size];
  public Quaternion[] Orientation = new Quaternion[size];
}
