using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public struct Expression
{
    public string id;
    public Sprite face;
    public Sprite body;
    public Vector2 faceOffset;
}

public class CharacterExpression : MonoBehaviour
{
    [SerializeField] SpriteRenderer _bodyRenderer, _faceRenderer;
    [SerializeField] List<Expression> _expressions = new();
    [SerializeField] string _testExpression = "Neutral";

    Dictionary<string, Expression> _lookup;

    public void SetExpression(string id)
    {
        var e = ResolveExpression(id);
        if (e == null) return;
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObjects(
            new UnityEngine.Object[] { _bodyRenderer, _faceRenderer }, "Set Expression");
#endif
        _bodyRenderer.sprite = e.Value.body;
        _faceRenderer.sprite = e.Value.face;
        _faceRenderer.transform.localPosition = e.Value.faceOffset;
    }

    Expression? ResolveExpression(string id)
    {
        _lookup ??= _expressions.ToDictionary(e => e.id, StringComparer.OrdinalIgnoreCase);
        if (_lookup.TryGetValue(id, out var e)) return e;
        Debug.LogWarning($"Expression '{id}' not found on {name}", this);
        return _lookup.TryGetValue("Neutral", out var n) ? n : null;
    }

    [ContextMenu("Apply Test Expression")]
    void ApplyTest() { _lookup = null; SetExpression(_testExpression); }

    void OnValidate() => _lookup = null;
}