namespace e_Vent.models;

public class Event
{
    public int Id { get; set; }
    public required string EventManager { get; set; }
    public required string EventName { get; set; }
    public DateTime EventDate { get; set; } = DateTime.Now;
    public required string Location { get; set; }
    public string EventDescription { get; set; } = "...";
    public string FormDescription { get; set; } = string.Empty;
    public string DataMappings { get; set; } = string.Empty;
    public bool isLive { get; set; } = false;
}
