using System.Collections.Generic;

namespace RogueToolkit.Core.Validation
{
    /// <summary>
    /// Collects validation issues produced while checking configuration.
    /// </summary>
    public class ValidationResult
    {
        private readonly List<ValidationIssue> issues = new();

        public IReadOnlyList<ValidationIssue> Issues => issues;
        public bool IsValid => !HasErrors;
        public bool HasErrors
        {
            get
            {
                foreach (ValidationIssue issue in issues)
                {
                    if (issue.severity == ValidationIssue.Severity.Error)
                        return true;
                }

                return false;
            }
        }

        public void AddError(string message, object source = null)
        {
            issues.Add(new ValidationIssue(
                ValidationIssue.Severity.Error,
                message,
                source));
        }

        public void AddWarning(string message, object source = null)
        {
            issues.Add(new ValidationIssue(
                ValidationIssue.Severity.Warning,
                message,
                source));
        }
    }
}
