using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Tickify.Core.Common;
using Tickify.Core.DTOs.Orders;
using Tickify.Core.Entities;
using Tickify.Core.Enums;
using Tickify.Core.Interfaces.Repositories;
using Tickify.Data;
using Tickify.Data.Repositories;
using Tickify.Service.Services;
using Tickify.Tests.Helpers;

namespace Tickify.Tests.Integration;

[Trait("Category", "PostgreSQL")]
[Collection(IntegrationCollection.Name)]
public class ConcurrencyTests
{
    private const int EventId = 1; // seeded: "Summer Rock Night", 5 x 10 seats, ids 1..50
    private const int SeatId = 1;

    private readonly DatabaseFixture _fixture;

    public ConcurrencyTests(DatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task TwoContexts_UpdateSameSeat_SecondSaveThrowsDbUpdateConcurrencyException()
    {
        await _fixture.ResetAsync();

        using var ctx1 = _fixture.CreateContext();
        using var ctx2 = _fixture.CreateContext();

        // Both contexts read the same seat while it is still available.
        var seat1 = await ctx1.Seats.SingleAsync(s => s.Id == SeatId);
        var seat2 = await ctx2.Seats.SingleAsync(s => s.Id == SeatId);
        Assert.Equal(seat1.RowVersion, seat2.RowVersion);

        seat1.Status = SeatStatus.Sold;
        seat2.Status = SeatStatus.Sold;

        await ctx1.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => ctx2.SaveChangesAsync());

        using var verify = _fixture.CreateContext();
        var stored = await verify.Seats.AsNoTracking().SingleAsync(s => s.Id == SeatId);
        Assert.Equal(SeatStatus.Sold, stored.Status);
    }

    [Fact]
    public async Task TwoPurchasesSameSeat_OneSucceeds_OtherGetsConflict()
    {
        await _fixture.ResetAsync();

        using var ctx1 = _fixture.CreateContext();
        using var ctx2 = _fixture.CreateContext();

        // Make sure both services have read the seat before either of them saves.
        var rendezvous = new Rendezvous(2);
        var service1 = CreateOrderService(ctx1, rendezvous);
        var service2 = CreateOrderService(ctx2, rendezvous);

        var request = new CreateOrderRequest { EventId = EventId, SeatIds = new List<int> { SeatId } };

        var results = await Task.WhenAll(
            service1.PurchaseAsync(userId: 1, request, CancellationToken.None),
            service2.PurchaseAsync(userId: 2, request, CancellationToken.None));

        Assert.Single(results, r => r.IsSuccess);
        var loser = Assert.Single(results, r => !r.IsSuccess);
        Assert.Equal(ErrorType.Conflict, loser.ErrorType);
        Assert.Equal(OrderService.SeatConflictMessage, loser.Error);

        using var verify = _fixture.CreateContext();
        Assert.Equal(1, await verify.Orders.CountAsync());
        var seat = await verify.Seats.AsNoTracking().SingleAsync(s => s.Id == SeatId);
        Assert.Equal(SeatStatus.Sold, seat.Status);
        Assert.NotNull(seat.OrderId);
    }

    [Fact]
    public async Task CategoryNames_AreUniqueIgnoringCase()
    {
        using var db = _fixture.CreateContext();
        db.Categories.Add(new Category { Name = "MUSIC" }); // "Music" is seeded

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    private static OrderService CreateOrderService(AppDbContext ctx, Rendezvous rendezvous) => new(
        new OrderRepository(ctx),
        new EventRepository(ctx),
        new RendezvousSeatRepository(new SeatRepository(ctx), rendezvous),
        new UnitOfWork(ctx),
        TestMapper.Create(),
        NullLogger<OrderService>.Instance);

    /// <summary>Real repository, but waits after loading the seats so both reads happen before any save.</summary>
    private sealed class RendezvousSeatRepository : ISeatRepository
    {
        private readonly ISeatRepository _inner;
        private readonly Rendezvous _rendezvous;

        public RendezvousSeatRepository(ISeatRepository inner, Rendezvous rendezvous)
        {
            _inner = inner;
            _rendezvous = rendezvous;
        }

        public async Task<List<Seat>> GetByIdsForUpdateAsync(int eventId, IEnumerable<int> ids, CancellationToken ct)
        {
            var seats = await _inner.GetByIdsForUpdateAsync(eventId, ids, ct);
            await _rendezvous.ArriveAsync();
            return seats;
        }

        public Task<List<Seat>> GetByEventAsync(int eventId, CancellationToken ct) => _inner.GetByEventAsync(eventId, ct);
        public Task<Dictionary<int, int>> GetAvailableCountsAsync(IEnumerable<int> eventIds, CancellationToken ct) => _inner.GetAvailableCountsAsync(eventIds, ct);
        public Task<(int Rows, int SeatsPerRow)> GetLayoutAsync(int eventId, CancellationToken ct) => _inner.GetLayoutAsync(eventId, ct);
        public Task AddRangeAsync(IEnumerable<Seat> seats, CancellationToken ct) => _inner.AddRangeAsync(seats, ct);
    }
}
