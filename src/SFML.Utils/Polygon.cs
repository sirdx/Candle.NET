using SFML.Graphics;
using SFML.System;

namespace SFML.Utils
{
    /// <summary>
    /// Auxiliary struct to represent a polygon as vector of lines.
    /// Not meant to be used outside Candle.
    /// </summary>
    internal struct Polygon
    {
        private readonly List<Line> _lines = new List<Line>();

        public Polygon(Vector2f[] points)
        {
            Initialize(points);
        }

        public Polygon(FloatRect rect)
        {
            Initialize(rect);
        }

        public List<Line> GetLines()
        {
            return _lines;
        }

        public void Initialize(Vector2f[] points)
        {
            int n = points.Length;

            _lines.Clear();
            _lines.Capacity = n;

            for (int i = 1; i <= n; i++)
                _lines.Add(new Line(points[i - 1], points[i % n]));
        }

        public void Initialize(FloatRect rect)
        {
            Vector2f lt = new(rect.Left, rect.Top);
            Vector2f rt = new(rect.Width, rect.Top);
            Vector2f lb = new(rect.Left, rect.Height);
            Vector2f rb = new(rect.Width, rect.Height);

            _lines.Add(new Line(lt, rt));
            _lines.Add(new Line(rt, rb));
            _lines.Add(new Line(rb, lb));
            _lines.Add(new Line(lb, lt));
        }
    }
}
