using System;
using UnityEngine;

namespace GPTUnity.Helpers
{
    public static class Extensions
    {
        /// <summary>
        /// Sets the alpha value of a Color.
        /// </summary>
        /// <param name="color">The original color.</param>
        /// <param name="alpha">The alpha value to set (0 to 1).</param>
        /// <returns>A new Color with the updated alpha value.</returns>
        public static Color WithAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }
        
        public static string PathToGameObject(this GameObject go)
        {
            if (go == null)
                return string.Empty;

            var path = go.name;
            var parent = go.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
        
        public static string FormatExceptionWithInner(this Exception ex, string context = null)
        {
            var sb = new System.Text.StringBuilder();

            if (!string.IsNullOrEmpty(context))
            {
                sb.AppendLine(context);
            }

            int depth = 0;
            while (ex != null)
            {
                string indent = new string('-', depth);
                sb.AppendLine($"<color=red>{indent} {ex.Message}</color>");
                ex = ex.InnerException;
                depth++;
            }

            return sb.ToString();
        }
    }
}