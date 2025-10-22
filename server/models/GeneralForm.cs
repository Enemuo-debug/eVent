namespace e_Vent.models;

public class GeneralForm
{
    // Primary Key
    public int Id { get; set; }
    // Using a kind of ECMP Load Balancing to hold Textual Data
    public string TextField1 { get; set; } = string.Empty;
    public string TextField2 { get; set; } = string.Empty;
    public string TextField3 { get; set; } = string.Empty;

    // Date field
    public DateTime DateData { get; set; }

    // Unique Data Field
    public string UUID { get; set; }

    // For checking users in and out on the program
    public bool CheckedIn { get; set; } = false;

    // Foreign Key
    public int EventId { get; set; }
}