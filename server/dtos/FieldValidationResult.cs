namespace e_Vent.dtos;

public class FieldValidationResult
{
    public required string Name { get; set; }
    public required string Type { get; set; }
    public required string Value { get; set; }
    public bool IsValid { get; set; }
    public string? Error { get; set; }
}
