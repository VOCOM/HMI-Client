using System.Numerics;

namespace Renderer.Structs;

struct Vertex(Vector3 position, Vector3 color) {
  public Vector3 Position = position;
  public Vector3 Color = color;
}
