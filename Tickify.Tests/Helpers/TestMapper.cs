using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Tickify.Service.Mapping;

namespace Tickify.Tests.Helpers;

/// <summary>The real AutoMapper configuration (all profiles of the Service assembly), for tests.</summary>
public static class TestMapper
{
    public static MapperConfiguration Configuration() =>
        new(cfg => cfg.AddMaps(typeof(EventProfile).Assembly), NullLoggerFactory.Instance);

    public static IMapper Create() => Configuration().CreateMapper();
}
