using System.Collections;
using UnityEngine;
using Yarn.Unity;
using static E_Cocktail;

public class Test_NPCGetIn : MonoBehaviour
{
    [Header("Sprite Emotional")]
    public SpriteRenderer SpriteRenderer;
    [SerializeField] public Sprite _neutralSprite;
    [SerializeField] public Sprite _happySprite;
    [SerializeField] public Sprite _upsetSprite;

    [Header("Sprite Look Direction")]
    [SerializeField] private Sprite _lookLeftSprite;
    [SerializeField] private Sprite _lookRightSprite;
    [SerializeField] private Sprite _lookForwardSprite;

    public NPC_Name Name;

    [Header("Waypoints")]
    [SerializeField] private Transform[] _waypoints;

    [Header("Settings")]
    [SerializeField] private float _moveSpeed = 3f;

    private bool _isMoving = false;
    private bool _arrived = false;
    private int _currentIndex = -1;

    private void Start()
    {
        if (SpriteRenderer == null)
            SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (_waypoints == null || _waypoints.Length == 0)
            Debug.LogWarning($"{name} has no waypoints assigned.");
        if(_lookForwardSprite == null)
            _lookForwardSprite = _neutralSprite;
    }

    private void Update()
    {
        if (!_isMoving) return;

        Transform target = _waypoints[_currentIndex];
        Vector3 dir = target.position - transform.position;

        if (dir.sqrMagnitude <= 0.01f)
        {
            transform.position = target.position;
            _isMoving = false;
            _arrived = true;
            return;
        }

        transform.position += dir.normalized * (_moveSpeed * Time.deltaTime);
    }


    // ─── Yarn Commands ───────────────────────────────────────────────────────

    [YarnCommand("SetEmotion")]
    public static void SetEmotion(string npcNameStr, string emotion)
    {
        Test_NPCGetIn npc = FindNPC(npcNameStr);
        if (npc == null) return;
        npc.DoSetEmotion(emotion);
    }
    [YarnCommand("SetLookDirection")]
    public static void SetLookDirection(string npcNameStr, string direction)
    {
        Test_NPCGetIn npc = FindNPC(npcNameStr);
        if (npc == null) return;
        Debug.Log($"{npc.name} now looking {direction}");
        npc.DoLookDirection(direction);
    }

    [YarnCommand("MoveIn")]
    public static IEnumerator MoveIn(string npcNameStr, int waypointIndex)
    {
        Test_NPCGetIn npc = FindNPC(npcNameStr);
        if (npc == null) yield break;
        yield return npc.StartCoroutine(npc.DoMoveTo(waypointIndex));
    }

    [YarnCommand("MoveOut")]
    public static IEnumerator MoveOut(string npcNameStr)
    {
        yield return MoveIn(npcNameStr, 0);
    }

    // ─── Helper Methods ─────────────────────────────────────────────────────
    private static Test_NPCGetIn FindNPC(string npcNameStr)
    {
        // Yarn passes "NPC_Name.Owen" — strip the prefix if present
        string rawName = npcNameStr.Contains(".")
            ? npcNameStr.Split('.')[1]
            : npcNameStr;

        if (!System.Enum.TryParse<NPC_Name>(rawName, ignoreCase: true, out NPC_Name targetName)
            || targetName == NPC_Name.None)
        {
            Debug.LogWarning($"[FindNPC] Invalid NPC name: '{npcNameStr}'");
            return null;
        }

        foreach (Test_NPCGetIn npc in FindObjectsByType<Test_NPCGetIn>(FindObjectsSortMode.None))
        {
            if (npc.Name == targetName)
                return npc;
        }

        Debug.LogWarning($"[FindNPC] No NPC with NPCName.{targetName} found in scene.");
        return null;
    }

    private void DoSetEmotion(string emotion)
    {
        SpriteRenderer sr = SpriteRenderer;
        sr.sprite = emotion.ToLower() switch
        {
            "happy" => _happySprite,
            "upset" => _upsetSprite,
            "neutral" => _neutralSprite,
            _ => sr.sprite
        };

        if (sr.sprite == null)
            Debug.LogWarning($"Unknown emotion '{emotion}' for {name}");
    }

    private void DoLookDirection(string direction)
    {
        string dir = direction.ToLower();
        string rawDir = direction.Contains(".") ? dir.Split('.')[1] : dir;
        switch (rawDir)
        {
            case "left":
                SpriteRenderer.sprite = _lookLeftSprite;
                break;
            case "right":
                SpriteRenderer.sprite = _lookRightSprite;
                break;
            case "forward":
                SpriteRenderer.sprite = _lookForwardSprite;
                break;
            default:
                Debug.LogWarning($"Unknown look direction '{direction}' for {name}");
                break;
        }
    }

    private IEnumerator DoMoveTo(int waypointIndex)
    {
        if (!IsValidIndex(waypointIndex)) yield break;

        Debug.Log($"{name} moving to waypoint {waypointIndex}");
        _currentIndex = waypointIndex;
        _arrived = false;
        _isMoving = true;

        yield return new WaitUntil(() => _arrived);
        Debug.Log($"{name} reached waypoint {waypointIndex}");
    }

    private bool IsValidIndex(int index)
    {
        if (_waypoints == null || index < 0 || index >= _waypoints.Length)
        {
            Debug.LogWarning($"{name}: waypoint index {index} is out of range.");
            return false;
        }
        return true;
    }
}