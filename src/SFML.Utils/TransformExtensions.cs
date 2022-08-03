using System.Reflection;
using SFML.Graphics;

namespace SFML.Utils
{
    /// <summary>
    /// Utility extension functions for transforms.
    /// </summary>
    public static class TransformExtensions
    {
        /// <summary>
        /// Creates a deep copy of the transform.
        /// </summary>
        /// <remarks>
        /// SFML.Net's Transform struct does not have a copy constructor,
        /// so this is a workaround that creates a Transform clone by 
        /// multiplying the identity transform by the selected transform.
        /// </remarks>
        /// <returns>A new transform.</returns>
        public static Transform Clone(this Transform transform)
        {
            return Transform.Identity * transform;

            // OLD CLONE METHOD - WILL REMOVE SOON
            // Type type = ob.GetType();
            // float a00 = (float)type.GetField("m00", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ob);
            // float a01 = (float)type.GetField("m01", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ob);
            // float a02 = (float)type.GetField("m02", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ob);
            // float a10 = (float)type.GetField("m10", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ob);
            // float a11 = (float)type.GetField("m11", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ob);
            // float a12 = (float)type.GetField("m12", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ob);
            // float a20 = (float)type.GetField("m20", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ob);
            // float a21 = (float)type.GetField("m21", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ob);
            // float a22 = (float)type.GetField("m22", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ob);
            // return new Transform(a00, a01, a02, a10, a11, a12, a20, a21, a22);
        }
    }
}
