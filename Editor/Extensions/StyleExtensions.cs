using UnityEngine.UIElements;

namespace GPTUnity.Helpers
{
    public static class StyleExtensions
    {
        public static void SetAllBorder(this IStyle style, float value)
        {
            style.borderBottomLeftRadius = style.borderBottomRightRadius =
                style.borderTopLeftRadius = style.borderTopRightRadius = value;
        }

        public static void SetAllPadding(this IStyle style, float value)
        {
            style.paddingRight = style.paddingLeft = style.paddingTop = style.paddingBottom = value;
        }
    }
}