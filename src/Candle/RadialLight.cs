using SFML.Graphics;
using SFML.System;
using SFML.Utils;

namespace Candle
{
    public class RadialLight : LightSource
    {
        private float _beamAngle;
        private const float BASE_RADIUS = 400F;

        private static int _instanceCount = 0;
        private static RenderTexture _lightTextureFade;
        private static RenderTexture _lightTexturePlain;
        private static bool _lightTexturesReady = false;

        /// <summary>
        /// The range for which rays may be casted.
        /// </summary>
        /// <remarks>
        /// The angle shall be specified in degrees. The angle in which the rays 
        /// will be casted will be [R - angle/2, R + angle/2], 
        /// where R is the rotation of the object.
        /// The default angle is 360.
        /// </remarks>
        public float BeamAngle
        {
            get => _beamAngle;
            set => _beamAngle = Module360(value);
        }

        /// <summary>
        /// Get the local bounding rectangle of the light.
        /// </summary>
        public FloatRect LocalBounds
        {
            get => new FloatRect(0F, 0F, BASE_RADIUS * 2F, BASE_RADIUS * 2F);
        }

        /// <summary>
        /// Get the global bounding rectangle of the light.
        /// </summary>
        public FloatRect GlobalBounds
        {
            get
            {
                float scaledRange = Range / BASE_RADIUS;
                Transform trm = Transform.Clone();
                trm.Scale(scaledRange, scaledRange, BASE_RADIUS, BASE_RADIUS);
                return trm.TransformRect(LocalBounds);
            }
        }

        /// <summary>
        /// Constructs a new instance of the RadialLight.
        /// </summary>
        public RadialLight()
        {
            if (!_lightTexturesReady)
            {
                // The first time we create a RadialLight, we must create the textures
                InitializeTextures();
                _lightTexturesReady = true;
            }

            _polygon.PrimitiveType = PrimitiveType.TriangleFan;
            _polygon.Resize(6);

            _polygon.ModifyVertex(0, (ref Vertex v) => v.Position = v.TexCoords = new Vector2f(BASE_RADIUS + 1F, BASE_RADIUS + 1F));
            _polygon.ModifyVertex(1, (ref Vertex v) => v.Position = v.TexCoords = new Vector2f());
            _polygon.ModifyVertex(2, (ref Vertex v) => v.Position = v.TexCoords = new Vector2f(BASE_RADIUS * 2F + 2F, 0F));
            _polygon.ModifyVertex(3, (ref Vertex v) => v.Position = v.TexCoords = new Vector2f(BASE_RADIUS * 2F + 2F, BASE_RADIUS * 2F + 2F));
            _polygon.ModifyVertex(4, (ref Vertex v) => v.Position = v.TexCoords = new Vector2f(0F, BASE_RADIUS * 2F + 2F));
            _polygon.ModifyVertex(5, (ref Vertex v) => v.Position = v.TexCoords = new Vector2f());

            Origin = new Vector2f(BASE_RADIUS, BASE_RADIUS);
            Range = 1F;
            BeamAngle = 360F;

            _instanceCount++;
        }

        /// <summary>
        /// Construct radial light from another radial light
        /// </summary>
        /// <param name="copy">RadialLight to copy</param>
        public RadialLight(RadialLight copy)
        {
            BeamAngle = copy.BeamAngle;
            _polygon = new VertexArray(copy._polygon);
            Fade = copy.Fade;
            Range = copy.Range;
            Intensity = copy.Intensity;
            _intensity = copy._intensity;
            Color = new Color(copy.Color);
            Position = copy.Position;
            Rotation = copy.Rotation;
            Scale = copy.Scale;
            Origin = copy.Origin;
        }

        ~RadialLight()
        {
            _instanceCount--;

            // if (_instanceCount == 0 && _lightTextureFade != null && _lightTexturePlain != null)
            // {
            //     _lightTextureFade = null;
            //     _lightTexturePlain = null;
            //     _lightTexturesReady = false;
            // }
        }

        /// <summary>
        /// This function initializes the Texture used for the RadialLights.
        /// </summary>
        /// <remarks>
        /// This function is called the first time a RadialLight is created, 
        /// so the user shouldn't need to do it. Anyways, it could be 
        /// necessary to do it explicitly if you declare a RadialLight that, 
        /// for some reason, is static RadialLight and is not constructed in
        /// a normal order.
        /// </remarks>
        public static void InitializeTextures()
        {
            uint points = 100;

            _lightTextureFade = new RenderTexture((uint)(BASE_RADIUS * 2 + 2), (uint)(BASE_RADIUS * 2 + 2));
            _lightTexturePlain = new RenderTexture((uint)(BASE_RADIUS * 2 + 2), (uint)(BASE_RADIUS * 2 + 2));

            VertexArray lightShape = new VertexArray(PrimitiveType.TriangleFan, points + 2);
            float step = MathF.PI * 2F / points;
            lightShape.ModifyVertex(0, (ref Vertex v) => v.Position = new Vector2f(BASE_RADIUS + 1F, BASE_RADIUS + 1F));

            for (uint i = 1; i < points + 2; i++)
            {
                lightShape.ModifyVertex(i, (ref Vertex v) =>
                {
                    v.Position = new Vector2f
                    (
                        (MathF.Sin(step * i) + 1) * BASE_RADIUS + 1,
                        (MathF.Cos(step * i) + 1) * BASE_RADIUS + 1
                    );

                    v.Color.A = 0;
                });      
            }

            _lightTextureFade.Clear(Color.Transparent);
            _lightTextureFade.Draw(lightShape);
            _lightTextureFade.Display();
            _lightTextureFade.Smooth = true;

            lightShape.SetColor(Color.White);
            _lightTexturePlain.Clear(Color.Transparent);
            _lightTexturePlain.Draw(lightShape);
            _lightTexturePlain.Display();
            _lightTexturePlain.Smooth = true;
        }

        private static float Module360(float x)
        {
            x %= 360F;
            x += x < 0F ? 360F : 0F;
            return x;
        }

        protected override void ResetColor()
        {
            _polygon.SetColor(Color);
        }

        public override void CastLight(List<Line> edges)
        {
            float scaledRange = Range / BASE_RADIUS;
            Transform trm = Transform.Clone();
            trm.Scale(scaledRange, scaledRange, BASE_RADIUS, BASE_RADIUS);

            // 2: beam angle, 4: corners, 2: pnts/sgmnt, 3 rays/pnt
            List<Line> rays = new List<Line>(2 + edges.Count * 2 * 3);

            // Start casting
            float bl1 = Module360(Rotation - BeamAngle / 2F);
            float bl2 = Module360(Rotation + BeamAngle / 2F);
            bool beamAngleBigEnough = BeamAngle < 0.1F;
            Vector2f castPoint = Position;
            float off = 0.001F;

            bool angleInBeam(float a) => beamAngleBigEnough
                       || (bl1 < bl2 && a > bl1 && a < bl2)
                       || (bl1 > bl2 && (a > bl1 || a < bl2));

            for (float a = 45F; a < 360F; a += 90F)
            {
                if (beamAngleBigEnough || angleInBeam(a))
                    rays.Add(new Line(castPoint, a));
            }

            FloatRect lightBounds = GlobalBounds;
            foreach (var line in edges)
            {
                // Only cast a ray if the line is in range
                if (lightBounds.Intersects(line.GetGlobalBounds()))
                {
                    Line r1 = new Line(castPoint, line.Origin);
                    Line r2 = new Line(castPoint, line.Point(1F));
                    float a1 = r1.Direction.Angle();
                    float a2 = r2.Direction.Angle();

                    if (angleInBeam(a1))
                    {
                        rays.Add(r1);
                        rays.Add(new Line(castPoint, a1 - off));
                        rays.Add(new Line(castPoint, a1 + off));
                    }
                    if (angleInBeam(a2))
                    {
                        rays.Add(r2);
                        rays.Add(new Line(castPoint, a2 - off));
                        rays.Add(new Line(castPoint, a2 + off));
                    }
                }
            }

            if (bl1 > bl2)
            {
                rays.Sort((r1, r2) =>
                {
                    float _bl1 = bl1 - 0.1F;
                    float _bl2 = bl2 + 0.1F;
                    float a1 = r1.Direction.Angle();
                    float a2 = r2.Direction.Angle();
                    return (a1 >= _bl1 && a2 <= _bl2) || (a1 < a2 && (_bl1 <= a1 || a2 <= _bl2)) ? -1 : 1;
                });
            }
            else
            {
                rays.Sort((r1, r2) => r1.Direction.Angle().CompareTo(r2.Direction.Angle()));
            }

            if (!beamAngleBigEnough)
            {
                rays.Insert(0, new Line(castPoint, bl1));
                rays.Add(new Line(castPoint, bl2));
            }

            Transform trmInv = trm.GetInverse();

            // Keep only the ones within the area
            List<Vector2f> points = new List<Vector2f>(rays.Count);
            rays.ForEach(r => points.Add(trmInv.TransformPoint(Line.CastRay(edges, r, Range * Range))));
            _polygon.Resize((uint)(points.Count + 1 + (beamAngleBigEnough ? 1 : 0))); // + center and last
            _polygon.ModifyVertex(0, (ref Vertex v) =>
            {
                v.Color = Color;
                v.Position = v.TexCoords = trmInv.TransformPoint(castPoint);
            });

            for (int i = 0; i < points.Count; i++)
            {
                Vector2f p = points[i];
                _polygon.ModifyVertex((uint)i + 1, (ref Vertex v) =>
                {
                    v.Position = p;
                    v.TexCoords = p;
                    v.Color = Color;
                });
            }

            if (beamAngleBigEnough)
                _polygon[(uint)points.Count + 1] = _polygon[1];
        }

        public override void Draw(RenderTarget target, RenderStates states)
        {
            Transform trm = Transform.Clone();
            trm.Scale(Range / BASE_RADIUS, Range / BASE_RADIUS, BASE_RADIUS, BASE_RADIUS);

            states.Transform.Combine(trm);
            states.Texture = Fade ? _lightTextureFade.Texture : _lightTexturePlain.Texture;

            if (states.BlendMode == BlendMode.Alpha)
                states.BlendMode = BlendMode.Add;

            target.Draw(_polygon, states);
        }
    }
}
