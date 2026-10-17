using System.Reflection;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Tickify.API.Controllers;
using Tickify.Core.Interfaces.Repositories;

namespace Tickify.Tests.Architecture;

/// <summary>
/// Guards the structural rules of the assignment (layers, dependency direction, middleware order), so a later
/// change cannot silently break them. Needs no database.
/// </summary>
public class ArchitectureTests
{
    private static string RepoFile(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Tickify.sln")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(new[] { dir!.FullName }.Concat(parts).ToArray());
    }

    private static int IndexOf(string text, string needle)
    {
        var index = text.IndexOf(needle, StringComparison.Ordinal);
        Assert.True(index >= 0, $"Program.cs does not contain '{needle}'.");
        return index;
    }

    [Fact]
    public void Pipeline_ExceptionHandlingIsFirst_AndAuthenticationComesBeforeAuthorization()
    {
        var program = File.ReadAllText(RepoFile("Tickify.API", "Program.cs"));

        var exceptions = IndexOf(program, "UseMiddleware<ExceptionHandlingMiddleware>");
        var correlation = IndexOf(program, "UseMiddleware<CorrelationIdMiddleware>");
        var authentication = IndexOf(program, "app.UseAuthentication()");
        var authorization = IndexOf(program, "app.UseAuthorization()");

        Assert.True(exceptions < correlation, "Error handling must be the first middleware.");
        Assert.True(correlation < authentication);
        Assert.True(authentication < authorization, "Authentication must run before authorization.");
    }

    [Fact]
    public void Program_DoesNotTouchTheDbContextOrEfCore()
    {
        var program = File.ReadAllText(RepoFile("Tickify.API", "Program.cs"));

        Assert.DoesNotContain("AppDbContext", program);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", program);
    }

    [Fact]
    public void Core_HasNoPackageOrProjectReferences()
    {
        var csproj = XDocument.Load(RepoFile("Tickify.Core", "Tickify.Core.csproj"));

        Assert.Empty(csproj.Descendants("PackageReference"));
        Assert.Empty(csproj.Descendants("ProjectReference"));
    }

    [Fact]
    public void Service_DoesNotReferenceDataOrApi()
    {
        var csproj = XDocument.Load(RepoFile("Tickify.Service", "Tickify.Service.csproj"));
        var references = csproj.Descendants("ProjectReference").Select(r => (string?)r.Attribute("Include") ?? "").ToList();

        Assert.Equal(new[] { "Tickify.Core" }, references.Select(r => Path.GetFileNameWithoutExtension(r.Replace('\\', '/'))));
    }

    [Fact]
    public void Controllers_DependOnlyOnServiceInterfaces()
    {
        var controllers = typeof(ApiControllerBase).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t))
            .ToList();
        Assert.NotEmpty(controllers);

        foreach (var controller in controllers)
            foreach (var parameter in controller.GetConstructors().Single().GetParameters())
                Assert.True(
                    parameter.ParameterType.IsInterface && parameter.ParameterType.Namespace == "Tickify.Core.Interfaces.Services",
                    $"{controller.Name} injects {parameter.ParameterType.Name}; controllers may only use Core service interfaces.");
    }

    [Fact]
    public void EveryAsyncMethodInTheCoreInterfacesTakesACancellationToken()
    {
        var interfaces = typeof(IUnitOfWork).Assembly.GetTypes()
            .Where(t => t.IsInterface && t.Namespace!.StartsWith("Tickify.Core.Interfaces"));

        foreach (var method in interfaces.SelectMany(i => i.GetMethods()).Where(m => typeof(Task).IsAssignableFrom(m.ReturnType)))
            Assert.True(
                method.GetParameters().LastOrDefault()?.ParameterType == typeof(CancellationToken),
                $"{method.DeclaringType!.Name}.{method.Name} must end with a CancellationToken parameter.");
    }

    [Fact]
    public void ReadOnlyLookups_ExistNextToTheTrackedOnes()
    {
        // Display endpoints must not use the tracked "for update" queries (AsNoTracking on every read-only query).
        Assert.NotNull(typeof(ICategoryRepository).GetMethod(nameof(ICategoryRepository.GetByIdReadOnlyAsync)));
        Assert.NotNull(typeof(IOrderRepository).GetMethod(nameof(IOrderRepository.GetByIdWithDetailsAsync)));
        Assert.NotNull(typeof(IUserRepository).GetMethod(nameof(IUserRepository.GetByIdAsync)));
    }
}
