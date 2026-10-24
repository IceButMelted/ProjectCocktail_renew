using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Tools > Capture: saves a PNG of the Game view or Scene view to <project>/Screenshots.
public static class ViewCapture
{
    const string Folder = "Screenshots";

    [MenuItem("Tools/Capture/Game View %#F12")]
    static void CaptureGameView()
    {
        string path = NewPath("Game");
        // Includes Screen Space - Overlay UI; written when the Game view next renders.
        ScreenCapture.CaptureScreenshot(path);
        var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        EditorWindow.GetWindow(gameViewType).Repaint();
        EditorApplication.delayCall += () => Done(path);
    }

    [MenuItem("Tools/Capture/Scene View %&F12")]
    static void CaptureSceneView()
    {
        var view = SceneView.lastActiveSceneView;
        if (view == null) { Debug.LogWarning("[Capture] No Scene view open."); return; }

        Camera cam = view.camera;
        int w = cam.pixelWidth, h = cam.pixelHeight;
        var rt = RenderTexture.GetTemporary(w, h, 24);
        var prevTarget = cam.targetTexture;
        var prevActive = RenderTexture.active;

        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();

        cam.targetTexture = prevTarget;
        RenderTexture.active = prevActive;
        RenderTexture.ReleaseTemporary(rt);

        string path = NewPath("Scene");
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        Done(path);
    }

    static string NewPath(string kind)
    {
        Directory.CreateDirectory(Folder);
        return Path.GetFullPath(Path.Combine(Folder, $"{kind}_{DateTime.Now:yyyyMMdd_HHmmss}.png"));
    }

    static void Done(string path)
    {
        Debug.Log($"[Capture] Saved {path}");
        EditorUtility.RevealInFinder(path);
    }
}
