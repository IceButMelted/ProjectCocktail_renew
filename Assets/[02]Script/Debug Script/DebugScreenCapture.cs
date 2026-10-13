using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Right-click the component header → "Capture Screen" to save the Game view
/// as a PNG into <see cref="_outputDirectory"/>.
///
/// Relative paths resolve against the project root in the Editor (next to
/// Assets/) and against Application.persistentDataPath in builds. Absolute
/// paths are used as-is.
///
/// ScreenCapture writes the file at the end of the next rendered frame, so in
/// Edit mode the Game view must be open (and may need a repaint) for the file
/// to appear.
/// </summary>
public class DebugScreenCapture : MonoBehaviour
{
    [Tooltip("Relative to the project root in the Editor, or absolute.")]
    [SerializeField] private string _outputDirectory = "Screenshots";
    [SerializeField] private string _filePrefix = "Capture";
    [Tooltip("Resolution multiplier (1 = Game view size).")]
    [Range(1, 8)]
    [SerializeField] private int _superSize = 1;

    [ContextMenu("Capture Screen")]
    public void CaptureScreen()
    {
        string directory = ResolveDirectory();
        Directory.CreateDirectory(directory);

        string fileName = $"{_filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
        string fullPath = Path.Combine(directory, fileName);

        ScreenCapture.CaptureScreenshot(fullPath, _superSize);
        Debug.Log($"[DebugScreenCapture] Saving screenshot to {fullPath}", this);
    }

    [ContextMenu("Open Output Folder")]
    private void OpenOutputFolder()
    {
        string directory = ResolveDirectory();
        Directory.CreateDirectory(directory);
        Application.OpenURL("file://" + directory);
    }

    private string ResolveDirectory()
    {
        if (Path.IsPathRooted(_outputDirectory))
            return _outputDirectory;

#if UNITY_EDITOR
        string root = Path.GetDirectoryName(Application.dataPath);
#else
        string root = Application.persistentDataPath;
#endif
        return Path.GetFullPath(Path.Combine(root, _outputDirectory));
    }
}
