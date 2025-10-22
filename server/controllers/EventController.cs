using e_Vent.data;
using e_Vent.dtos;
using e_Vent.models;
using Microsoft.AspNetCore.Mvc;
using e_Vent.tools;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace e_Vent.controllers;

[ApiController]
[Route("events")]
public class EventController: ControllerBase
{
    #region Variables and Constructor
    private readonly ApplicationDbContext context;
    private readonly EmailService emailService;
    private readonly Token JwtToken;
    private readonly UserManager<EventManager> userManager;
    public EventController(ApplicationDbContext _context, Token _jwtToken, UserManager<EventManager> _userManager)
    {
        context = _context;
        JwtToken = _jwtToken;
        userManager = _userManager;
        emailService = new EmailService();
    }
    #endregion
    
    [HttpGet("all")]
    public async Task<IActionResult> GetAllEvents()
    {
        #region Authentication of User  
        var cookie = Request.Cookies["EventManagerToken"];
        if (string.IsNullOrEmpty(cookie))
            return Unauthorized(new { Status = "Error", Message = "User is not authenticated." });

        // Verify the token using the Token tool
        var principal = JwtToken.VerifyToken(cookie, out string userEmail);
        if (!principal)
        {
            return Unauthorized(new { Status = "Error", Message = userEmail });
        }

        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized(new { Status = "Error", Message = "Token does not contain user email." });

        var user = await userManager.FindByEmailAsync(userEmail);
        if (user == null)
        {
            return NotFound(new { Status = "Error", Message = "User not found." });
        }
        #endregion
        var events = context.Events.Where(s => s.EventManager == user.Id);
        return Ok(await events.ToListAsync());
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateEvent([FromBody] NewEventDto newEvent)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var cookie = Request.Cookies["EventManagerToken"];
        if (string.IsNullOrEmpty(cookie))
            return Unauthorized(new { Status = "Error", Message = "User is not authenticated." });

        // Verify the token using the Token tool
        var principal = JwtToken.VerifyToken(cookie, out string userEmail);
        if (!principal)
        {
            return Unauthorized(new { Status = "Error", Message = userEmail });
        }

        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized(new { Status = "Error", Message = "Token does not contain user email." });

        var user = await userManager.FindByEmailAsync(userEmail);
        if (user == null)
        {
            return NotFound(new { Status = "Error", Message = "User not found." });
        }
        context.Events.Add(new Event
        {
            EventName = newEvent.EventName,
            EventDate = newEvent.EventDate,
            EventDescription = newEvent.EventDescription,
            Location = newEvent.Location,
            EventManager = user.Id
        });
        context.SaveChanges();
        return Ok(new { Status = "Success", Message = "Event created successfully!" });
    }
    
    [HttpDelete("{eventId:int}")]
    public async Task<IActionResult> DeleteEvent([FromRoute] int eventId)
    {
        var cookie = Request.Cookies["EventManagerToken"];
        if (string.IsNullOrEmpty(cookie))
            return Unauthorized(new { Status = "Error", Message = "User is not authenticated." });

        var principal = JwtToken.VerifyToken(cookie, out string userEmail);
        if (!principal)
            return Unauthorized(new { Status = "Error", Message = userEmail });

        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized(new { Status = "Error", Message = "Token does not contain user email." });

        var user = await userManager.FindByEmailAsync(userEmail);
        if (user == null)
            return NotFound(new { Status = "Error", Message = "User not found." });

        var ev = await context.Events.FirstOrDefaultAsync(e => e.Id == eventId && e.EventManager == user.Id);
        if (ev == null)
            return NotFound(new { Status = "Error", Message = "Event not found or not authorized to delete." });

        context.Events.Remove(ev);
        var allFormEntries = context.GeneralForms.Where(s => s.EventId == ev.Id).AsQueryable();
        var deleteEntries = await allFormEntries.ToListAsync();
        context.GeneralForms.RemoveRange(deleteEntries);
        await context.SaveChangesAsync();

        return Ok(new { Status = "Success", Message = "Event deleted successfully!" });
    }
    
    [HttpPut("edit/{eventId:int}")]
    public async Task<IActionResult> EditEvent([FromRoute] int eventId, [FromBody] UpdateEventDto updatedEvent)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        #region token
        var cookie = Request.Cookies["EventManagerToken"];
        if (string.IsNullOrEmpty(cookie))
            return Unauthorized(new { Status = "Error", Message = "User is not authenticated." });

        var principal = JwtToken.VerifyToken(cookie, out string userEmail);
        if (!principal)
            return Unauthorized(new { Status = "Error", Message = userEmail });

        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized(new { Status = "Error", Message = "Token does not contain user email." });

        var user = await userManager.FindByEmailAsync(userEmail);
        if (user == null)
            return NotFound(new { Status = "Error", Message = "User not found." });

        var ev = await context.Events.FirstOrDefaultAsync(e => e.Id == eventId && e.EventManager == user.Id);
        if (ev == null)
            return NotFound(new { Status = "Error", Message = "Event not found or unauthorized." });
        #endregion

        #region Validate Form Description
        var formDetails = updatedEvent.FormDescription.Split("@", StringSplitOptions.RemoveEmptyEntries);

        int emailCount = 0;
        int dateCount = 0;
        HashSet<string> fieldNames = [];

        foreach (var item in formDetails)
        {
            var row = item.Split("->", StringSplitOptions.RemoveEmptyEntries);
            if (row.Length < 3)
                return BadRequest("Invalid form field format detected.");

            var fieldName = row[0].Trim();
            var fieldType = row[2].Trim().ToLower();

            if (!fieldNames.Add(fieldName))
                return BadRequest($"Duplicate field name '{fieldName}' is not allowed.");

            if (fieldType == "email")
                emailCount++;

            if (fieldType == "date")
                dateCount++;
        }

        if (emailCount != 1)
            return BadRequest("Your Form Details column must contain exactly one email field!");

        if (dateCount > 1)
            return BadRequest("Only one date field type is allowed");
        #endregion

        #region Data Mappings
        // Using Round-Robin Algorithm to safely load balance the text data on the database
        // There are 3 text fieldsand so we use modulo 3
        string dataMapping = "";
        string[] textCols = ["TF1", "TF2", "TF3"];
        int currentIndex = 0;

        foreach(var i in formDetails)
        {
            var row = i.Split("->");
            if (row[2] == "date")
            {
                dataMapping += $"DateData->{row[0]}@";
            }
            else if (row[2] == "text" || row[2] == "phone" || row[2] == "number")
            {
                dataMapping += $"{textCols[currentIndex]}->{row[0]}@";
                currentIndex = (currentIndex + 1) % 3;
            }
            else if (row[2] == "email")
            {
                dataMapping += $"UUID->{row[0]}@";
            }
        }
        if (dataMapping.EndsWith("@"))
            dataMapping = dataMapping.Substring(0, dataMapping.Length - 1);
        #endregion

        // Only live events can be updated...
        if (ev.isLive) return Ok(new { Status = "Success", Message = "Event is live and cannot be updated" });

        ev.EventName = updatedEvent.EventName;
        ev.EventDate = updatedEvent.EventDate;
        ev.Location = updatedEvent.Location;
        ev.EventDescription = updatedEvent.EventDescription;
        ev.FormDescription = updatedEvent.FormDescription;
        ev.DataMappings = dataMapping;

        await context.SaveChangesAsync();

        return Ok(new { Status = "Success", Message = "Event updated successfully!" });
    }

    [HttpPut("publish/{eventId:int}")]
    public async Task<IActionResult> PublishEvent([FromRoute] int eventId)
    {
        #region token
        var cookie = Request.Cookies["EventManagerToken"];
        if (string.IsNullOrEmpty(cookie))
            return Unauthorized(new { Status = "Error", Message = "User is not authenticated." });

        var principal = JwtToken.VerifyToken(cookie, out string userEmail);
        if (!principal)
            return Unauthorized(new { Status = "Error", Message = userEmail });

        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized(new { Status = "Error", Message = "Token does not contain user email." });

        var user = await userManager.FindByEmailAsync(userEmail);
        if (user == null)
            return NotFound(new { Status = "Error", Message = "User not found." });

        var ev = await context.Events.FirstOrDefaultAsync(e => e.Id == eventId && e.EventManager == user.Id);
        if (ev == null)
            return NotFound(new { Status = "Error", Message = "Event not found or unauthorized." });
        #endregion

        if (ev.isLive) return Ok(new { Status = "Success", Message = "Event is already live" });
        if (string.IsNullOrEmpty(ev.FormDescription) || string.IsNullOrEmpty(ev.DataMappings))
            return BadRequest("This form for this event has not been configured...");

        ev.isLive = true;
        await context.SaveChangesAsync();

        return Ok(new { Status = "Success", Message = "Your event is now live!!" });
    }

    [HttpGet("{eventId:int}")]
    public async Task<IActionResult> GetEventById([FromRoute] int eventId)
    {
        #region cookieVerification
        var cookie = Request.Cookies["EventManagerToken"];
        if (string.IsNullOrEmpty(cookie))
            return Unauthorized(new { Status = "Error", Message = "User is not authenticated." });

        var principal = JwtToken.VerifyToken(cookie, out string userEmail);
        if (!principal)
            return Unauthorized(new { Status = "Error", Message = userEmail });

        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized(new { Status = "Error", Message = "Token does not contain user email." });

        var user = await userManager.FindByEmailAsync(userEmail);
        if (user == null)
            return NotFound(new { Status = "Error", Message = "User not found." });

        var ev = await context.Events.FirstOrDefaultAsync(e => e.Id == eventId && e.EventManager == user.Id);
        if (ev == null)
            return NotFound(new { Status = "Error", Message = "Event not found or not owned by this user." });
        #endregion
        return Ok(new
        {
            ev.Id,
            ev.EventName,
            ev.EventDate,
            ev.Location,
            ev.EventDescription,
            ev.FormDescription,
            ev.isLive,
            user.UserName
        });
    }

    [HttpGet("form/{eventId:int}")]
    public async Task<IActionResult> GetEventFormDetails([FromRoute] int eventId)
    {
        var cookie = Request.Cookies["EventManagerToken"];
        if (string.IsNullOrEmpty(cookie))
            return Unauthorized(new { Status = "Error", Message = "User is not authenticated." });

        var principal = JwtToken.VerifyToken(cookie, out string userEmail);
        if (!principal)
            return Unauthorized(new { Status = "Error", Message = userEmail });

        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized(new { Status = "Error", Message = "Token does not contain user email." });

        var user = await userManager.FindByEmailAsync(userEmail);
        if (user == null)
            return NotFound(new { Status = "Error", Message = "User not found." });

        var e_Vent = await context.Events
            .FirstOrDefaultAsync(e => e.EventManager == user.Id && e.Id == eventId);

        if (e_Vent == null)
            return NotFound(new { Status = "Error", Message = "This event isn't yours or no longer exists" });

        var keys = e_Vent.DataMappings.Split("@").Select(e => e.Split("->")[1].ToString()).ToList();
        var mapKeys = e_Vent.DataMappings.Split("@").Select(e => e.Split("->")[0]);
        var values = await context.GeneralForms.Where(gF => gF.EventId == eventId).ToListAsync();

        List<List<string>> textCols = [];
        List<List<string>> output = [];

        for (int i = 0; i < values.Count; i++)
        {
            textCols.Add(new List<string>());

            var one = values[i].TextField1.Split("₦", StringSplitOptions.RemoveEmptyEntries).ToList();
            var two = values[i].TextField2.Split("₦", StringSplitOptions.RemoveEmptyEntries).ToList();
            var three = values[i].TextField3.Split("₦", StringSplitOptions.RemoveEmptyEntries).ToList();

            while (one.Count > 0 || two.Count > 0 || three.Count > 0)
            {
                if (one.Count > 0)
                {
                    textCols[i].Add(one[0]);
                    one.RemoveAt(0);
                }

                if (two.Count > 0)
                {
                    textCols[i].Add(two[0]);
                    two.RemoveAt(0);
                }

                if (three.Count > 0)
                {
                    textCols[i].Add(three[0]);
                    three.RemoveAt(0);
                }
            }
        }
        keys.Add("Checked In");
        output.Add(keys);

        for (int j = 0; j < values.Count; j++)
        {
            List<string> temp = [];

            foreach (var i in mapKeys)
            {
                if (i == "TF1" || i == "TF2" || i == "TF3")
                {
                    if (textCols[j].Count > 0)
                    {
                        temp.Add(textCols[j][0]);
                        textCols[j].RemoveAt(0);
                    }
                    else
                    {
                        temp.Add("");
                    }
                }
                else if (i == "DateData")
                {
                    temp.Add(values[j].DateData.ToString("dd-MM-yyyy"));
                }
                else if (i == "UUID")
                {
                    temp.Add(values[j].UUID);
                }
            }

            temp.Add(values[j].CheckedIn.ToString());

            output.Add(temp);
        }

        return Ok(output);
    }

    [HttpPost("submitForm")]
    public async Task<IActionResult> SubmitEventForm([FromBody] EventForm submittedValues)
    {
        try
        {
            string inviteeEmail = "";
            var ev = await context.Events.FindAsync(submittedValues.eventId);
            if (ev == null)
                return NotFound(new { Success = false, Message = "Event not found." });

            if (!ev.isLive)
                return BadRequest(new { Success = false, Message = "This event has not yet been published." });

            if (string.IsNullOrWhiteSpace(ev.FormDescription))
                return BadRequest(new { Success = false, Message = "This event has no defined form description." });

            var formFields = ev.FormDescription
                .Split('@', StringSplitOptions.RemoveEmptyEntries)
                .Select(f =>
                {
                    var parts = f.Split("->", StringSplitOptions.RemoveEmptyEntries);
                    Console.WriteLine($"{parts[0]}, {parts[1]}");
                    return new EventFieldDescription
                    {
                        Name = parts[0].Trim() ?? "",
                        Description = parts[1].Trim(),
                        Type = parts[2].Trim()?.ToLower() ?? "text"
                    };
                })
                .ToList();

            if (submittedValues.FormData.Count != formFields.Count)
                return BadRequest(new
                {
                    Success = false,
                    Message = $"Invalid number of form inputs. Expected {formFields.Count}, got {submittedValues.FormData.Count}."
                });

            var validationResults = new List<FieldValidationResult>();

            for (int i = 0; i < formFields.Count; i++)
            {
                var field = formFields[i];
                var value = submittedValues.FormData[i]?.Trim() ?? "";

                bool isValid = true;
                string? error = null;

                switch (field.Type)
                {
                    case "email":
                        if (!Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        {
                            isValid = false;
                            error = "Invalid email format.";
                        }
                        break;
                    case "phone":
                        if (!Regex.IsMatch(value, @"^\+?\d{7,15}$"))
                        {
                            isValid = false;
                            error = "Invalid phone number.";
                        }
                        break;
                    case "number":
                        if (!int.TryParse(value, out _))
                        {
                            isValid = false;
                            error = "Value must be a valid number.";
                        }
                        break;
                    case "date":
                        if (!DateTime.TryParse(value, out _))
                        {
                            isValid = false;
                            error = "Invalid date format.";
                        }
                        break;
                    default:
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            isValid = false;
                            error = "Text cannot be empty.";
                        }
                        break;
                }

                validationResults.Add(new FieldValidationResult
                {
                    Name = field.Name,
                    Type = field.Type,
                    Value = value,
                    IsValid = isValid,
                    Error = error
                });
            }

            if (validationResults.Any(v => !v.IsValid))
                return BadRequest(new
                {
                    Success = false,
                    Message = "Validation failed.",
                    ValidationResults = validationResults
                });

            // 🔹 Handle data mappings
            var mappings = (ev.DataMappings ?? "")
                .Split('@', StringSplitOptions.RemoveEmptyEntries)
                .Select(m => m.Split("->"))
                .ToList();

            var newGeneralForm = new GeneralForm
            {
                EventId = submittedValues.eventId
            };

            for (int i = 0; i < mappings.Count && i < validationResults.Count; i++)
            {
                var value = validationResults[i].Value;

                switch (mappings[i][0])
                {
                    case "TF1":
                        newGeneralForm.TextField1 += $"{value}₦";
                        break;
                    case "TF2":
                        newGeneralForm.TextField2 += $"{value}₦";
                        break;
                    case "TF3":
                        newGeneralForm.TextField3 += $"{value}₦";
                        break;
                    case "DateData":
                        newGeneralForm.DateData = DateTime.Parse(value);
                        break;
                    case "UUID":
                        bool exists = await context.GeneralForms.AnyAsync(c => c.EventId == ev.Id && c.UUID == value);
                        if (exists) return BadRequest(new { Message = "This email has already registered for this event" });
                        inviteeEmail = value;
                        newGeneralForm.UUID = value;
                        break;
                }
            }
            if (newGeneralForm.TextField1 != "")
            {
                if (newGeneralForm.TextField1.EndsWith("₦")) newGeneralForm.TextField1 = newGeneralForm.TextField1.Substring(0, newGeneralForm.TextField1.Length - 1);
            }
            if (newGeneralForm.TextField2 != "")
            {
                if (newGeneralForm.TextField2.EndsWith("₦")) newGeneralForm.TextField2 = newGeneralForm.TextField2.Substring(0, newGeneralForm.TextField2.Length - 1);
            }
            if (newGeneralForm.TextField3 != "")
            {
                if (newGeneralForm.TextField3.EndsWith("₦")) newGeneralForm.TextField3 = newGeneralForm.TextField3.Substring(0, newGeneralForm.TextField3.Length - 1);
            }
            context.GeneralForms.Add(newGeneralForm);
            await emailService.SendEmailWithQrAsync(inviteeEmail, $"Registration for the {ev.EventName} was successful", Environment.GetEnvironmentVariable("URL")! + $"/check?email={inviteeEmail}&id={ev.Id}");
            await context.SaveChangesAsync();

            return Ok(new
            {
                Success = true,
                Message = "Form submitted successfully!",
                ValidationResults = validationResults
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error submitting form: {ex.Message}");
            return StatusCode(500, new
            {
                Success = false,
                Message = "Server error while submitting form."
            });
        }
    }

    [HttpPost("{email}/{eventId:int}")]
    public async Task<IActionResult> CheckUserInOrOut([FromRoute] string email, [FromRoute] int eventId, [FromBody] bool isLeaving)
    {
        var eventInstance = await context.Events.FindAsync(eventId);

        if (eventInstance == null) return NotFound(new { message = "This event no longer exists..." });

        if (eventInstance.EventDate.DayOfYear > DateTime.Now.DayOfYear) return BadRequest(new { message = "Event Date hasn't reached..." });

        var entry = await context.GeneralForms.FirstOrDefaultAsync(h => h.EventId == eventId && h.UUID == email);

        if (entry == null)
            return NotFound(new { message = "User entry not found for this event." });

        if (isLeaving && entry.CheckedIn)
        {
            entry.CheckedIn = false; // Allow the person to go
        } else if (!isLeaving && !entry.CheckedIn) {
            entry.CheckedIn = true; // Allow the person to go on into the program
        } else {
            return NotFound(new { message = "Duplicate User" });
        }

        await context.SaveChangesAsync();

        return Ok(new { message = $"User {(entry.CheckedIn ? "checked in" : "checked out")} successfully" });
    }

    [HttpPost("send-mails/{eventId:int}")]
    public async Task<IActionResult> SendEmailsForEvent([FromRoute] int eventId, [FromBody] Message message)
    {
        Console.WriteLine(message.Subject);
        Console.WriteLine(message.Body);
        Console.WriteLine("LFGGGGG");
        if (message == null || string.IsNullOrWhiteSpace(message.Subject) || string.IsNullOrWhiteSpace(message.Body))
        {
            return BadRequest(new { Status = "Error", Message = "Message body or subject cannot be empty." });
        }

        // Get event by ID
        var eVent = await context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
        if (eVent == null)
        {
            return NotFound(new { Status = "Error", Message = "Event not found." });
        }

        // Get all registered users (assuming GeneralForms contains user entries)
        var registeredUsers = await context.GeneralForms
            .Where(gf => gf.EventId == eventId)
            .Select(gf => gf.UUID) // assuming UUID is the email
            .ToListAsync();

        if (registeredUsers == null || registeredUsers.Count == 0)
        {
            return NotFound(new { Status = "Error", Message = "No registered users found for this event." });
        }

        try
        {
            await emailService.SendAnEmailList(message, registeredUsers);
            return Ok(new
            {
                Status = "Success",
                Message = $"Emails sent successfully to {registeredUsers.Count} registered users."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Status = "Error",
                Message = "Failed to send emails.",
                Details = ex.Message
            });
        }
    }
}