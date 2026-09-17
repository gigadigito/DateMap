namespace DateMap.Web.Models;

public record MapEventModel(
    Guid Id,
    string Title,
    string EventDate,
    string? PlaceName,
    double Latitude,
    double Longitude,
    string? CategoryName,
    string? CategoryColor
);

public static class MapEventMapper
{
    private const string DefaultColor = "#3388ff";

    public static MapEventModel ToMapEvent(this EventDto ev, CategoryDto[] categories)
    {
        var cat = ev.CategoryId.HasValue ? categories.FirstOrDefault(c => c.Id == ev.CategoryId.Value) : null;
        return new MapEventModel(
            ev.Id,
            ev.Title,
            ev.EventDate.ToString("dd MMM yyyy \u2014 HH:mm"),
            ev.PlaceName,
            ev.Latitude ?? 0,
            ev.Longitude ?? 0,
            cat?.Name,
            string.IsNullOrEmpty(cat?.Color) ? DefaultColor : cat!.Color
        );
    }

    public static EventDto[] WithCoordinates(this EventDto[] events)
        => events.Where(e => e.Latitude.HasValue && e.Longitude.HasValue).ToArray();
}
