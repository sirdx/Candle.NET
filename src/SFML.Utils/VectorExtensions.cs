using SFML.System;

namespace SFML.Utils
{
    /// <summary>
    /// Utility extension functions for 2D vectors.
    /// </summary>
    public static class VectorExtensions
    {
        /// <summary>
        /// Get the magnitude of the 2D vector.
        /// </summary>
        /// <returns>Magnitude of the vector.</returns>
        public static float Magnitude(this Vector2f vector)
        {
            return MathF.Sqrt(vector.MagnitudeSqr());
        }

        /// <summary>
        /// Get the squared magnitude of the 2D vector.
        /// </summary>
        /// <returns>Squared magnitude of the vector.</returns>
        public static float MagnitudeSqr(this Vector2f vector)
        {
            return vector.X * vector.X + vector.Y * vector.Y;
        }

        /// <summary>
        /// Get the normalized version of the 2D vector.
        /// </summary>
        /// <returns>Normalized 2D vector.</returns>
        public static Vector2f Normalize(this Vector2f vector)
        {
            float m = vector.Magnitude();
            return new Vector2f(vector.X / m, vector.Y / m);
        }

        /// <summary>
        /// Get the dot product of two 2D vectors.
        /// </summary>
        /// <param name="other">The other vector.</param>
        /// <returns>Dot product of two 2D vectors.</returns>
        public static float Dot(this Vector2f vector, Vector2f other)
        {
            return vector.X * other.X + vector.Y * other.Y;
        }

        /// <summary>
        /// Get the angle between two 2D vectors.
        /// </summary>
        /// <param name="other">The other vector.</param>
        /// <returns>Angle between two 2D vectors.</returns>
        public static float Angle(this Vector2f vector, Vector2f other)
        {
            return MathF.Acos(vector.Dot(other) / (vector.Magnitude() * other.Magnitude())) * 180F / MathF.PI;
        }

        /// <summary>
        /// Get the angle of the 2D vector with the X axis.
        /// </summary>
        /// <returns>Angle of the 2D vector with the X axis.</returns>
        public static float Angle(this Vector2f vector)
        {
            return (MathF.Atan2(vector.Y, vector.X) * 180F / MathF.PI + 360F) % 360F;
        }
    }
}
