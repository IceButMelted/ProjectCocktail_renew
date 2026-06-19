using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using static E_Cocktail;

public class NPC_Base : MonoBehaviour
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
    private Direction _currentLookDirection;

    [Header("Waypoints")]
    [SerializeField] private Transform[] _waypoints;

    [Header("Settings")]
    [SerializeField] private float _moveSpeed = 3f;

    private int _currentIndex = -1;

    private static readonly Dictionary<NPC_Name, NPC_Base> _registry = new();

    public int CurrentWaypointIndex { get => _currentIndex; set => _currentIndex = value; }
    public Direction CurrentLookDirection { get => _currentLookDirection; set => _currentLookDirection = value; }

    private void Awake() => _registry[Name] = this;
    private void OnDestroy() => _registry.Remove(Name);

    private void Start()
    {
        if (SpriteRenderer == null)
            SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (_waypoints == null || _waypoints.Length == 0)
            Debug.LogWarning($"{name} has no waypoints assigned.");
        if (_lookForwardSprite == null)
            _lookForwardSprite = _neutralSprite;
    }


    // ─── Yarn Commands ───────────────────────────────────────────────────────

    [YarnCommand("SetEmotion")]
    public static void SetEmotion(string npcNameStr, string emotion) =>
        FindNPC(npcNameStr)?.DoSetEmotion(emotion);

    [YarnCommand("SetLookDirection")]
    public static void SetLookDirection(string npcNameStr, string direction) =>
        FindNPC(npcNameStr)?.DoLookDirection(direction);

    [YarnCommand("MoveIn")]
    public static IEnumerator MoveIn(string npcNameStr, int waypointIndex)
    {
        if (SceneLoaderBridge.IsSilentReplay) yield break;
        yield return SlideMoveIn(npcNameStr, waypointIndex);
    }

    [YarnCommand("MoveOut")]
    public static IEnumerator MoveOut(string npcNameStr)
    {
        if (SceneLoaderBridge.IsSilentReplay) yield break;
        yield return SlideMoveOut(npcNameStr);
    }


    #region Method Helpers
    public static IEnumerator SlideMoveIn(string npcNameStr, int waypointIndex)
    {
        NPC_Base npc = FindNPC(npcNameStr);
        if (npc == null) yield break;
        yield return npc.StartCoroutine(npc.DoMoveTo(waypointIndex));
    }

    public static IEnumerator SlideMoveOut(string npcNameStr) => SlideMoveIn(npcNameStr, 0);

    private static NPC_Base FindNPC(string npcNameStr)
    {
        string raw = npcNameStr.Contains(".") ? npcNameStr.Split('.')[1] : npcNameStr;
        if (!System.Enum.TryParse<NPC_Name>(raw, true, out NPC_Name target) || target == NPC_Name.None)
        {
            Debug.LogWarning($"[FindNPC] Invalid NPC name: '{npcNameStr}'");
            return null;
        }
        if (!_registry.TryGetValue(target, out NPC_Base npc))
        {
            Debug.LogWarning($"[FindNPC] No NPC with NPCName.{target} found in scene.");
            return null;
        }
        return npc;
    }

    private void DoSetEmotion(string emotion)
    {
        SpriteRenderer.sprite = emotion.ToLower() switch
        {
            "happy" => _happySprite,
            "upset" => _upsetSprite,
            "neutral" => _neutralSprite,
            _ => SpriteRenderer.sprite  // unknown: keep current, let Yarn script catch the typo
        };
    }

    private void DoLookDirection(string direction)
    {
        string raw = (direction.Contains(".") ? direction.Split('.')[1] : direction).ToLower();
        switch (raw)
        {
            case "left": SpriteRenderer.sprite = _lookLeftSprite; break;
            case "right": SpriteRenderer.sprite = _lookRightSprite; break;
            case "forward": SpriteRenderer.sprite = _lookForwardSprite; break;
            default: Debug.LogWarning($"Unknown look direction '{direction}' for {name}"); break;
        }
    }

    private IEnumerator DoMoveTo(int waypointIndex)
    {
        if (!IsValidIndex(waypointIndex)) yield break;
        _currentIndex = waypointIndex;
        Transform target = _waypoints[waypointIndex];
        while (transform.position != target.position)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, _moveSpeed * Time.deltaTime);
            yield return null;
        }
    }

    public void TeleportToWaypoint(int index)
    {
        if (!IsValidIndex(index)) return;
        _currentIndex = index;
        transform.position = _waypoints[index].position;
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
    #endregion
}