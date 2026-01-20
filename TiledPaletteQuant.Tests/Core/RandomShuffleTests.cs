using FluentAssertions;
using TiledPaletteQuant.Core;
using Xunit;

namespace TiledPaletteQuant.Tests.Core;

public class RandomShuffleTests
{
    [Fact]
    public void Next_ShouldReturnValidIndices()
    {
        // Arrange
        var shuffle = new RandomShuffle(10);
        var indices = new HashSet<int>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            int index = shuffle.Next();
            indices.Add(index);
        }

        // Assert
        indices.Should().HaveCount(10);
        indices.Should().OnlyContain(i => i >= 0 && i < 10);
    }

    [Fact]
    public void Next_ShouldReshuffleAfterFullCycle()
    {
        // Arrange
        var shuffle = new RandomShuffle(5);
        var firstCycle = new List<int>();
        var secondCycle = new List<int>();

        // Act
        for (int i = 0; i < 5; i++)
            firstCycle.Add(shuffle.Next());
        for (int i = 0; i < 5; i++)
            secondCycle.Add(shuffle.Next());

        // Assert
        firstCycle.Should().HaveCount(5);
        secondCycle.Should().HaveCount(5);
        firstCycle.Distinct().Should().HaveCount(5);
        secondCycle.Distinct().Should().HaveCount(5);
    }

    [Fact]
    public void Next_ShouldProvideUniformDistribution()
    {
        // Arrange
        var shuffle = new RandomShuffle(100);
        var counts = new int[100];

        // Act - Run many cycles
        for (int cycle = 0; cycle < 1000; cycle++)
        {
            for (int i = 0; i < 100; i++)
            {
                int index = shuffle.Next();
                counts[index]++;
            }
        }

        // Assert - Each index should appear roughly 1000 times
        // Allow for statistical variance
        counts.Should().AllSatisfy(count => count.Should().BeInRange(900, 1100));
    }
}
