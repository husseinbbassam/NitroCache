# Contributing to NitroCache

Thank you for your interest in contributing to NitroCache!

## Development Setup

1. **Prerequisites**
   - .NET 9 SDK or later
   - Docker (for Redis)
   - Your favorite IDE (Visual Studio, VS Code, Rider)

2. **Clone and Setup**
   ```bash
   git clone https://github.com/husseinbbassam/NitroCache.git
   cd NitroCache
   ./start.sh  # or start.bat on Windows
   ```

3. **Build the Solution**
   ```bash
   dotnet build
   ```

## Project Structure

- **NitroCache.Library**: Core caching library with interfaces and implementations
- **NitroCache.Api**: Demo API showcasing the library usage
- **NitroCache.Benchmarks**: Performance benchmarks using BenchmarkDotNet

## Making Changes

1. Create a new branch for your feature/fix
2. Make your changes following the existing code style
3. Build and test your changes
4. Submit a pull request

## Code Style

- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and concise

## Testing

- Ensure all projects build successfully
- Test API endpoints manually or with the provided scripts
- Run benchmarks to verify performance improvements

## Pull Request Process

1. Update the README.md with details of changes if needed
2. Ensure your code builds without warnings
3. Update documentation as necessary
4. The PR will be reviewed and merged if approved

## Questions?

Open an issue for any questions or concerns.
