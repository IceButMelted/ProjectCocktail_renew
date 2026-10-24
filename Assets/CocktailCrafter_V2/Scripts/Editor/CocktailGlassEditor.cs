using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CocktailGlass))]
public class CocktailGlassEditor : Editor
{
    // Points within this fraction of the mask height from its top count as the rim
    const float RimBand = 0.05f;

    public override void OnInspectorGUI()
    {
        var glass = (CocktailGlass)target;
        if (glass.innerTop && glass.fillLine && glass.Headroom < glass.minHeadroom)
            EditorGUILayout.HelpBox(
                $"Wave headroom (InnerTop - FillLine) is {glass.Headroom:F2}, below {glass.minHeadroom:F2}. Ripples will be flattened. Move FillLine down.",
                MessageType.Warning);

        if (GUILayout.Button("Generate Inner Collider From Mask"))
            GenerateInnerCollider(glass);

        DrawDefaultInspector();
    }

    // Traces the mask sprite's physics outline into InnerCollider, leaving the rim open so ice can drop in.
    static void GenerateInnerCollider(CocktailGlass glass)
    {
        SpriteMask mask = glass.liquidMask;
        EdgeCollider2D col = glass.innerCollider;
        if (!mask || !mask.sprite || !col)
        {
            EditorUtility.DisplayDialog("Generate Inner Collider", "Assign Liquid Mask (with a sprite) and Inner Collider first.", "OK");
            return;
        }

        Sprite sprite = mask.sprite;
        if (sprite.GetPhysicsShapeCount() == 0)
        {
            EditorUtility.DisplayDialog("Generate Inner Collider",
                $"Sprite '{sprite.name}' has no physics shape. Enable 'Generate Physics Shape' in its import settings.", "OK");
            return;
        }

        // Use the outline with the most points (the glass interior)
        var shape = new List<Vector2>();
        var candidate = new List<Vector2>();
        for (int i = 0; i < sprite.GetPhysicsShapeCount(); i++)
        {
            sprite.GetPhysicsShape(i, candidate);
            if (candidate.Count > shape.Count) shape = new List<Vector2>(candidate);
        }

        int n = shape.Count;
        var local = new Vector2[n];
        float minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < n; i++)
        {
            local[i] = col.transform.InverseTransformPoint(mask.transform.TransformPoint(shape[i]));
            minY = Mathf.Min(minY, local[i].y);
            maxY = Mathf.Max(maxY, local[i].y);
        }

        // Longest run of consecutive rim points; the path goes around everything else
        float threshold = maxY - (maxY - minY) * RimBand;
        int runStart = -1, runLength = 0;
        for (int s = 0; s < n; s++)
        {
            if (local[s].y < threshold || local[(s - 1 + n) % n].y >= threshold) continue;
            int length = 0;
            while (length < n && local[(s + length) % n].y >= threshold) length++;
            if (length > runLength) { runStart = s; runLength = length; }
        }

        int first, count;
        if (runLength >= 2)
        {
            first = (runStart + runLength - 1) % n; // last rim point
            count = n - runLength + 2;              // ... around the bowl ... back to the first rim point
        }
        else
        {
            // Pointy rim: drop the single highest segment
            int top = 0;
            for (int i = 1; i < n; i++)
                if (local[i].y + local[(i + 1) % n].y > local[top].y + local[(top + 1) % n].y) top = i;
            first = (top + 1) % n;
            count = n;
        }

        var points = new Vector2[count];
        for (int k = 0; k < count; k++) points[k] = local[(first + k) % n];

        Undo.RecordObject(col, "Generate Inner Collider");
        col.offset = Vector2.zero;
        col.points = points;
        EditorUtility.SetDirty(col);
    }
}
