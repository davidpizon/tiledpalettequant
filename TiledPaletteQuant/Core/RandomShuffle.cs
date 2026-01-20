namespace TiledPaletteQuant.Core;

/// <summary>
/// Implements circular Fisher-Yates shuffle for randomized iteration.
/// Automatically reshuffles when all elements have been iterated.
/// </summary>
public class RandomShuffle
{
    private readonly int _count;
    private readonly int[] _indices;
    private readonly Random _random;
    private int _currentIndex;

    /// <summary>
    /// Initializes a new RandomShuffle with the specified count.
    /// </summary>
    public RandomShuffle(int count)
    {
        _count = count;
        _indices = new int[count];
        _random = new Random();
        _currentIndex = 0;

        // Initialize indices
        for (int i = 0; i < count; i++)
        {
            _indices[i] = i;
        }

        Shuffle();
    }

    /// <summary>
    /// Gets the next shuffled index and reshuffles if needed.
    /// </summary>
    public int Next()
    {
        // From worker.js lines 731-755
        if (_currentIndex >= _count)
        {
            _currentIndex = 0;
            Shuffle();
        }

        return _indices[_currentIndex++];
    }

    /// <summary>
    /// Performs Fisher-Yates shuffle on the indices array.
    /// </summary>
    private void Shuffle()
    {
        // Fisher-Yates shuffle
        for (int i = _count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (_indices[i], _indices[j]) = (_indices[j], _indices[i]);
        }
    }
}
