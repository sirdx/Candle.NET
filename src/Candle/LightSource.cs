using SFML.Graphics;
using SFML.Utils;

namespace Candle
{
    /// <summary>
    /// Abstract class for objects that emit light
    /// </summary>
    /// <remarks>
    /// LightSources use raycasting algorithms to compute the polygon
    /// illuminated by the light. The main difference between the
    /// implementations, RadialLight and DirectedLight, is whether
    /// the constant is the origin or the direction of the rays.
    /// <para/>
    /// LightSources manage their colour separating the alpha value from the RGB. 
    /// This is convenient to manipulate color of the light (interpreted as 
    /// the RGB value) and intensity (interpreted as the alpha value) separately.
    /// <para/>
    /// By default, they use a BlendMode.Add. This means that you can
    /// specify any other blend mode you want, except BlendMode.Alpha, 
    /// that will be changed to the additive mode.
    /// </remarks>
    public abstract class LightSource : Transformable, Drawable
    {
        protected Color _color;
        protected VertexArray _polygon;
        protected float _intensity; // Only for fog
        protected bool _fade;

        /// <summary>
        /// The range of the illuminated area.
        /// </summary>
        /// <remarks>
        /// The range of the light indicates the how far a light ray
        /// may hit from its origin.
        /// </remarks>
        public float Range { get; set; }

        /// <summary>
        /// The intensity of the light determines two things: 
        /// how much fog opacity it reduces when drawn in a LightingArea in 
        /// FOG mode, and how much presence its color has when drawn normally.
        /// </summary>
        /// <remarks>
        /// The default value is 1.
        /// New value should be between 0.0F and 1.0F.
        /// </remarks>
        public float Intensity
        {
            get => _color.A / 255F;
            set
            {
                _color.A = (byte)(255 * value);
                ResetColor();
            }
        }

        /// <summary>
        /// The light color.
        /// </summary>
        /// <remarks>
        /// The light color refers only to the RGB values.
        /// The default value is Color.White.
        /// When specifying a new value the alpha value is ignored.
        /// </remarks>
        public Color Color
        {
            get => new Color(_color.R, _color.G, _color.B, 255);
            set
            {
                _color = new Color(value.R, value.G, value.B, _color.A);
                ResetColor();
            }
        }

        /// <summary>
        /// When the fade flag is set, the light will lose intensity
        /// in the limits of its range. 
        /// Otherwise, the intensity will remain constant.
        /// </summary>
        /// <remarks>
        /// The default value is true.
        /// </remarks>
        public bool Fade
        {
            get => _fade;
            set
            {
                _fade = value;
                ResetColor();
            }
        }

        /// <summary>
        /// Constructs a new instance of the LightSource.
        /// </summary>
        protected LightSource()
        {
            _polygon = new VertexArray();
            Color = Color.White;
            Fade = true;
        }

        protected abstract void ResetColor();

        /// <summary>
        /// Modify the polygon of the illuminated area with 
        /// a raycasting algorithm.
        /// </summary>
        /// <remarks>
        /// The algorithm needs to know which edges to use to cast 
        /// shadows. They are specified within a range of two iterators 
        /// of a list of edges of type Line.
        /// </remarks>
        /// <param name="edges">The list of edges to take into account.</param>
        public abstract void CastLight(List<Line> edges);

        /// <summary>
        /// Draw the light source to the target.
        /// </summary>
        /// <param name="target">Render target to draw to.</param>
        /// <param name="states">Current render states.</param>
        public abstract void Draw(RenderTarget target, RenderStates states);
    }
}
