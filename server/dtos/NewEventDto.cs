using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace e_Vent.dtos
{
    public class NewEventDto : IValidatableObject
    {
        [Required]
        public required string EventName { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        public required string Location { get; set; }

        public string EventDescription { get; set; } = "...";

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EventDate <= DateTime.Today)
            {
                yield return new ValidationResult(
                    "Event date must be in the future.",
                    new[] { nameof(EventDate) }
                );
            }
        }
    }
}