using xTile.Dimensions;

namespace xTile.Layers;

public class LayerEventArgs
{
	private Layer m_layer;

	private Rectangle m_viewport;

	public Layer Layer => m_layer;

	public Rectangle Viewport => m_viewport;

	public LayerEventArgs(Layer layer, Rectangle viewport)
	{
		m_layer = layer;
		m_viewport = viewport;
	}
}
