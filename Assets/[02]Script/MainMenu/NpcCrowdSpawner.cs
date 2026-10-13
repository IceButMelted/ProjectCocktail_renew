using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ambient crowd for the main menu: pooled silhouette NPCs crossing the frame
/// on several lanes in both directions. Call FadeOutAll() from the Start button.
/// </summary>
public class NpcCrowdSpawner : MonoBehaviour
{
    [SerializeField] private NpcWalker npcPrefab;
    [SerializeField] private NpcLane[] lanes;
    [SerializeField] private Transform cameraTransform;

    [Header("Crowd")]
    [SerializeField] private int maxActive = 8;
    [SerializeField] private int prewarmCount = 5;
    [SerializeField] private Vector2 spawnInterval = new Vector2(2f, 6f);

    [Header("Per-NPC Randomness")]
    [SerializeField] private Vector2 walkSpeed = new Vector2(0.8f, 1.4f);
    [SerializeField] private Vector2 bobFrequency = new Vector2(1.6f, 2.4f);

    [Header("Exit")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    private readonly Stack<NpcWalker> pool = new Stack<NpcWalker>();
    private readonly List<NpcWalker> active = new List<NpcWalker>();
    private float[] nextSpawnTime;
    private bool stopped;

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        for (int i = 0; i < maxActive; i++)
        {
            NpcWalker npc = Instantiate(npcPrefab, transform);
            npc.gameObject.SetActive(false);
            pool.Push(npc);
        }

        nextSpawnTime = new float[lanes.Length];
        for (int i = 0; i < lanes.Length; i++)
            nextSpawnTime[i] = Time.time + Random.Range(spawnInterval.x, spawnInterval.y);

        for (int i = 0; i < prewarmCount; i++)
            Spawn(lanes[Random.Range(0, lanes.Length)], Random.value);
    }

    private void Update()
    {
        if (stopped) return;

        for (int i = 0; i < lanes.Length; i++)
        {
            if (Time.time < nextSpawnTime[i]) continue;

            Spawn(lanes[i], 0f);
            nextSpawnTime[i] = Time.time + Random.Range(spawnInterval.x, spawnInterval.y);
        }
    }

    public void FadeOutAll()
    {
        stopped = true;
        foreach (NpcWalker npc in active)
            npc.FadeOut(fadeOutDuration);
    }

    private void Spawn(NpcLane lane, float startProgress)
    {
        if (pool.Count == 0 || !lane.IsValid) return;

        bool reverse = Random.value < 0.5f;
        NpcWalker npc = pool.Pop();
        active.Add(npc);
        npc.Begin(
            reverse ? lane.End : lane.Start,
            reverse ? lane.Start : lane.End,
            Random.Range(walkSpeed.x, walkSpeed.y),
            Random.Range(bobFrequency.x, bobFrequency.y),
            startProgress,
            cameraTransform,
            Release);
    }

    private void Release(NpcWalker npc)
    {
        active.Remove(npc);
        pool.Push(npc);
    }
}
