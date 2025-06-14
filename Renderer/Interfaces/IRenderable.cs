using OpenTK.Mathematics;

namespace Renderer.Interfaces;

public interface IRenderable {
  // Static Methods
  public static void Spawn(IRenderable instance) => Entities.Add(instance);

  public static readonly List<IRenderable> Entities = [];
  public static readonly Matrix4 ENU2GL = new(Vector4.UnitX, Vector4.UnitZ, Vector4.UnitY, Vector4.UnitW);

  // Instance Methods
  public void Draw();

  public Vector3 Position { get; set; }
  public Quaternion Orientation { get; set; }
  public Matrix4 Matrix { get; }
}
