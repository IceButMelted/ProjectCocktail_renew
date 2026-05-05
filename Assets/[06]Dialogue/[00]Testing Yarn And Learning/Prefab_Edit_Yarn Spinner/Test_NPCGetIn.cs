using System.Collections;
using UnityEngine;
using Yarn.Unity;

public class Test_NPCGetIn : MonoBehaviour
{
    [Header("Sprite Emotional")]
    public SpriteRenderer SpriteRenderer;
    [SerializeField] public Sprite _neutralSprite;
    [SerializeField] public Sprite _happySprite;
    [SerializeField] public Sprite _upsetSprite;

    [Header("Waypoints")]
    [SerializeField] private Transform[] _waypoints;

    [Header("Settings")]
    [SerializeField] private float _moveSpeed = 3f;

    private bool _isMoving = false;
    private bool _arrived = false;
    private int _currentIndex = -1;

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
        //transform.rotation = Quaternion.LookRotation(dir);
    }

    [YarnCommand("SetEmotion")]
    public static void SetEmotion(string npcName, string emotion)
    {
        Test_NPCGetIn npc = FindNPC(npcName);
        if (npc == null) return;
        npc.DoSetEmotion(emotion);
    }

    private void DoSetEmotion(string emotion)
    {
        SpriteRenderer sr = SpriteRenderer;
        sr.sprite = emotion.ToLower() switch
        {
            "happy" => _happySprite,
            "upset" => _upsetSprite,
            "neutral" => _neutralSprite,
            _ => sr.sprite // keep current on unknown
        };

        if (sr.sprite == null)
            Debug.LogWarning($"Unknown emotion '{emotion}' for {name}");
    }

    [YarnCommand("MoveIn")]
    public static IEnumerator MoveIn(string npcName, int waypointIndex)
    {
        Test_NPCGetIn npc = FindNPC(npcName);
        if (npc == null) yield break;
        yield return npc.StartCoroutine(npc.DoMoveTo(waypointIndex));
    }

    [YarnCommand("MoveOut")]
    public static IEnumerator MoveOut(string npcName)
    {
        yield return MoveIn(npcName, 0);
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

    private static Test_NPCGetIn FindNPC(string npcName)
    {
        GameObject go = GameObject.Find(npcName);
        if (go == null)
        {
            Debug.LogWarning($"[MoveIn] No GameObject named '{npcName}' found.");
            return null;
        }

        Test_NPCGetIn npc = go.GetComponent<Test_NPCGetIn>();
        if (npc == null)
            Debug.LogWarning($"[MoveIn] '{npcName}' has no Test_NPCGetIn component.");

        return npc;
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