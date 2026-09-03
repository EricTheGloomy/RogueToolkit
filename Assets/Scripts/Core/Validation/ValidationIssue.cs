namespace RogueToolkit.Core.Validation
{
    /// <summary>
    /// Describes a single configuration or validation problem.
    /// </summary>
    public class ValidationIssue
    {
        public enum Severity
        {
            Warning,
            Error
        }

        public Severity severity { get; }
        public string message { get; }
        public object source { get; }

        public ValidationIssue(Severity severity, string message, object source = null)
        {
            this.severity = severity;
            this.message = message;
            this.source = source;
        }
    }
}
