# TiledPaletteQuant Tests

Comprehensive test suite for the Tiled Palette Quantization Tool.

## Test Organization

### Core Tests (`Core/`)

- **ColorUtilsTests.cs**: Tests for color operations and conversions
  - Color distance calculations
  - n-bit quantization
  - Linear/sRGB conversions
  - Color operations (add, subtract, scale, clamp)
  
- **DitherEngineTests.cs**: Tests for dithering functionality
  - All 6 dither patterns
  - Error diffusion
  - Closest color selection
  - Position-based dithering
  
- **RandomShuffleTests.cs**: Tests for Fisher-Yates shuffle
  - Index validity
  - Cycle behavior
  - Distribution uniformity
  
- **PaletteSorterTests.cs**: Tests for palette optimization (if implemented)
  - Palette ordering
  - Color index mapping

### Models Tests (`Models/`)

- **QuantizationOptionsTests.cs**: Tests for configuration
  - Default values
  - Command-line argument parsing
  - Validation logic
  - All CLI options

### IO Tests (`IO/`)

- **BmpWriterTests.cs**: Tests for BMP file generation
  - BMP header structure
  - Palette encoding (BGR format)
  - Color index mapping
  - File format validation
  
- **ImageProcessorTests.cs**: Tests for image loading/saving (if needed)

### Integration Tests (`Integration/`)

- **EndToEndTests.cs**: Full pipeline tests
  - Load → Quantize → Save workflow
  - Various image types (solid, gradient, checkerboard)
  - Different dither modes
  - Different tile and palette configurations
  - Small and large images

## Running Tests

### Run All Tests

```bash
dotnet test
```

### Run with Verbose Output

```bash
dotnet test --logger "console;verbosity=detailed"
```

### Run Specific Test Category

```bash
# Run only Core tests
dotnet test --filter "FullyQualifiedName~TiledPaletteQuant.Tests.Core"

# Run only Integration tests
dotnet test --filter "FullyQualifiedName~TiledPaletteQuant.Tests.Integration"
```

### Run Single Test Class

```bash
dotnet test --filter "FullyQualifiedName~ColorUtilsTests"
```

### Run with Coverage (if coverage tools installed)

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

## Test Patterns

### Unit Tests

Unit tests focus on individual components in isolation:

- **Arrange**: Set up test data and dependencies
- **Act**: Execute the method under test
- **Assert**: Verify the result using FluentAssertions

Example:
```csharp
[Fact]
public void ColorDistance_ShouldCalculateWeightedDistance()
{
    // Arrange
    var color1 = new double[] { 100, 150, 200 };
    var color2 = new double[] { 120, 140, 190 };

    // Act
    double distance = ColorUtils.ColorDistance(color1, color2);

    // Assert
    distance.Should().Be(1300);
}
```

### Integration Tests

Integration tests verify the full pipeline:

- Create test images programmatically
- Run the full quantization process
- Validate output format and correctness
- Test with various configurations

### Test Data

Test images are generated programmatically:
- **Solid colors**: Simple single-color images
- **Gradients**: Smooth color transitions
- **Checkerboard**: High-frequency patterns
- **Real images**: Via temporary PNG files

## Dependencies

- **xUnit**: Test framework
- **FluentAssertions**: Fluent assertion library for readable tests
- **Moq**: Mocking framework (if needed)
- **SixLabors.ImageSharp**: Image processing for test data

## Coverage Goals

Target coverage by component:
- **Models**: 100% (simple data classes)
- **Core/ColorUtils**: 100% (pure functions)
- **Core/DitherEngine**: 90%+
- **Core/TiledPaletteQuantizer**: 80%+
- **IO**: 80%+
- **Overall**: 80%+

## Adding New Tests

When adding new tests:

1. Follow existing patterns (Arrange-Act-Assert)
2. Use FluentAssertions for readable assertions
3. Name tests descriptively: `MethodName_ShouldExpectedBehavior`
4. Test both happy paths and edge cases
5. Keep tests fast (<100ms per test)
6. Use `[Theory]` for parameterized tests

Example:
```csharp
[Theory]
[InlineData(0, 5, 0)]
[InlineData(255, 5, 255)]
[InlineData(128, 5, 127.5)]
public void ToNbit_ShouldQuantizeCorrectly(double value, int n, double expected)
{
    double result = ColorUtils.ToNbit(value, n);
    result.Should().BeApproximately(expected, 0.5);
}
```

## Continuous Integration

Tests are designed to run in CI environments:
- No external dependencies
- Deterministic results
- Fast execution
- Clean up temporary files

## Troubleshooting

### Tests Timeout
- Reduce `FractionOfPixels` in integration tests
- Use smaller test images
- Check for infinite loops

### Tests Fail on CI but Pass Locally
- Check for file system issues
- Verify temp directory permissions
- Look for timing-dependent tests

### Flaky Tests
- Check for random number generation without fixed seeds
- Verify no shared state between tests
- Ensure proper cleanup in `finally` blocks

## Contributing

When contributing tests:
- Ensure all tests pass before submitting
- Add tests for new features
- Update existing tests when changing behavior
- Keep test coverage above 80%
