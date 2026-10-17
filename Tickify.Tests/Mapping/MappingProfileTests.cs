using Tickify.Core.DTOs.Auth;
using Tickify.Core.DTOs.Events;
using Tickify.Core.Entities;
using Tickify.Core.Enums;
using Tickify.Tests.Helpers;

namespace Tickify.Tests.Mapping;

public class MappingProfileTests
{
    private readonly AutoMapper.IMapper _mapper = TestMapper.Create();

    [Fact]
    public void AutoMapperConfiguration_IsValid() =>
        TestMapper.Configuration().AssertConfigurationIsValid();

    [Fact]
    public void Event_MapsToResponse_WithCategoryNames()
    {
        var ev = new Event
        {
            Id = 7, Name = "Show", Venue = "Hall", TicketPrice = 50m,
            StartsAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EventCategories = { new EventCategory { Category = new Category { Name = "Music" } } }
        };

        var response = _mapper.Map<EventResponse>(ev);

        Assert.Equal(7, response.Id);
        Assert.Equal("Show", response.Name);
        Assert.Equal(50m, response.TicketPrice);
        Assert.Equal(new[] { "Music" }, response.Categories);
    }

    [Fact]
    public void UpdateEventRequest_ChangesDetailsOnly()
    {
        var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var ev = new Event { Id = 3, Name = "Old", CreatedAt = created };
        var request = new UpdateEventRequest
        {
            Name = "New", Venue = "V", Description = "D", TicketPrice = 10m, StartsAt = DateTime.UtcNow
        };

        _mapper.Map(request, ev);

        Assert.Equal("New", ev.Name);
        Assert.Equal(3, ev.Id);
        Assert.Equal(created, ev.CreatedAt);
    }

    [Fact]
    public void Order_MapsToResponse_WithAndWithoutLoadedEvent()
    {
        var order = new Order
        {
            Id = 1, EventId = 2, TotalPrice = 20m, SeatsSummary = "R1-S1", Status = OrderStatus.Active,
            Event = null!
        };

        Assert.Equal(string.Empty, _mapper.Map<Core.DTOs.Orders.OrderResponse>(order).EventName ?? string.Empty);

        order.Event = new Event { Name = "Show", StartsAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        var response = _mapper.Map<Core.DTOs.Orders.OrderResponse>(order);

        Assert.Equal("Show", response.EventName);
        Assert.Equal(order.Event.StartsAt, response.EventStartsAt);
    }

    [Fact]
    public void RegisterRequest_MapsToUser_WithoutPasswordHashOrRole()
    {
        var user = _mapper.Map<User>(new RegisterRequest { FullName = "A B", Email = "a@b.com", Password = "plain-secret" });

        Assert.Equal("A B", user.FullName);
        Assert.Equal(string.Empty, user.PasswordHash);   // set by AuthService after hashing
        Assert.DoesNotContain("plain-secret", user.PasswordHash);
    }

    [Fact]
    public void OrderResponse_ExposesThePurchaserNameButNoEmail()
    {
        var order = new Order { Id = 1, UserId = 9, User = new User { Id = 9, FullName = "Dana Cohen", Email = "dana@example.com" }, Event = new Event() };

        var response = _mapper.Map<Core.DTOs.Orders.OrderResponse>(order);

        Assert.Equal((9, "Dana Cohen"), (response.UserId, response.UserFullName));
        Assert.DoesNotContain(typeof(Core.DTOs.Orders.OrderResponse).GetProperties(), p => p.Name.Contains("Email", StringComparison.OrdinalIgnoreCase));
    }
}
