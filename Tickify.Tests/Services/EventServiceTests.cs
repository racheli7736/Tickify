using Moq;
using Tickify.Core.Common;
using Tickify.Core.DTOs.Common;
using Tickify.Core.DTOs.Events;
using Tickify.Core.Entities;
using Tickify.Core.Enums;
using Tickify.Core.Interfaces.Repositories;
using Tickify.Service.Services;
using Tickify.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Tickify.Tests.Services;

public class EventServiceTests
{
    private readonly Mock<IEventRepository> _events = new();
    private readonly Mock<ISeatRepository> _seats = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private EventService Sut() =>
        new(_events.Object, _seats.Object, _categories.Object, _unitOfWork.Object, TestMapper.Create(), NullLogger<EventService>.Instance);

    private static CreateEventRequest CreateRequest(DateTime? startsAt = null, int rows = 3, int perRow = 4, params int[] categoryIds) => new()
    {
        Name = "New show", Description = "d", Venue = "Hall", TicketPrice = 25m,
        StartsAt = startsAt ?? TestData.Future, Rows = rows, SeatsPerRow = perRow, CategoryIds = categoryIds.ToList()
    };

    private static UpdateEventRequest UpdateRequest(DateTime? startsAt = null, params int[] categoryIds) => new()
    {
        Name = "Renamed", Description = "d2", Venue = "Arena", TicketPrice = 70m,
        StartsAt = startsAt ?? TestData.Future, CategoryIds = categoryIds.ToList()
    };

    private void GivenCategoriesExist(params int[] ids) =>
        _categories.Setup(c => c.GetByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ids.Select(i => new Category { Id = i, Name = $"Cat{i}" }).ToList());

    // CreateAsync returns the saved event by reading it back through GetByIdAsync.
    private void GivenSavedEventCanBeReadBack()
    {
        _events.Setup(e => e.GetByIdWithDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(TestData.Event());
        _seats.Setup(s => s.GetAvailableCountsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<int, int>());
        _seats.Setup(s => s.GetLayoutAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((0, 0));
    }

    // ---------- Queries ----------

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetPagedAsync_UsersSeeUpcomingOnly_AdminsSeeEverything(bool isAdmin)
    {
        _events.Setup(e => e.GetPagedAsync(It.IsAny<EventQueryParameters>(), isAdmin, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Event> { Items = new List<Event>(), Page = 1, PageSize = 10, TotalCount = 0 });
        _seats.Setup(s => s.GetAvailableCountsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int>());

        var result = await Sut().GetPagedAsync(new EventQueryParameters(), isAdmin, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _events.Verify(e => e.GetPagedAsync(It.IsAny<EventQueryParameters>(), isAdmin, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPagedAsync_FillsAvailableSeatsFromOneGroupedQuery()
    {
        var items = new List<Event> { TestData.Event(1), TestData.Event(2) };
        _events.Setup(e => e.GetPagedAsync(It.IsAny<EventQueryParameters>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Event> { Items = items, Page = 1, PageSize = 10, TotalCount = 2 });
        _seats.Setup(s => s.GetAvailableCountsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int> { [1] = 12 }); // event 2 has no available seats

        var result = await Sut().GetPagedAsync(new EventQueryParameters(), false, CancellationToken.None);

        Assert.Equal(new[] { 12, 0 }, result.Value!.Items.Select(i => i.AvailableSeats));
        Assert.Equal(2, result.Value.TotalCount);
        _seats.Verify(s => s.GetAvailableCountsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownEvent_ReturnsNotFound()
    {
        _events.Setup(e => e.GetByIdWithDetailsAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync((Event?)null);

        var result = await Sut().GetByIdAsync(9, CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsSeatMapDimensionsAndAvailability()
    {
        _events.Setup(e => e.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(TestData.Event(1));
        _seats.Setup(s => s.GetAvailableCountsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int> { [1] = 30 });
        _seats.Setup(s => s.GetLayoutAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((5, 10));

        var result = await Sut().GetByIdAsync(1, CancellationToken.None);

        Assert.Equal((30, 5, 10), (result.Value!.AvailableSeats, result.Value.Rows, result.Value.SeatsPerRow));
    }

    [Fact]
    public async Task GetSeatsAsync_UnknownEvent_ReturnsNotFound()
    {
        _events.Setup(e => e.GetByIdWithDetailsAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync((Event?)null);

        var result = await Sut().GetSeatsAsync(9, CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _seats.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSeatsAsync_ReturnsTheSeatMap()
    {
        _events.Setup(e => e.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(TestData.Event(1));
        var seats = TestData.AvailableSeats(2);
        seats[1].Status = SeatStatus.Sold;
        _seats.Setup(s => s.GetByEventAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(seats);

        var result = await Sut().GetSeatsAsync(1, CancellationToken.None);

        Assert.Equal(new[] { SeatStatus.Available, SeatStatus.Sold }, result.Value!.Select(s => s.Status));
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_CreatesRowsTimesSeatsPerRowSeats()
    {
        GivenCategoriesExist(1);
        List<Seat>? created = null;
        _seats.Setup(s => s.AddRangeAsync(It.IsAny<IEnumerable<Seat>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<Seat>, CancellationToken>((seats, _) => created = seats.ToList())
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        GivenSavedEventCanBeReadBack();

        var result = await Sut().CreateAsync(CreateRequest(rows: 3, perRow: 4, categoryIds: new[] { 1 }), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(created);
        Assert.Equal(12, created!.Count);
        Assert.Equal(12, created.Select(s => (s.Row, s.Number)).Distinct().Count());
        Assert.All(created, s => Assert.Equal(SeatStatus.Available, s.Status));
        Assert.Equal(3, created.Max(s => s.Row));
        Assert.Equal(4, created.Max(s => s.Number));
        _events.Verify(e => e.AddAsync(It.Is<Event>(ev => ev.EventCategories.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_UnspecifiedDateKind_IsStoredAsUtc()
    {
        GivenCategoriesExist();
        Event? added = null;
        _events.Setup(e => e.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback<Event, CancellationToken>((ev, _) => added = ev).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        GivenSavedEventCanBeReadBack();
        var unspecified = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(10), DateTimeKind.Unspecified);

        await Sut().CreateAsync(CreateRequest(startsAt: unspecified), CancellationToken.None);

        Assert.Equal(DateTimeKind.Utc, added!.StartsAt.Kind);
    }

    [Fact]
    public async Task CreateAsync_PastDate_ReturnsValidationErrorAndSavesNothing()
    {
        var result = await Sut().CreateAsync(CreateRequest(startsAt: TestData.Past), CancellationToken.None);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _events.Verify(e => e.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UnknownCategory_ReturnsValidationError()
    {
        GivenCategoriesExist(1); // 1 exists, 2 does not

        var result = await Sut().CreateAsync(CreateRequest(categoryIds: new[] { 1, 2 }), CancellationToken.None);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_ChangesDetailsAndReplacesCategories_ButNotTheSeats()
    {
        var ev = TestData.Event(1);
        ev.EventCategories.Add(new EventCategory { EventId = 1, CategoryId = 1 });
        _events.Setup(e => e.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        GivenCategoriesExist(2);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await Sut().UpdateAsync(1, UpdateRequest(categoryIds: new[] { 2 }), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Renamed", ev.Name);
        Assert.Equal(70m, ev.TicketPrice);
        Assert.Equal(new[] { 2 }, ev.EventCategories.Select(c => c.CategoryId));
        _seats.VerifyNoOtherCalls();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UnknownEvent_ReturnsNotFound()
    {
        _events.Setup(e => e.GetByIdForUpdateAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync((Event?)null);

        var result = await Sut().UpdateAsync(9, UpdateRequest(), CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task UpdateAsync_PastDate_ReturnsValidationError()
    {
        _events.Setup(e => e.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(TestData.Event(1));

        var result = await Sut().UpdateAsync(1, UpdateRequest(startsAt: TestData.Past), CancellationToken.None);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_UnknownCategory_ReturnsValidationError()
    {
        _events.Setup(e => e.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(TestData.Event(1));
        GivenCategoriesExist();

        var result = await Sut().UpdateAsync(1, UpdateRequest(categoryIds: new[] { 5 }), CancellationToken.None);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_WithSoldSeats_ReturnsConflict()
    {
        var ev = TestData.Event(1);
        _events.Setup(e => e.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _events.Setup(e => e.HasSoldSeatsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await Sut().DeleteAsync(1, CancellationToken.None);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _events.Verify(e => e.Remove(It.IsAny<Event>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithOnlyCancelledOrders_ReturnsConflict()
    {
        // Orders reference events with a Restrict foreign key, so even cancelled ones block the delete.
        var ev = TestData.Event(1);
        _events.Setup(e => e.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _events.Setup(e => e.HasSoldSeatsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _events.Setup(e => e.HasOrdersAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await Sut().DeleteAsync(1, CancellationToken.None);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _events.Verify(e => e.Remove(It.IsAny<Event>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NoSales_RemovesTheEvent()
    {
        var ev = TestData.Event(1);
        _events.Setup(e => e.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await Sut().DeleteAsync(1, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _events.Verify(e => e.Remove(ev), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_UnknownEvent_ReturnsNotFound()
    {
        _events.Setup(e => e.GetByIdForUpdateAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync((Event?)null);

        var result = await Sut().DeleteAsync(9, CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task UpdateAsync_EventThatAlreadyHappened_CanStillEditDetailsWhenTheDateIsUnchanged()
    {
        var happened = TestData.Past;
        var ev = TestData.Event(1, startsAt: happened);
        _events.Setup(e => e.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        GivenCategoriesExist();
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await Sut().UpdateAsync(1, UpdateRequest(startsAt: happened), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Renamed", ev.Name);
    }

    [Fact]
    public async Task UpdateAsync_MovingAPastEventToAnotherPastDate_ReturnsValidationError()
    {
        var ev = TestData.Event(1, startsAt: TestData.Past);
        _events.Setup(e => e.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        var result = await Sut().UpdateAsync(1, UpdateRequest(startsAt: TestData.Past.AddDays(-5)), CancellationToken.None);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }
}
