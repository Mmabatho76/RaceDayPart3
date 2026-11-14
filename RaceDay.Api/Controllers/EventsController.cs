using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;


namespace RaceDay.Api.Controllers;

[Route("api/events")]
[Tags("3. Events")]
public class EventsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;


    // GET: api/events
    [HttpGet]
    public async Task<IActionResult> GetEvents(
        [FromQuery] string? type,
        [FromQuery] string? location,
        [FromQuery] DateTime? date,
        [FromQuery] bool includePast = false)
    {
        var query = _db.Events
            .AsNoTracking()
            .AsQueryable();

        if (date.HasValue)
        {
            var day = date.Value.Date;
            query = query.Where(e =>
                e.EventDate >= day &&
                e.EventDate < day.AddDays(1));
        }
        else if (!includePast)
        {
            var today = DateTime.Today;
            query = query.Where(e => e.EventDate >= today);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            var t = type.Trim().ToLower();
            query = query.Where(e =>
                e.EventType.TypeName.ToLower() == t);
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var l = location.Trim().ToLower();
            query = query.Where(e =>
                e.EventLocation.ToLower().Contains(l));
        }

        var events = await query
            .OrderBy(e => e.EventDate)
            .Select(EventResponse.FromEntity)
            .ToListAsync();

        foreach (var item in events)
        {
            item.BannerUrl = FindBannerUrl(item.EventId);
        }

        return Ok(events);
    }

    // GET: api/events/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEvent(int id)
    {
        var ev = await _db.Events
            .AsNoTracking()
            .Where(e => e.EventID == id)
            .Select(EventResponse.FromEntity)
            .FirstOrDefaultAsync();

        if (ev == null)
        {
            return NotFound(new ErrorResponse("Event not found."));
        }

        var categories = await _db.Categories
            .AsNoTracking()
            .Where(c => c.EventID == id)
            .OrderBy(c => c.CategoryID)
            .ToListAsync();

        ev.Categories = categories
            .Select(CategoryResponse.From)
            .ToList();

        ev.BannerUrl = FindBannerUrl(ev.EventId);

        return Ok(ev);
    }

    // POST: api/events
    [HttpPost]
    [SessionAuthorize(Roles = RoleNames.Organiser)]
    public async Task<IActionResult> CreateEvent(
        [FromBody] EventRequest request)
    {
        if (!await _db.EventTypes.AnyAsync(
            t => t.EventTypeID == request.EventTypeId))
        {
            return BadRequest(new ErrorResponse(
                "Invalid event type."));
        }

        var ev = new Event
        {
            EventName = request.Name.Trim(),
            Description = request.Description.Trim(),
            EventDate = request.EventDate!.Value,
            EventLocation = request.Location.Trim(),
            Distance = request.DistanceKm,
            EventTypeID = request.EventTypeId,
            UserID = CurrentUserId
        };

        _db.Events.Add(ev);

        await _db.SaveChangesAsync();

        var response = await _db.Events
            .AsNoTracking()
            .Where(e => e.EventID == ev.EventID)
            .Select(EventResponse.FromEntity)
            .FirstAsync();

        return CreatedAtAction(
            nameof(GetEvent),
            new { id = ev.EventID },
            response);
    }

    // PUT: api/events/5
    [HttpPut("{id:int}")]
    [SessionAuthorize(Roles = RoleNames.Organiser)]
    public async Task<IActionResult> UpdateEvent(
        int id,
        [FromBody] EventRequest request)
    {
        var ev = await _db.Events
            .FirstOrDefaultAsync(e => e.EventID == id);

        if (ev == null)
        {
            return NotFound(new ErrorResponse("Event not found."));
        }

        if (ev.UserID != CurrentUserId)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new ErrorResponse(
                    "You can only update your own events."));
        }

        if (!await _db.EventTypes.AnyAsync(
            t => t.EventTypeID == request.EventTypeId))
        {
            return BadRequest(new ErrorResponse(
                "Invalid event type."));
        }

        ev.EventName = request.Name.Trim();
        ev.Description = request.Description.Trim();
        ev.EventDate = request.EventDate!.Value;
        ev.EventLocation = request.Location.Trim();
        ev.Distance = request.DistanceKm;
        ev.EventTypeID = request.EventTypeId;

        await _db.SaveChangesAsync();

        var response = await _db.Events
            .AsNoTracking()
            .Where(e => e.EventID == id)
            .Select(EventResponse.FromEntity)
            .FirstAsync();

        return Ok(response);
    }

    // DELETE: api/events/5
    [HttpDelete("{id:int}")]
    [SessionAuthorize(Roles = RoleNames.Organiser)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _db.Events
            .FirstOrDefaultAsync(e => e.EventID == id);

        if (ev == null)
        {
            return NotFound(new ErrorResponse("Event not found."));
        }

        if (ev.UserID != CurrentUserId)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new ErrorResponse(
                    "You can only delete your own events."));
        }

        _db.Results.RemoveRange(
            _db.Results.Where(r => r.EventID == id));

        _db.Enrolments.RemoveRange(
            _db.Enrolments.Where(e => e.EventID == id));

        _db.Categories.RemoveRange(
            _db.Categories.Where(c => c.EventID == id));

        _db.Events.Remove(ev);

        await _db.SaveChangesAsync();

        var banner = FindBannerFile(id);

        if (banner != null && System.IO.File.Exists(banner))
        {
            System.IO.File.Delete(banner);
        }

        return Ok(new MessageResponse("Event deleted."));
    }

    // POST/PUT: api/events/5/banner
    [HttpPost("{id:int}/banner")]
    [HttpPut("{id:int}/banner")]
    [SessionAuthorize(Roles = RoleNames.Organiser)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadBanner(
        int id,
        IFormFile file)
    {
        var ev = await _db.Events
            .FirstOrDefaultAsync(e => e.EventID == id);

        if (ev == null)
        {
            return NotFound(new ErrorResponse("Event not found."));
        }

        if (ev.UserID != CurrentUserId)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new ErrorResponse(
                    "You can only upload banners for your own events."));
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new ErrorResponse(
                "Please select an image."));
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            return BadRequest(new ErrorResponse(
                "Image must be smaller than 5 MB."));
        }

        var extension = Path.GetExtension(file.FileName)
            .ToLowerInvariant();

        var allowedExtensions = new[]
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".webp"
        };

        if (!allowedExtensions.Contains(extension) ||
            !file.ContentType.StartsWith(
                "image/",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ErrorResponse(
                "Only JPG, JPEG, PNG, GIF and WEBP images are allowed."));
        }


        Directory.CreateDirectory(directory);

        var oldFile = FindBannerFile(id);

        if (oldFile != null &&
            System.IO.File.Exists(oldFile))
        {
            System.IO.File.Delete(oldFile);
        }

        var fileName = $"event-{id}{extension}";
        var filePath = Path.Combine(directory, fileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        var imageUrl =
            $"{Request.Scheme}://{Request.Host}/event-banners/{fileName}";

        return Ok(new { url = imageUrl });
    }

    // GET: api/events/mine
    [HttpGet("mine")]
    [SessionAuthorize(Roles = RoleNames.Organiser)]
    public async Task<IActionResult> GetMyEvents()
    {
        var userId = CurrentUserId;

        var events = await _db.Events
            .AsNoTracking()
            .Where(e => e.UserID == userId)
            .OrderBy(e => e.EventDate)
            .Select(EventResponse.FromEntity)
            .ToListAsync();

        var counts = await _db.Enrolments
            .AsNoTracking()
            .Where(e =>
                e.Event.UserID == userId &&
                e.Status != EnrolmentStatus.Cancelled)
            .GroupBy(e => e.EventID)
            .Select(g => new
            {
                EventId = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        foreach (var ev in events)
        {
            ev.EnrolmentCount =
                counts.FirstOrDefault(
                    c => c.EventId == ev.EventId)?.Count ?? 0;

            ev.BannerUrl = FindBannerUrl(ev.EventId);
        }

        return Ok(events);
    }

    // Finds the banner URL.
    private string? FindBannerUrl(int eventId)
    {
        var file = FindBannerFile(eventId);

        if (file == null)
        {
            return null;
        }

        return $"{Request.Scheme}://{Request.Host}/event-banners/{Path.GetFileName(file)}";
    }

    // Finds the locally stored banner file.
    private string? FindBannerFile(int eventId)
    {
       

        if (!Directory.Exists(directory))
        {
            return null;
        }

        return Directory.GetFiles(
            directory,
            $"event-{eventId}.*")
            .FirstOrDefault();
    }
}


