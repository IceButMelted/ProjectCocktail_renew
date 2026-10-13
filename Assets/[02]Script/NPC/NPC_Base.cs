using System.Collections;
using UnityEngine;
using static E_Cocktail;

/// <summary>
/// One person at the bar: their face, their gaze and where they stand.
///
/// Knows nothing about dialogue. TalkToCustomerManager finds an NPC by name and owns the Yarn
/// commands (SetEmotion, SetLookDirection, MoveIn, MoveOut) that drive the methods below, and the
/// save system's snapshot of the position.
/// </summary>
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

    public int CurrentWaypointIndex { get => _currentIndex; set => _currentIndex = value; }
    public Direction CurrentLookDirection { get => _currentLookDirection; set => _currentLookDirection = value; }

    private void Start()
    {
        if (SpriteRenderer == null)
            SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (_waypoints == null || _waypoints.Length == 0)
            Debug.LogWarning($"{name} has no waypoints assigned.");
        if (_lookForwardSprite == null)
            _lookForwardSprite = _neutralSprite;
    }

    /// <summary>"happy", "upset" or "neutral". Anything else keeps the current face so the typo is visible in the scene.</summary>
    public void SetEmotion(string emotion)
    {
        SpriteRenderer.sprite = emotion.ToLower() switch
        {
            "happy" => _happySprite,
            "upset" => _upsetSprite,
            "neutral" => _neutralSprite,
            _ => SpriteRenderer.sprite
        };
    }

    /// <summary>"left", "right" or "forward", optionally spelled "Direction.left".</summary>
    public void LookDirection(string direction)
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

    /// <summary>Slides to the waypoint at <paramref name="waypointIndex"/>. Run it with StartCoroutine on this NPC.</summary>
    public IEnumerator MoveTo(int waypointIndex)
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
}
