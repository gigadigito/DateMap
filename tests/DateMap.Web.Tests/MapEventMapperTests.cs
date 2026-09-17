using DateMap.Web.Models;
using WebEventDto = DateMap.Web.Models.EventDto;
using WebCategoryDto = DateMap.Web.Models.CategoryDto;

namespace DateMap.Web.Tests;

public sealed class MapEventMapperTests
{
    private static WebEventDto MakeEvent(double? lat = 1.0, double? lng = 2.0, Guid? catId = null)
        => new(Guid.NewGuid(), Guid.NewGuid(), catId, "title", "desc", DateTime.MinValue, null, lat, lng, DateTime.MinValue, DateTime.MinValue);

    [Fact]
    public void WithCoordinates_filters_events_without_latitude()
    {
        var withLat = MakeEvent(lat: 1.0, lng: 2.0);
        var noLat = MakeEvent(lat: null, lng: 2.0);
        var result = new[] { withLat, noLat }.WithCoordinates();
        Assert.Single(result);
        Assert.Equal(withLat.Id, result[0].Id);
    }

    [Fact]
    public void WithCoordinates_filters_events_without_longitude()
    {
        var withLng = MakeEvent(lat: 1.0, lng: 2.0);
        var noLng = MakeEvent(lat: 1.0, lng: null);
        var result = new[] { withLng, noLng }.WithCoordinates();
        Assert.Single(result);
        Assert.Equal(withLng.Id, result[0].Id);
    }

    [Fact]
    public void WithCoordinates_filters_events_without_both_coordinates()
    {
        var valid = MakeEvent(lat: 1.0, lng: 2.0);
        var neither = MakeEvent(lat: null, lng: null);
        var result = new[] { valid, neither }.WithCoordinates();
        Assert.Single(result);
    }

    [Fact]
    public void ToMapEvent_maps_fields_correctly()
    {
        var catId = Guid.NewGuid();
        var ev = MakeEvent(lat: 3.14, lng: 2.71, catId: catId);
        var categories = new[] { new WebCategoryDto(catId, "Food", null, "#ff0000", DateTime.MinValue, DateTime.MinValue) };

        var model = ev.ToMapEvent(categories);

        Assert.Equal(ev.Id, model.Id);
        Assert.Equal("Food", model.CategoryName);
        Assert.Equal("#ff0000", model.CategoryColor);
        Assert.Equal(3.14, model.Latitude);
        Assert.Equal(2.71, model.Longitude);
    }

    [Fact]
    public void ToMapEvent_uses_default_color_when_category_has_no_color()
    {
        var catId = Guid.NewGuid();
        var ev = MakeEvent(catId: catId);
        var categories = new[] { new WebCategoryDto(catId, "Misc", null, null, DateTime.MinValue, DateTime.MinValue) };

        var model = ev.ToMapEvent(categories);

        Assert.Equal("#3388ff", model.CategoryColor);
    }

    [Fact]
    public void ToMapEvent_uses_default_color_when_no_category()
    {
        var ev = MakeEvent(catId: null);
        var categories = Array.Empty<WebCategoryDto>();

        var model = ev.ToMapEvent(categories);

        Assert.Null(model.CategoryName);
        Assert.Equal("#3388ff", model.CategoryColor);
    }
}
