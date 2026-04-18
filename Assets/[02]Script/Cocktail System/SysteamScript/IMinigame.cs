using UnityEngine;

/// <summary>
/// ค่า Config พื้นฐานที่ทุก Minigame ต้องการ
/// Derive ต่อเพื่อเพิ่มค่าเฉพาะของแต่ละเกม
/// </summary>
[CreateAssetMenu(fileName = "NewMinigameSetting", menuName = "Bar410/Minigame/Base Setting")]
public class SO_MinigameSetting : ScriptableObject
{
    [Header("General")]
    [Tooltip("ตัวคูณความยาก (1 = ปกติ, >1 = ยากขึ้น)")]
    public float DifficultyMultiplier = 1f;

    [Tooltip("เวลารวมของ Minigame (วินาที)")]
    public float Duration = 5f;
}

/// <summary>
/// GDD 3.2.1 — Spam Click เพื่อรักษาเกจให้อยู่ใน Zone
/// </summary>
[CreateAssetMenu(fileName = "ShakingSetting", menuName = "Bar410/Minigame/Shaking Setting")]
public class SO_ShakingSetting : SO_MinigameSetting
{
    [Header("Shaking Zone")]
    [Range(0f, 1f)] public float TargetZoneMin = 0.4f;
    [Range(0f, 1f)] public float TargetZoneMax = 0.7f;

    [Tooltip("เกจลดลงต่อวินาที (ไม่คลิก)")]
    public float GaugeDecayRate = 0.15f;

    [Tooltip("เกจเพิ่มขึ้นต่อ 1 คลิก")]
    public float GaugeIncreasePerClick = 0.08f;
}

/// <summary>
/// GDD 3.2.2 — กดให้เข็มหยุดในช่องที่กำหนด (Timing Bar)
/// </summary>
[CreateAssetMenu(fileName = "MixingSetting", menuName = "Bar410/Minigame/Mixing Setting")]
public class SO_MixingSetting : SO_MinigameSetting
{
    [Header("Timing Bar")]
    [Range(0f, 1f)] public float TargetZoneMin = 0.45f;
    [Range(0f, 1f)] public float TargetZoneMax = 0.55f;

    [Tooltip("ความเร็วเข็ม (normalized 0-1 ต่อวินาที)")]
    public float NeedleSpeed = 0.6f;

    [Tooltip("จำนวนครั้งที่ต้องกด")]
    public int RequiredHits = 3;
}


/// <summary>
/// GDD 3.2.3 — บด Mixer ก่อน แล้วเพิ่มส่วนผสมอื่น
/// </summary>
[CreateAssetMenu(fileName = "GrindSetting", menuName = "Bar410/Minigame/Grind Setting")]
public class SO_GrindSetting : SO_MinigameSetting
{
    [Header("Grind")]
    [Tooltip("จำนวนคลิกที่ต้องบดจนครบ")]
    public int RequiredGrindClicks = 10;

    [Tooltip("หลังบดเสร็จ ผู้เล่นเลือกทำ Shaking หรือ Mixing ต่อได้")]
    public bool CanChainToShaking = true;
    public bool CanChainToMixing = true;
}

/// <summary>
/// Interface มาตรฐานที่ทุก Minigame ต้อง implement
/// เพื่อให้ BeverageManager เรียกใช้แบบ Polymorphism ได้
/// </summary>
public interface IMinigame
{
    /// <summary>Config ของ Minigame นี้ (ตั้งค่าผ่าน Inspector)</summary>
    SO_MinigameSetting Setting { get; set; }

    /// <summary>เริ่มต้น Minigame — Reset state, เริ่ม Timer</summary>
    void StartGame();

    /// <summary>จบเกม — ส่ง Event ผลลัพธ์, Cleanup</summary>
    void EndGame();

    /// <summary>เรียกทุก Frame (ใน Update) — Update logic หลัก</summary>
    void ProcessedGame();

    /// <summary>ดึง state ปัจจุบันในรูป string (ใช้ Debug / UI)</summary>
    string GetGameState();

    /// <summary>true = กำลังรันอยู่</summary>
    bool IsRunning { get; }
}