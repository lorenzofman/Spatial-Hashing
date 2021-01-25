using UnityEngine;

public class PaintedNode : MonoBehaviour
{
    private Renderer rendererComponent;
    private MaterialPropertyBlock block;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    private void Start()
    {
        rendererComponent = GetComponentInChildren<Renderer>();
        block = new MaterialPropertyBlock();
    }

    public void Paint(Color color)
    {
        block.SetColor(BaseColor, color);

        rendererComponent.SetPropertyBlock(block);
    }
}