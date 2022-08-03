using SFML.Graphics;
using SFML.System;

namespace SFML.Utils
{
    public static class VertexArrayExtensions
    {
        /// <summary>
        /// Encapsulates a method that has the Vertex reference as a parameter
        /// and does not return a value.
        /// </summary>
        /// <param name="v">The vertex reference.</param>
        public delegate void VertexAction(ref Vertex v);

        /// <summary>
        /// Performs the specified action on each element of the VertexArray
        /// </summary>
        /// <remarks>
        /// This method uses the ModifyVertex extension method.
        /// </remarks>
        /// <param name="action">The VertexAction delegate to perform on each vertex.</param>
        public static void ForEach(this VertexArray va, VertexAction action)
        {
            for (uint i = 0; i < va.VertexCount; i++)
                va.ModifyVertex(i, action);
        }

        /// <summary>
        /// Performs the specified action on the specified vertex.
        /// </summary>
        /// <remarks>
        /// SFML.Net doesn't allow to change data inside VertexArray's vertices'.
        /// This method is a workaround which gets the vertex, stores it as a variable,
        /// executes the <paramref name="action"/> on it and the sets the modified version in place.
        /// </remarks>
        /// <param name="index">Vertex index.</param>
        /// <param name="action">The VertexAction delegate to perform on the vertex.</param>
        public static void ModifyVertex(this VertexArray va, uint index, VertexAction action)
        {
            Vertex vertex = va[index];
            action(ref vertex);
            va[index] = vertex;
        }

        /// <summary>
        /// Updates the color of every vertex in the VertexArray
        /// </summary>
        /// <param name="color">New color.</param>
        public static void SetColor(this VertexArray va, Color color)
        {
            va.ForEach((ref Vertex v) => v.Color = color);
        }

        /// <summary>
        /// Transforms the position of every vertex in the VertexArray
        /// </summary>
        /// <param name="transform">Transform.</param>
        public static void Transform(this VertexArray va, Transform transform)
        {
            va.ForEach((ref Vertex v) => v.Position = transform.TransformPoint(v.Position));
        }

        /// <summary>
        /// Moves every vertex in the VertexArray
        /// </summary>
        /// <param name="direction">Direction vector.</param>
        public static void Move(this VertexArray va, Vector2f direction)
        {
            va.ForEach((ref Vertex v) => v.Position += direction);
        }

        /// <summary>
        /// Darkens the color of every vertex in the VertexArray
        /// by the specified value.
        /// </summary>
        /// <param name="r">The value.</param>
        public static void Darken(this VertexArray va, float r)
        {
            va.ForEach((ref Vertex v) => v.Color = v.Color.Darken(r));
        }

        /// <summary>
        /// Lightens the color of every vertex in the VertexArray
        /// by the specified value.
        /// </summary>
        /// <param name="r">The value.</param>
        public static void Lighten(this VertexArray va, float r)
        {
            va.ForEach((ref Vertex v) => v.Color = v.Color.Lighten(r));
        }

        /// <summary>
        /// Applies a new color to every vertex in the VertexArray 
        /// by interpolation.
        /// </summary>
        /// <param name="nextColor">The next color.</param>
        /// <param name="r">The interval value.</param>
        public static void Interpolate(this VertexArray va, Color nextColor, float r)
        {
            va.ForEach((ref Vertex v) => v.Color = v.Color.Interpolate(nextColor, r));
        }

        /// <summary>
        /// Applies the complementary color for each vertex in the VertexArray
        /// </summary>
        public static void Complementary(this VertexArray va)
        {
            va.ForEach((ref Vertex v) => v.Color = v.Color.Complementary());
        }
    }
}
