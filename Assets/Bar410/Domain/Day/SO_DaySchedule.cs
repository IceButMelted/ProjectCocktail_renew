using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// One row per day: the Yarn node StoryBeforeOpen starts that day, and the chapter name the
/// UI shows when the bar opens. The day number itself is the Yarn variable <c>$day</c>.
/// </summary>
[CreateAssetMenu(fileName = "DaySchedule", menuName = "Bar410/Day Schedule")]
public class SO_DaySchedule : ScriptableObject
{
    [Serializable]
    public class Day
    {
        [TableColumnWidth(50, Resizable = false), Min(1)]
        public int day = 1;

        [Tooltip("Yarn node StoryBeforeOpen starts on this day.")]
        public string startNode;

        // ponytail: plain string until Localization exists; then this becomes a String Table key.
        [Tooltip("Shown by the chapter UI when the bar opens.")]
        public string chapterName;
    }

    [TableList(AlwaysExpanded = true, ShowIndexLabels = false)]
    [ValidateInput(nameof(NoDuplicateDays), "The same day appears in more than one row")]
    [SerializeField] private List<Day> days = new List<Day>();

    /// <summary>The row for this day, or false when the table has none.</summary>
    public bool TryGet(int day, out Day row)
    {
        row = days.FirstOrDefault(d => d.day == day);
        return row != null;
    }

    private bool NoDuplicateDays(List<Day> list)
        => list == null || list.Select(d => d.day).Distinct().Count() == list.Count;
}
