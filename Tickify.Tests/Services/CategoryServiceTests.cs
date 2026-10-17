using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Tickify.Core.Common;
using Tickify.Core.DTOs.Categories;
using Tickify.Core.Entities;
using Tickify.Core.Interfaces.Repositories;
using Tickify.Service.Services;
using Tickify.Tests.Helpers;

namespace Tickify.Tests.Services;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CategoryService Sut() => new(_categories.Object, _unitOfWork.Object, TestMapper.Create(), NullLogger<CategoryService>.Instance);

    private void VerifyNothingSaved() =>
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

    [Fact]
    public async Task GetAllAsync_ReturnsAllCategories()
    {
        _categories.Setup(c => c.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category> { new() { Id = 1, Name = "Music" }, new() { Id = 2, Name = "Comedy" } });

        var result = await Sut().GetAllAsync(CancellationToken.None);

        Assert.Equal(new[] { "Music", "Comedy" }, result.Value!.Select(c => c.Name));
    }

    [Fact]
    public async Task CreateAsync_NewName_SavesTheTrimmedName()
    {
        Category? added = null;
        _categories.Setup(c => c.NameExistsAsync("Dance", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _categories.Setup(c => c.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .Callback<Category, CancellationToken>((c, _) => added = c).Returns(Task.CompletedTask);

        var result = await Sut().CreateAsync(new CategoryRequest { Name = "  Dance " }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Dance", added!.Name);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ReturnsConflict()
    {
        _categories.Setup(c => c.NameExistsAsync("Music", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await Sut().CreateAsync(new CategoryRequest { Name = "Music" }, CancellationToken.None);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task UpdateAsync_RenamesTheCategory_IgnoringItselfInTheUniquenessCheck()
    {
        var category = new Category { Id = 4, Name = "Old" };
        _categories.Setup(c => c.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _categories.Setup(c => c.NameExistsAsync("New", 4, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await Sut().UpdateAsync(4, new CategoryRequest { Name = "New" }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("New", category.Name);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NameUsedByAnotherCategory_ReturnsConflict()
    {
        var category = new Category { Id = 4, Name = "Old" };
        _categories.Setup(c => c.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _categories.Setup(c => c.NameExistsAsync("Music", 4, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await Sut().UpdateAsync(4, new CategoryRequest { Name = "Music" }, CancellationToken.None);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal("Old", category.Name);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task UpdateAsync_UnknownCategory_ReturnsNotFound()
    {
        _categories.Setup(c => c.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);

        var result = await Sut().UpdateAsync(9, new CategoryRequest { Name = "X" }, CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task DeleteAsync_CategoryAssignedToEvents_ReturnsConflict()
    {
        _categories.Setup(c => c.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Category { Id = 1, Name = "Music" });
        _categories.Setup(c => c.HasEventsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await Sut().DeleteAsync(1, CancellationToken.None);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _categories.Verify(c => c.Remove(It.IsAny<Category>()), Times.Never);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task DeleteAsync_UnusedCategory_RemovesIt()
    {
        var category = new Category { Id = 1, Name = "Music" };
        _categories.Setup(c => c.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _categories.Setup(c => c.HasEventsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await Sut().DeleteAsync(1, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _categories.Verify(c => c.Remove(category), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_UnknownCategory_ReturnsNotFound()
    {
        _categories.Setup(c => c.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);

        var result = await Sut().DeleteAsync(9, CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
