using SFML.Graphics;
using SFML.System;

namespace SFML.Utils
{
    /// <summary>
    /// Represents a 2D Line defined by an origin point and a direction vector.
    /// </summary>
    public class Line
    { 
        /// <summary>
        /// Origin point of the line.
        /// </summary>
        public Vector2f Origin { get; set; }

        /// <summary>
        /// Direction vector (not necessarily normalized)
        /// </summary>
        public Vector2f Direction { get; set; }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="copy">Line to copy.</param>
        public Line(Line copy)
        {
            Origin = copy.Origin;
            Direction = copy.Direction;
        }

        /// <summary>
        /// Construct a line that passes through 
        /// <paramref name="pointA"/> and <paramref name="pointB"/>
        /// </summary>
        /// <remarks>
        /// The direction is interpreted as: <c>PointB - PointA</c>.
        /// </remarks>
        /// <param name="pointA">First point.</param>
        /// <param name="pointB">Second point.</param>
        public Line(Vector2f pointA, Vector2f pointB)
        {
            Origin = pointA;
            Direction = pointB - pointA;
        }

        /// <summary>
        /// Construct a line defined by a point and an angle.
        /// </summary>
        /// <remarks>
        /// The direction is interpreted as <c>Cos(Angle), Sin(Angle))</c>.
        /// </remarks>
        /// <param name="origin">Origin point.</param>
        /// <param name="angle">Angle defining the line.</param>
        public Line(Vector2f origin, float angle)
        {
            Origin = origin;

            float tau = MathF.PI * 2F;
            float ang = (angle * MathF.PI / 180F + MathF.PI) % tau;
            ang += ang < 0 ? tau : 0;
            ang -= MathF.PI;
            Direction = new Vector2f(MathF.Cos(ang), MathF.Sin(ang));
        }

        /// <summary>
        /// Get the global bounding rectangle of the line.
        /// </summary>
        /// <remarks>
        /// The returned rectangle is in global coordinates, which
        /// means that it takes into account the transformations 
        /// (translation, rotation, scale, ...) (see SFML).
        /// </remarks>
        /// <returns>Global bounding rectangle in float.</returns>
        public FloatRect GetGlobalBounds()
        {
            Vector2f pointA = Origin;
            Vector2f pointB = Direction + Origin;

            return new FloatRect
            (
                // Make sure that the rectangle begin from the upper left corner
                (pointA.X < pointB.Y) ? pointA.X : pointB.X,
                (pointA.Y < pointB.Y) ? pointA.Y : pointB.Y,
                // The +1 is here to avoid having a width of zero
                // (SFML doesn't like 0 in rect)
                MathF.Abs(Direction.X) + 1F,
                MathF.Abs(Direction.Y) + 1F
            );
        }

        /// <summary>
        /// Get the relative position of a point to the line.
        /// </summary>
        /// <remarks>
        /// If the point is to the right of the direction vector, the
        /// value returned is -1. If it is to the left, it is +1. 
        /// If the point belongs to the line, returns 0.
        /// </remarks>
        /// <param name="point">The point.</param>
        /// <returns>-1, 0 or 1.</returns>
        public int RelativePosition(Vector2f point)
        {
            float f = (point.X - Origin.X) / Direction.X - (point.Y - Origin.Y) / Direction.Y;
            return (0F < f ? 1 : 0) - (f < 0F ? 1 : 0);
        }

        /// <summary>
        /// Get the minimum distance of a point to the line.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>The minimum distance of a point to the line.</returns>
        public float Distance(Vector2f point)
        {
            float d;

            if (Direction.X == 0)
                d = MathF.Abs(point.X - Origin.X);
            else if (Direction.Y == 0)
                d = MathF.Abs(point.Y - Origin.Y);
            else
            {
                float A = 1F / Direction.X;
                float B = -1F / Direction.Y;
                float C = -B * Origin.Y - A * Origin.X;
                d = MathF.Abs(A * point.X + B * point.Y + C) / MathF.Sqrt(A * A + B * B);
            }

            return d;
        }

        /// <summary>
        /// Returns a boolean if there is intersection of this line to another.
        /// </summary>
        /// <param name="otherLine">Another line.</param>
        /// <returns>True, if there is an intersection.</returns>
        public bool Intersection(Line otherLine)
        {
            return Intersection(otherLine, out _, out _);
        }

        /// <summary>
        /// Returns the magnitude corresponding of the intersection of this line to another.
        /// </summary>
        /// <remarks>
        /// If there is an intersection, the output argument
        /// <paramref name="normA"/> contains the magnitude required 
        /// to get the intersection point from this line direction.
        /// </remarks>
        /// <param name="otherLine">Another line.</param>
        /// <param name="normA">The output magnitude.</param>
        /// <returns>True, if there is an intersection.</returns>
        public bool Intersection(Line otherLine, out float normA)
        {
            return Intersection(otherLine, out normA, out _);
        }

        /// <summary>
        /// Returns the magnitude corresponding of the intersection of this line to another.
        /// </summary>
        /// <remarks>
        /// If there is an intersection, the output argument <paramref name="normB"/>
        /// contains the magnitude required to get the intersection
        /// point from <paramref name="lineB"/> direction and <paramref name="normA"/>, 
        /// the magnitude required to get the intersection point from this line direction.
        /// </remarks>
        /// <param name="lineB">Another line.</param>
        /// <param name="normA">The output A magnitude.</param>
        /// <param name="normB">The output B magnitude.</param>
        /// <returns>True, if there is an intersection.</returns>
        public bool Intersection(Line lineB, out float normA, out float normB)
        {
            Vector2f lineAOrigin = Origin;
            Vector2f lineADirection = Direction;
            Vector2f lineBOrigin = lineB.Origin;
            Vector2f lineBDirection = lineB.Direction;

            // When the lines are parallel, we consider that there is not intersection.
            float lineAngle = lineADirection.Angle(lineBDirection);

            if ((lineAngle < 0.001F || lineAngle > 359.999F) || ((lineAngle < 180.001F) && (lineAngle > 179.999F)))
            {
                normA = 0F;
                normB = 0F;
                return false;
            }

            // Math resolving, you can find more information here: https://ncase.me/sight-and-light/
            if ((MathF.Abs(lineBDirection.Y) >= 0F) && (MathF.Abs(lineBDirection.X) < 0.001F) || (MathF.Abs(lineADirection.Y) < 0.001F) && (MathF.Abs(lineADirection.X) >= 0F))
            {
                normB = (lineADirection.X * (lineAOrigin.Y - lineBOrigin.Y) + lineADirection.Y * (lineBOrigin.X - lineAOrigin.X)) / (lineBDirection.Y * lineADirection.X - lineBDirection.X * lineADirection.Y);
                normA = (lineBOrigin.X + lineBDirection.X * normB - lineAOrigin.X) / lineADirection.X;
            }
            else
            {
                normA = (lineBDirection.X * (lineBOrigin.Y - lineAOrigin.Y) + lineBDirection.Y * (lineAOrigin.X - lineBOrigin.X)) / (lineADirection.Y * lineBDirection.X - lineADirection.X * lineBDirection.Y);
                normB = (lineAOrigin.X + lineADirection.X * normA - lineBOrigin.X) / lineBDirection.X;
            }

            // Make sure that there is actually an intersection
            return (normB > 0) && (normA > 0) && (normA < Direction.Magnitude());
        }

        /// <summary>
        /// Get a point of the line.
        /// </summary>
        /// <remarks>
        /// The point is obtained using this calculation: <c>Origin + param * Direction</c>.
        /// </remarks>
        /// <param name="param">Point offset.</param>
        /// <returns>A point of the line.</returns>
        public Vector2f Point(float param)
        {
            return Origin + param * Direction;
        }

        /// <summary>
        /// Cast a ray against a set of segments.
        /// </summary>
        /// <remarks>
        /// Use a line as a ray, casted from its origin point in its direction. 
        /// It is intersected with * a set of segments, represented as Lines too, 
        /// and the one closest to the cast point is returned.
        /// <para/>
        /// Segments are interpreted to be delimited by the <c>ray._origin</c> 
        /// and <c>ray.Point(1)</c>.
        /// </remarks>
        /// <param name="enumerable">Ray collection.</param>
        /// <param name="ray">The ray.</param>
        /// <param name="maxRange">
        /// Optional argument to indicate the max distance allowed
        /// for a ray to hit a segment.
        /// </param>
        /// <returns></returns>
        public static Vector2f CastRay(IEnumerable<Line> enumerable, Line ray, float maxRange = float.PositiveInfinity)
        {
            float minRange = maxRange;
            ray.Direction = ray.Direction.Normalize();

            foreach (var item in enumerable)
            {
                if
                (
                    item.Intersection(ray, out float tSeg, out float tRay)
                    && tRay <= minRange
                    && tRay >= 0F
                    && tSeg <= 1F
                    && tSeg >= 0F
                )
                {
                    minRange = tRay;
                }
            }

            return ray.Point(minRange);
        }
    }
}
