namespace JSL_SentinelPro.src.Models
{
    /// <summary>
    /// Plantilla de email.
    /// </summary>
    public class EmailTemplate
    {
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = false;
    }
}
