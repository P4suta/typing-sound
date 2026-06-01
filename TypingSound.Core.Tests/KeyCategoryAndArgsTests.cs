using TypingSound.Core.Abstractions;

namespace TypingSound.Core.Tests;

public class KeyCategoryAndArgsTests
{
    [Fact]
    public void ForEnterCarriesEnterCategory()
    {
        Assert.Equal(KeyCategory.Enter, KeyPressedEventArgs.For(KeyCategory.Enter).Category);
    }

    [Fact]
    public void ForOtherCarriesOtherCategory()
    {
        Assert.Equal(KeyCategory.Other, KeyPressedEventArgs.For(KeyCategory.Other).Category);
    }

    [Fact]
    public void SharedEnterCarriesEnterCategory()
    {
        Assert.Equal(KeyCategory.Enter, KeyPressedEventArgs.Enter.Category);
    }

    [Fact]
    public void SharedOtherCarriesOtherCategory()
    {
        Assert.Equal(KeyCategory.Other, KeyPressedEventArgs.Other.Category);
    }

    [Theory]
    [InlineData(KeyCategory.Other)]
    [InlineData(KeyCategory.Enter)]
    public void ConstructorReflectsGivenCategory(KeyCategory category)
    {
        Assert.Equal(category, new KeyPressedEventArgs(category).Category);
    }
}
