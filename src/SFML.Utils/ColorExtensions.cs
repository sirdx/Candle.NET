using SFML.Graphics;

namespace SFML.Utils
{
    /// <summary>
    /// Utility extension functions for colors.
    /// </summary>
    public static class ColorExtensions
    {
        /// <summary>
        /// Lightens the color by the specified value.
        /// </summary>
        /// <param name="r">The value.</param>
        /// <returns>A new lightened color.</returns>
        public static Color Lighten(this Color color, float r)
        {
            return new Color((byte)(color.R * (1F + r)), (byte)(color.G * (1F + r)), (byte)(color.B * (1F + r)), color.A);
        }

        /// <summary>
        /// Darkens the color by the specified value.
        /// </summary>
        /// <param name="r">The value.</param>
        /// <returns>A new darkened color.</returns>
        public static Color Darken(this Color color, float r)
        {
            return color.Lighten(-r);
            // return new Color((byte)(color.R * (1F - r)), (byte)(color.G * (1F - r)), (byte)(color.B * (1F - r)), color.A);
        }

        /// <summary>
        /// Calculates a new color by interpolation.
        /// </summary>
        /// <param name="nextColor">The next color.</param>
        /// <param name="r">The interval value.</param>
        /// <returns>A new interpolated color.</returns>
        public static Color Interpolate(this Color color, Color nextColor, float r)
        {
            return new Color
            (
                (byte)(color.R + (nextColor.R - color.R) * r), 
                (byte)(color.G + (nextColor.G - color.G) * r),
                (byte)(color.B + (nextColor.B - color.B) * r), 
                (byte)(color.A + (nextColor.A - color.A) * r)
            );
        }

        /// <summary>
        /// Calculates the complementary color of the color
        /// </summary>
        /// <returns>A new complementary color.</returns>
        public static Color Complementary(this Color color)
        {
            return new Color((byte)(255 - color.R), (byte)(255 - color.G), (byte)(255 - color.B), color.A);
        }
    }
}
