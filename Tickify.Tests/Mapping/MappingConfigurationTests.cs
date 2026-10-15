using AutoMapper;
using Tickify.Service.Mapping;

namespace Tickify.Tests.Mapping;

public class MappingConfigurationTests
{
    [Fact]
    public void AutoMapperConfiguration_IsValid()
    {
        var config = new MapperConfiguration(cfg => cfg.AddMaps(typeof(EventProfile).Assembly));

        config.AssertConfigurationIsValid();
    }
}
