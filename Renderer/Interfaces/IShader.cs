using OpenTK.Mathematics;

namespace Renderer.Interfaces;

// Renderable Entities
// #TODO: Abstract Renderable entities
public interface IShader {
  public void Use();
  public void SetUniform(string attributeName, ref Matrix4 matrix);
  public void Render(List<IRenderable> models);
}
