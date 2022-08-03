using SFML.Graphics;
using SFML.System;
using SFML.Utils;

namespace Candle
{
    /// <summary>
    /// LightSource that emits light in a single direction.
    /// </summary>
    /// <remarks>
    /// A DirectedLight is defined, mainly, by the direction of the rays, 
    /// the position of the source, the beam width and the range of the light.
    /// You can manipulate the first two changing the rotation and
    /// position as you would with any Transformable. 
    /// </remarks>
    public class DirectedLight : LightSource
    {
        /// <summary>
        /// The width specifies the maximum distance allowed from the 
        /// center of the segment to cast a ray, along a segment normal 
        /// to the light direction.
        /// </summary>
        public float BeamWidth { get; set; }

        /// <summary>
        /// Constructs a new instance of the DirectedLight.
        /// </summary>
        public DirectedLight()
        {
            _polygon.PrimitiveType = PrimitiveType.Quads;
            _polygon.Resize(2);
            BeamWidth = 10F;
        }

        /// <summary>
        /// Construct directed light from another directed light
        /// </summary>
        /// <param name="copy">DirectedLight to copy</param>
        public DirectedLight(DirectedLight copy) : this()
        {
            BeamWidth = copy.BeamWidth;
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

        protected override void ResetColor()
        {
            int quads = (int)(_polygon.VertexCount / 4);

            for (int i = 0; i < quads; i++)
            {
                uint p1 = (uint)(i * 4);
                uint p2 = p1 + 1;
                uint p3 = p1 + 2;
                uint p4 = p1 + 3;
                Vector2f r1 = _polygon[p1].Position;
                Vector2f r2 = _polygon[p2].Position;
                Vector2f r3 = _polygon[p4].Position;
                Vector2f r4 = _polygon[p3].Position;

                float dr1 = 1F - (Fade ? 1 : 0) * ((r2 - r1).Magnitude() / Range);
                float dr2 = 1F - (Fade ? 1 : 0) * ((r4 - r3).Magnitude() / Range);

                _polygon.ModifyVertex(p1, (ref Vertex v) =>
                {
                    v.Color = Color;
                });

                _polygon.ModifyVertex(p2, (ref Vertex v) =>
                {
                    v.Color = Color;
                    v.Color.A = (byte)(Color.A * dr1);
                });

                _polygon.ModifyVertex(p3, (ref Vertex v) =>
                {
                    v.Color = Color;
                    v.Color.A = (byte)(Color.A * dr2);
                });

                _polygon.ModifyVertex(p4, (ref Vertex v) =>
                {
                    v.Color = Color;
                });
            }
        }

        public override void CastLight(List<Line> edges)
        {
            Transform trm = Transform.Clone();
            Transform trmInv = trm.GetInverse();

            float widthHalf = BeamWidth / 2F;
            FloatRect baseBeam = new FloatRect(0F, -widthHalf, Range, BeamWidth);

            Vector2f lim1o = trm.TransformPoint(0F, -widthHalf);
            Vector2f lim1d = trm.TransformPoint(Range, -widthHalf);
            Vector2f lim2o = trm.TransformPoint(0F, widthHalf);
            Vector2f lim2d = trm.TransformPoint(Range, widthHalf);

            float off = 0.01F / (lim2o - lim1o).Magnitude();
            Vector2f lightDir = lim1d - lim1o;

            Line lim1 = new Line(lim1o, lim1d);
            Line lim2 = new Line(lim2o, lim2d);
            Line raySrc = new Line(lim1o, lim2o);
            Line rayRng = new Line(lim1d, lim2d);

            PriorityQueue<Line, float> rays = new PriorityQueue<Line, float>();
            rays.Enqueue(new Line(lim1), 0F);
            rays.Enqueue(new Line(lim2), 1F);

            foreach (var seg in edges)
            {
                if 
                (
                    rayRng.Intersection(seg, out float tRng, out float tSeg)
                    && tRng <= 1F
                    && tRng >= 0F
                    && tSeg <= 1F
                    && tSeg >= 0F
                )
                {
                    rays.Enqueue(new Line(raySrc.Point(tRng), raySrc.Point(tRng) + lightDir), tRng);
                }

                float t;
                Vector2f end = seg.Origin;
                Vector2f transformedEnd = trmInv.TransformPoint(end);
                if (baseBeam.Contains(transformedEnd.X, transformedEnd.Y))
                {
                    raySrc.Intersection(new Line(end, end - lightDir), out t);
                    rays.Enqueue(new Line(raySrc.Point(t - off), raySrc.Point(t - off) + lightDir), t - off);
                    rays.Enqueue(new Line(raySrc.Point(t),       raySrc.Point(t)       + lightDir), t);
                    rays.Enqueue(new Line(raySrc.Point(t + off), raySrc.Point(t + off) + lightDir), t + off);
                }

                end = seg.Point(1F);
                transformedEnd = trmInv.TransformPoint(end);
                if (baseBeam.Contains(transformedEnd.X, transformedEnd.Y))
                {
                    raySrc.Intersection(new Line(end, end - lightDir), out t);
                    rays.Enqueue(new Line(raySrc.Point(t - off), raySrc.Point(t - off) + lightDir), t - off);
                    rays.Enqueue(new Line(raySrc.Point(t),       raySrc.Point(t)       + lightDir), t);
                    rays.Enqueue(new Line(raySrc.Point(t + off), raySrc.Point(t + off) + lightDir), t + off);
                }
            }

            List<Vector2f> points = new List<Vector2f>(rays.Count * 2);

            while (rays.Count > 0)
            {
                Line r = rays.Dequeue();

                points.Add(trmInv.TransformPoint(r.Origin));
                points.Add(trmInv.TransformPoint(Line.CastRay(edges, r, Range)));
            }

            if (points.Count > 0)
            {
                int quads = points.Count / 2 - 1; // a quad between every two rays
                _polygon.Resize((uint)(quads * 4));

                for (int i = 0; i < quads; i++)
                {
                    uint p1 = (uint)(i * 4); int r1 = i * 2;
                    uint p2 = p1 + 1;        int r2 = r1 + 1;
                    uint p3 = p1 + 2;        int r3 = r1 + 2;
                    uint p4 = p1 + 3;        int r4 = r1 + 3;

                    float dr1 = 1F - (Fade ? 1 : 0) * ((points[r2] - points[r1]).Magnitude() / Range);
                    float dr2 = 1F - (Fade ? 1 : 0) * ((points[r4] - points[r3]).Magnitude() / Range);

                    _polygon.ModifyVertex(p1, (ref Vertex v) =>
                    {
                        v.Position = points[r1];
                        v.Color = Color;
                    });

                    _polygon.ModifyVertex(p2, (ref Vertex v) =>
                    {
                        v.Position = points[r2];
                        v.Color = Color;
                        v.Color.A = (byte)(Color.A * dr1);
                    });

                    _polygon.ModifyVertex(p3, (ref Vertex v) =>
                    {
                        v.Position = points[r4];
                        v.Color = Color;
                        v.Color.A = (byte)(Color.A * dr2);
                    });

                    _polygon.ModifyVertex(p4, (ref Vertex v) =>
                    {
                        v.Position = points[r3];
                        v.Color = Color;
                    });
                }
            }
        }

        public override void Draw(RenderTarget target, RenderStates states)
        {
            states.Transform.Combine(Transform);

            if (states.BlendMode == BlendMode.Alpha)
                states.BlendMode = BlendMode.Add;

            target.Draw(_polygon, states);
        }
    }
}
