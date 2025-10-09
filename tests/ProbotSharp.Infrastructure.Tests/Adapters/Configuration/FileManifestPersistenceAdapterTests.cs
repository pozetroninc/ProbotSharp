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

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
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
