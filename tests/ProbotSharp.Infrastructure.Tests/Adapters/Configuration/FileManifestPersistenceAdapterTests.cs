// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ProbotSharp.Infrastructure.Adapters.Configuration;

namespace ProbotSharp.Infrastructure.Tests.Adapters.Configuration;

public sealed class FileManifestPersistenceAdapterTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly FileManifestPersistenceAdapter _sut;

    public FileManifestPersistenceAdapterTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ProbotSharp:ManifestPath"] = Path.Combine(_tempDirectory, "manifest.json")
            })
            .Build();

        var environment = new TestHostEnvironment(_tempDirectory);
        var logger = Substitute.For<ILogger<FileManifestPersistenceAdapter>>();

        _sut = new FileManifestPersistenceAdapter(configuration, environment, logger);
    }

    [Fact]
    public async Task SaveAsync_ThenGetAsync_ShouldRoundTripManifest()
    {
        const string manifestJson = "{\"name\":\"probot\"}";

        var saveResult = await _sut.SaveAsync(manifestJson);
        var getResult = await _sut.GetAsync();

        saveResult.IsSuccess.Should().BeTrue();
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Should().Be(manifestJson);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenManifestMissing()
    {
        var result = await _sut.GetAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_WithNullManifest_ShouldThrow()
    {
        var act = async () => await _sut.SaveAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SaveAsync_WithEmptyManifest_ShouldThrow()
    {
        var act = async () => await _sut.SaveAsync(string.Empty);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SaveAsync_ShouldCreateDirectory_WhenDirectoryMissing()
    {
        var nestedPath = Path.Combine(_tempDirectory, "nested", "deep", "manifest.json");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ProbotSharp:ManifestPath"] = nestedPath
            })
            .Build();

        var environment = new TestHostEnvironment(_tempDirectory);
        var logger = Substitute.For<ILogger<FileManifestPersistenceAdapter>>();
        var adapter = new FileManifestPersistenceAdapter(configuration, environment, logger);

        var result = await adapter.SaveAsync("{\"test\":\"data\"}");

        result.IsSuccess.Should().BeTrue();
        File.Exists(nestedPath).Should().BeTrue();
    }

    [Fact]
    public async Task Constructor_WithNoConfiguration_ShouldUseDefaultPath()
    {
        var configuration = new ConfigurationBuilder().Build();
        var environment = new TestHostEnvironment(_tempDirectory);
        var logger = Substitute.For<ILogger<FileManifestPersistenceAdapter>>();

        var adapter = new FileManifestPersistenceAdapter(configuration, environment, logger);
        var result = await adapter.SaveAsync("{\"test\":\"default\"}");

        result.IsSuccess.Should().BeTrue();
        var defaultPath = Path.Combine(_tempDirectory, "storage", "manifest.json");
        File.Exists(defaultPath).Should().BeTrue();
    }

    [Fact]
    public async Task Constructor_WithManifestPathConfig_ShouldUseConfiguredPath()
    {
        var customPath = Path.Combine(_tempDirectory, "custom-manifest.json");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Manifest:Path"] = customPath
            })
            .Build();

        var environment = new TestHostEnvironment(_tempDirectory);
        var logger = Substitute.For<ILogger<FileManifestPersistenceAdapter>>();

        var adapter = new FileManifestPersistenceAdapter(configuration, environment, logger);
        var result = await adapter.SaveAsync("{\"test\":\"custom\"}");

        result.IsSuccess.Should().BeTrue();
        File.Exists(customPath).Should().BeTrue();
    }

    [Fact]
    public async Task GetAsync_WithReadOnlyFile_ShouldReturnSuccess()
    {
        const string manifestJson = "{\"readonly\":\"test\"}";
        await _sut.SaveAsync(manifestJson);

        var manifestPath = Path.Combine(_tempDirectory, "manifest.json");
        var fileInfo = new FileInfo(manifestPath);
        fileInfo.IsReadOnly = true;

        try
        {
            var result = await _sut.GetAsync();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(manifestJson);
        }
        finally
        {
            fileInfo.IsReadOnly = false;
        }
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrow()
    {
        var environment = new TestHostEnvironment(_tempDirectory);
        var logger = Substitute.For<ILogger<FileManifestPersistenceAdapter>>();

        var act = () => new FileManifestPersistenceAdapter(null!, environment, logger);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullEnvironment_ShouldThrow()
    {
        var configuration = new ConfigurationBuilder().Build();
        var logger = Substitute.For<ILogger<FileManifestPersistenceAdapter>>();

        var act = () => new FileManifestPersistenceAdapter(configuration, null!, logger);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        var configuration = new ConfigurationBuilder().Build();
        var environment = new TestHostEnvironment(_tempDirectory);

        var act = () => new FileManifestPersistenceAdapter(configuration, environment, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                var files = Directory.GetFiles(_tempDirectory, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.IsReadOnly)
                    {
                        fileInfo.IsReadOnly = false;
                    }
                }

                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch
            {
                // Cleanup best effort
            }
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
        }

        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "ProbotSharp.Tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
