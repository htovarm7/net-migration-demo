namespace ModernWeb.Models;

// The model binder fills this in directly from the posted form dictionary
// (Request.Form) by matching property names - no manual parsing needed.
public class ContactForm
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
}
