using SFML.Graphics;
using SFML.System;
using SFML.Utils;

namespace Candle
{
    /// <summary>
    /// Represents an object to manage ambient light and fog.
    /// </summary>
    /// <remarks>
    /// A LightingArea is a wrapper class to a RenderTexture that provides
    /// the required functions to use it as a mask for the light or a layer 
    /// of extra lighting.
    /// This behaviour is specified through the operation mode of the area.
    /// <para/>
    /// It can be used with a plain color or with another Texture as base.
    /// If this is the case, such texture must exist and be managed externally
    /// during the life of the LightingArea. All changes made to the color,
    /// opacity or texture of the area require a call to Clear() and
    /// Display() to make effect.
    /// <para/>
    /// As the RenderTexture may be a heavy resource to be creating
    /// repeteadly, there are only two moments in which you are able to do so.
    /// The first one and most common is upon construction, where you can
    /// specify the size of the area, which will also be the size of the
    /// RenderTexture. The second one is upon the assignment of a texture
    /// with SetAreaTexture(), on which the area is created again to match
    /// the size of the new texture.
    /// <para/>
    /// There are two things to note about this:
    /// 1. To change the size of a LightingArea that has already been
    /// constructed, without changing the base texture, you have to scale it 
    /// as you would do with any other Transformable.
    /// 2. If you change the texture of the area, its size might also be
    /// modified. So, if you want to change the texture and the size, 
    /// you must change the texture first and scale it after that.
    /// </remarks>
    public class LightingArea : Transformable, Drawable
    {
        private static readonly BlendMode _substractAlpha = new BlendMode
        (
            BlendMode.Factor.Zero,              // Color Source
            BlendMode.Factor.One,               // Color Destination
            BlendMode.Equation.Add,             // Color Equation
            BlendMode.Factor.Zero,              // Alpha Source
            BlendMode.Factor.OneMinusSrcAlpha,  // Alpha Destination
            BlendMode.Equation.Add              // Alpha Equation
        );

        private Texture _baseTexture;
        private IntRect _baseTextureRect;
        private VertexArray _baseTextureQuad;
        private RenderTexture _renderTexture;
        private VertexArray _areaQuad;
        private Color _color;
        private float _opacity;
        private Vector2f _size;

        /// <summary>
        /// Operation modes for a LightingArea.
        /// </summary>
        public enum Mode
        { 
            /// <summary>
            /// In this mode, the area behaves like a mask through which it is
            /// only possible to see by drawing light on it.
            /// </summary>
            Fog,
            /// <summary>
            /// Use the area as an extra layer of light.
            /// </summary>
            Ambient
        }

        /// <summary>
        /// The lighting mode.
        /// </summary>
        public Mode AreaMode { get; set; }

        /// <summary>
        /// Color of the fog/light. 
        /// </summary>
        /// <remarks>
        /// If the area has no texture, the plain color is used in
        /// the next calls to Clear(). Otherwise, the texture is multiplied
        /// by the color. In both cases, the alpha value of the color is
        /// preserved.
        /// The default color is Color.White.
        /// </remarks>
        public Color AreaColor
        {
            get => _color;
            set
            {
                _color = value;
                _baseTextureQuad.SetColor(GetActualColor());
            }
        }

        /// <summary>
        /// The opacity of the fog/light.
        /// </summary>
        /// <remarks>
        /// The opacity is a value multiplied to the alpha value before
        /// any use of the color, to ease the separate manipulation.
        /// </remarks>
        public float Opacity
        {
            get => _opacity;
            set
            {
                _opacity = value;
                _baseTextureQuad.SetColor(GetActualColor());
            }
        }

        /// <summary>
        /// Get the texture of the fog/light.
        /// </summary>
        public Texture AreaTexture
        {
            get => _baseTexture;
        }

        /// <summary>
        /// The rectangle of the used sub-section of the texture.
        /// </summary>
        /// <remarks>
        /// Note that the setter won't adjust the size of the area
        /// to fit the new rectangle.
        /// </remarks>
        public IntRect TextureRect
        {
            get => _baseTextureRect;
            set
            {
                _baseTextureQuad.ModifyVertex(0, (ref Vertex v) => v.TexCoords = new Vector2f(value.Left, value.Top));
                _baseTextureQuad.ModifyVertex(1, (ref Vertex v) => v.TexCoords = new Vector2f(value.Left + value.Width, value.Top));
                _baseTextureQuad.ModifyVertex(2, (ref Vertex v) => v.TexCoords = new Vector2f(value.Left + value.Width, value.Top + value.Height));
                _baseTextureQuad.ModifyVertex(3, (ref Vertex v) => v.TexCoords = new Vector2f(value.Left, value.Top + value.Height));
            }
        }

        /// <summary>
        /// Get the local bounding rectangle of the area.
        /// </summary>
        /// <remarks>
        /// The rectangle returned bounds the area before any 
        /// transformations.
        /// </remarks>
        public FloatRect LocalBounds
        {
            get => _areaQuad.Bounds;
        }

        /// <summary>
        /// Get the global bounding rectangle of the area.
        /// </summary>
        /// <remarks>
        /// The rectangle returned bounds the area with the
        /// transformation already applied.
        /// </remarks>
        public FloatRect GlobalBounds
        {
            get => Transform.TransformRect(_areaQuad.Bounds);
        }

        private LightingArea()
        {
            _baseTextureQuad = new VertexArray(PrimitiveType.Quads, 4);
            _areaQuad = new VertexArray(PrimitiveType.Quads, 4);
            AreaColor = Color.White;
        }

        /// <summary>
        /// Constructs a LightingArea with plain color and specifies
        /// the initial position and the size of the created RenderTexture.
        /// </summary>
        /// <param name="mode">The area mode.</param>
        /// <param name="position">The area position.</param>
        /// <param name="size">The area size.</param>
        public LightingArea(Mode mode, Vector2f position, Vector2u size) : this()
        {
            AreaMode = mode;
            _baseTexture = null;
            InitializeRenderTexture(size);
            Position = position;
            Opacity = 1F;
        }

        /// <summary>
        /// Constructs a LightArea from a texture, in position (0, 0).
        /// As an optional parameter, you can pass the rectangle of the texture
        /// that delimits the subsection of the texture to use.
        /// </summary>
        /// <param name="mode">The area mode.</param>
        /// <param name="texture">The area texture.</param>
        /// <param name="rect">Subsection of the texture to use.</param>
        public LightingArea(Mode mode, Texture texture, IntRect rect = new IntRect()) : this()
        {           
            AreaMode = mode;
            SetAreaTexture(texture, rect);
            Opacity = 1F;
        }

        private Color GetActualColor()
        {
            Color color = new Color(AreaColor);
            color.A = (byte)(color.A * Opacity);
            return color;
        }

        private void InitializeRenderTexture(Vector2u size)
        {
            _renderTexture = new RenderTexture(size.X, size.Y);
            _renderTexture.Smooth = true;

            Vector2f[] vectors =
            {
                new Vector2f(),
                new Vector2f(size.X, 0F),
                new Vector2f(size.X, size.Y),
                new Vector2f(0F, size.Y),
            };

            for (uint i = 0; i < vectors.Length; i++)
            {
                _baseTextureQuad.ModifyVertex(i, (ref Vertex v) => v.Position = vectors[i]);
                _areaQuad.ModifyVertex(i, (ref Vertex v) => { v.Position = v.TexCoords = vectors[i]; v.Color = Color.White; });
            }
        }

        /// <summary>
        /// Set the texture of the fog/light.
        /// </summary>
        /// <param name="texture">The new texture. Pass a null to just unset the texture.</param>
        /// <param name="rect">Optional rectangle to call SetTextureRect(). If none is specified, the whole texture is used.</param>
        public void SetAreaTexture(Texture texture, IntRect rect = new IntRect())
        {
            _baseTexture = texture;

            if (rect.Width == 0 && rect.Height == 0 && texture != null)
            {
                rect.Width = (int)texture.Size.X;
                rect.Height = (int)texture.Size.Y;
            }

            InitializeRenderTexture(new Vector2u((uint)rect.Width, (uint)rect.Height));
            TextureRect = rect;
        }

        /// <summary>
        /// Updates and restores the color and the texture.
        /// </summary>
        /// <remarks>
        /// In Fog mode, it restores the covered areas.
        /// </remarks>
        public void Clear()
        {
            if (_baseTexture == null)
            {
                _renderTexture.Clear(GetActualColor());
                return;
            }

            _renderTexture.Clear(Color.Transparent);
            _renderTexture.Draw(_baseTextureQuad, new RenderStates(_baseTexture));
        }

        public void Draw(RenderTarget target, RenderStates states)
        {
            if (Opacity > 0F)
            {
                if (AreaMode == Mode.Ambient)
                    states.BlendMode = BlendMode.Add;

                states.Transform.Combine(Transform);
                states.Texture = _renderTexture.Texture;
                target.Draw(_areaQuad, states);
            }
        }

        /// <summary>
        /// In Fog mode, makes visible the area illuminated by the light.
        /// </summary>
        /// <remarks>
        /// In Fog mode with opacity greater than zero, this function.
        /// is necessary to keep the lighting coherent. 
        /// In Ambient mode, this function has no effect.
        /// </remarks>
        /// <param name="light">The light to draw.</param>
        public void Draw(LightSource light)
        {
            if (Opacity > 0F && AreaMode == Mode.Fog)
            {
                RenderStates fogRenderStates = RenderStates.Default;
                fogRenderStates.BlendMode = _substractAlpha;
                fogRenderStates.Transform.Combine(Transform.GetInverse());
                _renderTexture.Draw(light, fogRenderStates);
            }
        }

        /// <summary>
        /// Calls display on the RenderTexture.
        /// </summary>
        /// <remarks>
        /// Updates the changes made since the last call to Clear().
        /// </remarks>
        public void Display()
        {
            _renderTexture.Display();
        }
    }
}
