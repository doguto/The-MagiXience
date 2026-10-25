using Project.Scenes.Battle.Scripts.Model;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Presenter
{
    internal static class WindIndicator
    {
        static GUIStyle label;
        public static void Draw()
        {
            if (!BattleWind.Enabled) return;
            var matrix = GUI.matrix;
            var color = GUI.color;
            float scale = Mathf.Max(0.4f, Screen.height / 1080f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUI.color = new Color(0.03f, 0.08f, 0.16f, 0.85f);
            GUI.DrawTexture(new Rect(24, 24, 146, 66), Texture2D.whiteTexture);
            label ??= new GUIStyle(GUI.skin.label) { fontSize = 19, alignment = TextAnchor.MiddleLeft };
            GUI.color = Color.white;
            GUI.Label(new Rect(38, 39, 64, 34), "WIND", label);
            GUI.color = new Color(0.5f, 0.95f, 1f);
            var center = new Vector2(134, 57);
            if (BattleWind.Direction == WindDirection.None)
            {
                // Middle dot, drawn without depending on the font's Japanese glyphs.
                GUI.DrawTexture(new Rect(center.x - 4, center.y - 4, 8, 8), Texture2D.whiteTexture,
                    ScaleMode.StretchToFill, true, 0f, GUI.color, 0f, 4f);
            }
            else
            {
                Vector2 dir = BattleWind.Vector;
                dir.y = -dir.y;
                Vector2 side = new(-dir.y, dir.x);
                Vector2 tip = center + dir * 18f;
                Line(center - dir * 17f, tip, 4f);
                Line(tip, tip - dir * 12f + side * 10f, 4f);
                Line(tip, tip - dir * 12f - side * 10f, 4f);
            }
            GUI.matrix = matrix;
            GUI.color = color;
        }

        static void Line(Vector2 from, Vector2 to, float width)
        {
            var matrix = GUI.matrix;
            var delta = to - from;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, from);
            GUI.DrawTexture(new Rect(from.x, from.y - width / 2f, delta.magnitude, width), Texture2D.whiteTexture);
            GUI.matrix = matrix;
        }
    }
}
