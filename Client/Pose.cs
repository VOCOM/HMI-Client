using OpenTK.Mathematics;

namespace Client;

public struct Pose() {
  public Quaternion Orientation = Quaternion.Identity;
  public Vector3 Position = Vector3.Zero;
}
