namespace Funcky.Async.Test.Extensions.AsyncEnumerableExtensions;

public class ConcatToStringTest
{
    [Fact]
    public async Task ConcatenatingAnEmptySetOfStringsReturnsAnEmptyString()
    {
        var empty = AsyncEnumerable.Empty<string>();

        Assert.Equal(string.Empty, await empty.ConcatToString());
    }

    [Fact]
    public async Task ConcatenatingASetWithExactlyOneElementReturnsTheElement()
    {
        var singleElement = AsyncSequence.Return("Alpha");

        Assert.Equal("Alpha", await singleElement.ConcatToString());
    }

    [Fact]
    public async Task ConcatenatingAListOfStringsReturnsAllElementsWithoutASeparator()
    {
        var strings = AsyncSequence.Return("Alpha", "Beta", "Gamma");

        Assert.Equal("AlphaBetaGamma", await strings.ConcatToString());
    }

    [Fact]
    public async Task ConcatenatingNonStringsWorksToo()
    {
        var numbers = AsyncSequence.Return(1, 2, 3);

        Assert.Equal("123", await numbers.ConcatToString());
    }

    [Fact]
    public async Task NullsAreHandledAsEmptyStringsWhileConcatenating()
    {
        var strings = AsyncSequence.Return("Alpha", null, "Gamma");

        Assert.Equal("AlphaGamma", await strings.ConcatToString());
    }
}
