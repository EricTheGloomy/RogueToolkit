using System.Collections.Generic;
using UnityEngine;
using RogueToolkit.Core.Validation;

[CreateAssetMenu(menuName = "Grid/Piece")]
public class GridPiece : ScriptableObject
{
    [Tooltip("Name shown in your UI. If empty, the asset's file name is used.")]
    public string displayName = "";
    public Sprite icon;
    [TextArea(4, 10)]
    public string shapeMask = "X";
    public List<string> tags = new List<string>();
    public List<StatAmount> baseStats = new List<StatAmount>();
    public List<AdjacencyRule> rules = new List<AdjacencyRule>();

    [SerializeField, HideInInspector]
    private string id;
    private PieceShape cachedShape;

    public string GetDisplayName()
    {
        if (string.IsNullOrEmpty(displayName)) return name;
        return displayName;
    }

    public PieceShape GetShape()
    {
        if (cachedShape == null) cachedShape = PieceShape.FromMask(shapeMask);
        return cachedShape;
    }

    public void RefreshShape() { cachedShape = null; }

    public bool HasTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return false;
        foreach (string mine in tags)
        {
            if (mine == tag) return true;
        }
        return false;
    }

    public string GetId()
    {
        if (string.IsNullOrEmpty(id)) id = MakeNewId();
        return id;
    }

    public ValidationResult Validate()
    {
        ValidationResult result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(shapeMask) || PieceShape.ParseMask(shapeMask).Count == 0)
            result.AddError("Shape Mask contains no filled squares. Give this piece at least one square.", this);

        if (baseStats == null)
        {
            result.AddError("Base Stats list is null.", this);
        }
        else
        {
            for (int i = 0; i < baseStats.Count; i++)
            {
                if (baseStats[i] == null)
                {
                    result.AddError("Base Stats has an empty entry at position " + i + ".", this);
                }
                else if (string.IsNullOrWhiteSpace(baseStats[i].statName))
                {
                    result.AddError("Base Stats entry " + i + " has no stat name.", this);
                }
            }
        }

        if (rules == null)
        {
            result.AddError("Rules list is null.", this);
        }
        else
        {
            for (int i = 0; i < rules.Count; i++)
            {
                AdjacencyRule rule = rules[i];
                if (rule == null)
                {
                    result.AddError("Rules has an empty entry at position " + i + ".", this);
                    continue;
                }

                if (rule.scope == AdjacencyRule.Scope.WithinDistance && rule.distance < 1)
                    result.AddError("Rule " + i + " uses WithinDistance but its distance is below 1.", this);

                if (string.IsNullOrWhiteSpace(rule.statName))
                    result.AddError("Rule " + i + " has no stat name.", this);
            }
        }

        return result;
    }

    [ContextMenu("Check For Problems")]
    public void CheckForProblems()
    {
        ValidationResult result = Validate();

        if (result.IsValid && result.Issues.Count == 0)
        {
            Debug.Log("[" + name + "] No problems.", this);
            return;
        }

        foreach (ValidationIssue issue in result.Issues)
        {
            if (issue.severity == ValidationIssue.Severity.Error)
                Debug.LogError("[" + name + "] " + issue.message, this);
            else
                Debug.LogWarning("[" + name + "] " + issue.message, this);
        }
    }

    private void OnEnable() { cachedShape = null; }

    protected virtual void OnValidate()
    {
        cachedShape = null;

        if (rules != null)
        {
            foreach (AdjacencyRule rule in rules)
            {
                if (rule != null && rule.distance < 1) rule.distance = 1;
            }
        }

        if (string.IsNullOrEmpty(id))
        {
            id = MakeNewId();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }

    [ContextMenu("Assign New Id")]
    private void AssignNewId()
    {
        id = MakeNewId();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log("Gave " + name + " a fresh ID. Any old save file loses this piece.");
    }

    [ContextMenu("Print Shape And Rotations")]
    private void PrintShape()
    {
        RefreshShape();
        PieceShape shape = GetShape();
        string output = GetDisplayName() + ": " + shape.CellCount + " squares, " + shape.RotationCount + " rotation(s)";

        for (int r = 0; r < shape.RotationCount; r++)
        {
            output += "\n\nrotation " + r + " (" + shape.GetWidth(r) + "x" + shape.GetHeight(r) + "):\n" + shape.ToMask(r);
        }

        Debug.Log(output, this);
    }

    private static string MakeNewId()
    {
        return System.Guid.NewGuid().ToString("N");
    }
}
