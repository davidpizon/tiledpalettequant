using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiledPaletteQuant
{
    /// <summary>
    /// Provides efficient random shuffling for pixel selection.
    /// </summary>
    public class RandomShuffle
    {
        private readonly int[] _values;
        private int _currentIndex;
        private readonly Random _random = new Random();

        /// <summary>
        /// Initializes a new RandomShuffle with n elements.
        /// </summary>
        /// <param name="n">Number of elements.</param>
        public RandomShuffle(int n)
        {
            _values = new int[n];
            for (int i = 0; i < n; i++)
            {
                _values[i] = i;
            }
            _currentIndex = n - 1;
        }

        /// <summary>
        /// Shuffles the internal array using Fisher-Yates algorithm.
        /// </summary>
        private void Shuffle()
        {
            for (int i = 0; i < _values.Length; i++)
            {
                int index = i + _random.Next(_values.Length - i);
                (_values[i], _values[index]) = (_values[index], _values[i]);
            }
        }

        /// <summary>
        /// Gets the next random index, reshuffling when needed.
        /// </summary>
        /// <returns>A random index.</returns>
        public int Next()
        {
            _currentIndex++;
            if (_currentIndex >= _values.Length)
            {
                Shuffle();
                _currentIndex = 0;
            }
            return _values[_currentIndex];
        }
    }
}
