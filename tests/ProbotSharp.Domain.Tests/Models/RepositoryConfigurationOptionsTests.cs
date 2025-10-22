// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Models;

namespace ProbotSharp.Domain.Tests.Models;

public class RepositoryConfigurationOptionsTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var options = new RepositoryConfigurationOptions();

        // Assert
        options.DefaultFileName.Should().Be("config.yml");
        options.EnableOrganizationConfig.Should().BeTrue();
        options.EnableGitHubDirectoryCascade.Should().BeTrue();
        options.EnableExtendsKey.Should().BeTrue();
        options.CacheTtl.Should().Be(TimeSpan.FromMinutes(5));
        options.ArrayMergeStrategy.Should().Be(ArrayMergeStrategy.Replace);
        options.MaxExtendsDepth.Should().Be(5);
    }

    [Fact]
    public void DefaultFileName_ShouldBeSettable()
    {
        // Arrange
        var customFileName = "custom-config.yaml";

        // Act
        var options = new RepositoryConfigurationOptions
        {
            DefaultFileName = customFileName
        };

        // Assert
        options.DefaultFileName.Should().Be(customFileName);
    }

    [Fact]
    public void EnableOrganizationConfig_ShouldBeSettableToFalse()
    {
        // Arrange & Act
        var options = new RepositoryConfigurationOptions
        {
            EnableOrganizationConfig = false
        };

        // Assert
        options.EnableOrganizationConfig.Should().BeFalse();
    }

    [Fact]
    public void EnableGitHubDirectoryCascade_ShouldBeSettableToFalse()
    {
        // Arrange & Act
        var options = new RepositoryConfigurationOptions
        {
            EnableGitHubDirectoryCascade = false
        };

        // Assert
        options.EnableGitHubDirectoryCascade.Should().BeFalse();
    }

    [Fact]
    public void EnableExtendsKey_ShouldBeSettableToFalse()
    {
        // Arrange & Act
        var options = new RepositoryConfigurationOptions
        {
            EnableExtendsKey = false
        };

        // Assert
        options.EnableExtendsKey.Should().BeFalse();
    }

    [Fact]
    public void CacheTtl_ShouldBeSettableToCustomDuration()
    {
        // Arrange
        var customTtl = TimeSpan.FromMinutes(10);

        // Act
        var options = new RepositoryConfigurationOptions
        {
            CacheTtl = customTtl
        };

        // Assert
        options.CacheTtl.Should().Be(customTtl);
    }

    [Fact]
    public void CacheTtl_ShouldSupportZeroDuration()
    {
        // Arrange & Act
        var options = new RepositoryConfigurationOptions
        {
            CacheTtl = TimeSpan.Zero
        };

        // Assert
        options.CacheTtl.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void CacheTtl_ShouldSupportLongDuration()
    {
        // Arrange
        var longTtl = TimeSpan.FromHours(24);

        // Act
        var options = new RepositoryConfigurationOptions
        {
            CacheTtl = longTtl
        };

        // Assert
        options.CacheTtl.Should().Be(longTtl);
    }

    [Fact]
    public void ArrayMergeStrategy_ShouldBeSettableToConcatenate()
    {
        // Arrange & Act
        var options = new RepositoryConfigurationOptions
        {
            ArrayMergeStrategy = ArrayMergeStrategy.Concatenate
        };

        // Assert
        options.ArrayMergeStrategy.Should().Be(ArrayMergeStrategy.Concatenate);
    }

    [Fact]
    public void ArrayMergeStrategy_ShouldBeSettableToDeepMergeByIndex()
    {
        // Arrange & Act
        var options = new RepositoryConfigurationOptions
        {
            ArrayMergeStrategy = ArrayMergeStrategy.DeepMergeByIndex
        };

        // Assert
        options.ArrayMergeStrategy.Should().Be(ArrayMergeStrategy.DeepMergeByIndex);
    }

    [Fact]
    public void MaxExtendsDepth_ShouldBeSettableToCustomValue()
    {
        // Arrange
        var customDepth = 10;

        // Act
        var options = new RepositoryConfigurationOptions
        {
            MaxExtendsDepth = customDepth
        };

        // Assert
        options.MaxExtendsDepth.Should().Be(customDepth);
    }

    [Fact]
    public void MaxExtendsDepth_ShouldSupportZero()
    {
        // Arrange & Act
        var options = new RepositoryConfigurationOptions
        {
            MaxExtendsDepth = 0
        };

        // Assert
        options.MaxExtendsDepth.Should().Be(0);
    }

    [Fact]
    public void MaxExtendsDepth_ShouldSupportLargeValue()
    {
        // Arrange & Act
        var options = new RepositoryConfigurationOptions
        {
            MaxExtendsDepth = 100
        };

        // Assert
        options.MaxExtendsDepth.Should().Be(100);
    }

    [Fact]
    public void Default_ShouldProvideDefaultInstance()
    {
        // Act
        var defaultOptions = RepositoryConfigurationOptions.Default;

        // Assert
        defaultOptions.Should().NotBeNull();
        defaultOptions.DefaultFileName.Should().Be("config.yml");
        defaultOptions.EnableOrganizationConfig.Should().BeTrue();
        defaultOptions.EnableGitHubDirectoryCascade.Should().BeTrue();
        defaultOptions.EnableExtendsKey.Should().BeTrue();
        defaultOptions.CacheTtl.Should().Be(TimeSpan.FromMinutes(5));
        defaultOptions.ArrayMergeStrategy.Should().Be(ArrayMergeStrategy.Replace);
        defaultOptions.MaxExtendsDepth.Should().Be(5);
    }

    [Fact]
    public void Default_ShouldBeReadOnly()
    {
        // Arrange
        var default1 = RepositoryConfigurationOptions.Default;
        var default2 = RepositoryConfigurationOptions.Default;

        // Assert - Same instance
        default1.Should().BeSameAs(default2);
    }

    [Fact]
    public void ObjectInitializer_ShouldSetAllProperties()
    {
        // Arrange & Act
        var options = new RepositoryConfigurationOptions
        {
            DefaultFileName = "app.config.yml",
            EnableOrganizationConfig = false,
            EnableGitHubDirectoryCascade = false,
            EnableExtendsKey = false,
            CacheTtl = TimeSpan.FromMinutes(15),
            ArrayMergeStrategy = ArrayMergeStrategy.Concatenate,
            MaxExtendsDepth = 3
        };

        // Assert
        options.DefaultFileName.Should().Be("app.config.yml");
        options.EnableOrganizationConfig.Should().BeFalse();
        options.EnableGitHubDirectoryCascade.Should().BeFalse();
        options.EnableExtendsKey.Should().BeFalse();
        options.CacheTtl.Should().Be(TimeSpan.FromMinutes(15));
        options.ArrayMergeStrategy.Should().Be(ArrayMergeStrategy.Concatenate);
        options.MaxExtendsDepth.Should().Be(3);
    }

    [Theory]
    [InlineData("config.yml")]
    [InlineData("config.yaml")]
    [InlineData(".github/bot.yml")]
    [InlineData("settings.json")]
    public void DefaultFileName_ShouldSupportVariousFileNames(string fileName)
    {
        // Arrange & Act
        var options = new RepositoryConfigurationOptions
        {
            DefaultFileName = fileName
        };

        // Assert
        options.DefaultFileName.Should().Be(fileName);
    }

    [Theory]
    [InlineData(true, true, true, true)]
    [InlineData(false, false, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(false, true, false, true)]
    public void BooleanFlags_ShouldSupportVariousCombinations(
        bool enableOrg,
        bool enableCascade,
        bool enableExtends,
        bool expectedOrg)
    {
        // Arrange & Act
        var options = new RepositoryConfigurationOptions
        {
            EnableOrganizationConfig = enableOrg,
            EnableGitHubDirectoryCascade = enableCascade,
            EnableExtendsKey = enableExtends
        };

        // Assert
        options.EnableOrganizationConfig.Should().Be(expectedOrg);
        options.EnableGitHubDirectoryCascade.Should().Be(enableCascade);
        options.EnableExtendsKey.Should().Be(enableExtends);
    }

    [Fact]
    public void RealWorldScenario_MinimalConfiguration_ShouldWork()
    {
        // Arrange & Act - Disable all advanced features
        var options = new RepositoryConfigurationOptions
        {
            EnableOrganizationConfig = false,
            EnableGitHubDirectoryCascade = false,
            EnableExtendsKey = false,
            MaxExtendsDepth = 0
        };

        // Assert
        options.EnableOrganizationConfig.Should().BeFalse();
        options.EnableGitHubDirectoryCascade.Should().BeFalse();
        options.EnableExtendsKey.Should().BeFalse();
        options.MaxExtendsDepth.Should().Be(0);
    }

    [Fact]
    public void RealWorldScenario_HighPerformanceConfiguration_ShouldWork()
    {
        // Arrange & Act - Long cache, minimal cascading
        var options = new RepositoryConfigurationOptions
        {
            CacheTtl = TimeSpan.FromHours(1),
            EnableGitHubDirectoryCascade = false,
            EnableOrganizationConfig = false
        };

        // Assert
        options.CacheTtl.Should().Be(TimeSpan.FromHours(1));
        options.EnableGitHubDirectoryCascade.Should().BeFalse();
        options.EnableOrganizationConfig.Should().BeFalse();
    }

    [Fact]
    public void RealWorldScenario_DeepInheritanceConfiguration_ShouldWork()
    {
        // Arrange & Act - Support deep inheritance chains
        var options = new RepositoryConfigurationOptions
        {
            EnableExtendsKey = true,
            MaxExtendsDepth = 20,
            ArrayMergeStrategy = ArrayMergeStrategy.DeepMergeByIndex
        };

        // Assert
        options.EnableExtendsKey.Should().BeTrue();
        options.MaxExtendsDepth.Should().Be(20);
        options.ArrayMergeStrategy.Should().Be(ArrayMergeStrategy.DeepMergeByIndex);
    }
}

public class ArrayMergeStrategyTests
{
    [Fact]
    public void Replace_ShouldHaveCorrectValue()
    {
        // Act
        var strategy = ArrayMergeStrategy.Replace;

        // Assert
        strategy.Should().Be(ArrayMergeStrategy.Replace);
        ((int)strategy).Should().Be(0);
    }

    [Fact]
    public void Concatenate_ShouldHaveCorrectValue()
    {
        // Act
        var strategy = ArrayMergeStrategy.Concatenate;

        // Assert
        strategy.Should().Be(ArrayMergeStrategy.Concatenate);
        ((int)strategy).Should().Be(1);
    }

    [Fact]
    public void DeepMergeByIndex_ShouldHaveCorrectValue()
    {
        // Act
        var strategy = ArrayMergeStrategy.DeepMergeByIndex;

        // Assert
        strategy.Should().Be(ArrayMergeStrategy.DeepMergeByIndex);
        ((int)strategy).Should().Be(2);
    }

    [Fact]
    public void Enum_ShouldHaveExactlyThreeValues()
    {
        // Act
        var values = Enum.GetValues<ArrayMergeStrategy>();

        // Assert
        values.Should().HaveCount(3);
        values.Should().Contain(ArrayMergeStrategy.Replace);
        values.Should().Contain(ArrayMergeStrategy.Concatenate);
        values.Should().Contain(ArrayMergeStrategy.DeepMergeByIndex);
    }

    [Theory]
    [InlineData(ArrayMergeStrategy.Replace, "Replace")]
    [InlineData(ArrayMergeStrategy.Concatenate, "Concatenate")]
    [InlineData(ArrayMergeStrategy.DeepMergeByIndex, "DeepMergeByIndex")]
    public void ToString_ShouldReturnCorrectName(ArrayMergeStrategy strategy, string expectedName)
    {
        // Act
        var name = strategy.ToString();

        // Assert
        name.Should().Be(expectedName);
    }

    [Fact]
    public void Strategies_ShouldBeDistinct()
    {
        // Arrange
        var strategies = new[]
        {
            ArrayMergeStrategy.Replace,
            ArrayMergeStrategy.Concatenate,
            ArrayMergeStrategy.DeepMergeByIndex
        };

        // Assert
        strategies.Should().OnlyHaveUniqueItems();
    }
}
