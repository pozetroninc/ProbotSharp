// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Specifications;

namespace ProbotSharp.Domain.Tests.Specifications;

public class SpecificationTests
{
    private class AlwaysTrueSpecification<T> : Specification<T>
    {
        public override bool IsSatisfiedBy(T candidate) => true;
    }

    private class AlwaysFalseSpecification<T> : Specification<T>
    {
        public override bool IsSatisfiedBy(T candidate) => false;
    }

    private class IsPositiveSpecification : Specification<int>
    {
        public override bool IsSatisfiedBy(int candidate) => candidate > 0;
    }

    private class IsEvenSpecification : Specification<int>
    {
        public override bool IsSatisfiedBy(int candidate) => candidate % 2 == 0;
    }

    [Fact]
    public void And_WithTwoTrueSpecs_ShouldReturnTrue()
    {
        var spec = new AlwaysTrueSpecification<int>().And(new AlwaysTrueSpecification<int>());

        spec.IsSatisfiedBy(42).Should().BeTrue();
    }

    [Fact]
    public void And_WithOneFalseSpec_ShouldReturnFalse()
    {
        var spec = new AlwaysTrueSpecification<int>().And(new AlwaysFalseSpecification<int>());

        spec.IsSatisfiedBy(42).Should().BeFalse();
    }

    [Fact]
    public void Or_WithOneTrueSpec_ShouldReturnTrue()
    {
        var spec = new AlwaysTrueSpecification<int>().Or(new AlwaysFalseSpecification<int>());

        spec.IsSatisfiedBy(42).Should().BeTrue();
    }

    [Fact]
    public void Or_WithTwoFalseSpecs_ShouldReturnFalse()
    {
        var spec = new AlwaysFalseSpecification<int>().Or(new AlwaysFalseSpecification<int>());

        spec.IsSatisfiedBy(42).Should().BeFalse();
    }

    [Fact]
    public void Not_WithTrueSpec_ShouldReturnFalse()
    {
        var spec = new AlwaysTrueSpecification<int>().Not();

        spec.IsSatisfiedBy(42).Should().BeFalse();
    }

    [Fact]
    public void Not_WithFalseSpec_ShouldReturnTrue()
    {
        var spec = new AlwaysFalseSpecification<int>().Not();

        spec.IsSatisfiedBy(42).Should().BeTrue();
    }

    [Fact]
    public void AndOperator_ShouldWorkSameAsAndMethod()
    {
        var spec1 = new IsPositiveSpecification() & new IsEvenSpecification();
        var spec2 = new IsPositiveSpecification().And(new IsEvenSpecification());

        spec1.IsSatisfiedBy(4).Should().Be(spec2.IsSatisfiedBy(4));
        spec1.IsSatisfiedBy(-2).Should().Be(spec2.IsSatisfiedBy(-2));
    }

    [Fact]
    public void OrOperator_ShouldWorkSameAsOrMethod()
    {
        var spec1 = new IsPositiveSpecification() | new IsEvenSpecification();
        var spec2 = new IsPositiveSpecification().Or(new IsEvenSpecification());

        spec1.IsSatisfiedBy(4).Should().Be(spec2.IsSatisfiedBy(4));
        spec1.IsSatisfiedBy(3).Should().Be(spec2.IsSatisfiedBy(3));
    }

    [Fact]
    public void NotOperator_ShouldWorkSameAsNotMethod()
    {
        var spec1 = !new IsPositiveSpecification();
        var spec2 = new IsPositiveSpecification().Not();

        spec1.IsSatisfiedBy(5).Should().Be(spec2.IsSatisfiedBy(5));
        spec1.IsSatisfiedBy(-5).Should().Be(spec2.IsSatisfiedBy(-5));
    }

    [Fact]
    public void ComplexComposition_ShouldWorkCorrectly()
    {
        // (Positive AND Even) OR (NOT Positive)
        var spec = (new IsPositiveSpecification() & new IsEvenSpecification()) | !new IsPositiveSpecification();

        spec.IsSatisfiedBy(4).Should().BeTrue();   // positive and even
        spec.IsSatisfiedBy(3).Should().BeFalse();  // positive but not even
        spec.IsSatisfiedBy(-2).Should().BeTrue();  // not positive
        spec.IsSatisfiedBy(0).Should().BeTrue();   // not positive (and even)
    }

    [Fact]
    public void And_WithNullSpec_ShouldThrow()
    {
        var spec = new AlwaysTrueSpecification<int>();

        var act = () => spec.And(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Or_WithNullSpec_ShouldThrow()
    {
        var spec = new AlwaysTrueSpecification<int>();

        var act = () => spec.Or(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BitwiseAnd_ShouldWorkSameAsAndMethod()
    {
        var spec1 = new IsPositiveSpecification();
        var spec2 = new IsEvenSpecification();

        var result = Specification<int>.BitwiseAnd(spec1, spec2);

        result.IsSatisfiedBy(4).Should().BeTrue();   // positive and even
        result.IsSatisfiedBy(3).Should().BeFalse();  // positive but not even
        result.IsSatisfiedBy(-2).Should().BeFalse(); // even but not positive
    }

    [Fact]
    public void BitwiseAnd_WithNullLeft_ShouldThrow()
    {
        var spec = new IsPositiveSpecification();

        var act = () => Specification<int>.BitwiseAnd(null!, spec);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BitwiseAnd_WithNullRight_ShouldThrow()
    {
        var spec = new IsPositiveSpecification();

        var act = () => Specification<int>.BitwiseAnd(spec, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BitwiseOr_ShouldWorkSameAsOrMethod()
    {
        var spec1 = new IsPositiveSpecification();
        var spec2 = new IsEvenSpecification();

        var result = Specification<int>.BitwiseOr(spec1, spec2);

        result.IsSatisfiedBy(4).Should().BeTrue();   // positive or even (both)
        result.IsSatisfiedBy(3).Should().BeTrue();   // positive
        result.IsSatisfiedBy(-2).Should().BeTrue();  // even
        result.IsSatisfiedBy(-3).Should().BeFalse(); // neither
    }

    [Fact]
    public void BitwiseOr_WithNullLeft_ShouldThrow()
    {
        var spec = new IsPositiveSpecification();

        var act = () => Specification<int>.BitwiseOr(null!, spec);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BitwiseOr_WithNullRight_ShouldThrow()
    {
        var spec = new IsPositiveSpecification();

        var act = () => Specification<int>.BitwiseOr(spec, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void LogicalNot_ShouldWorkSameAsNotMethod()
    {
        var spec = new IsPositiveSpecification();

        var result = Specification<int>.LogicalNot(spec);

        result.IsSatisfiedBy(5).Should().BeFalse();  // positive
        result.IsSatisfiedBy(-5).Should().BeTrue();  // not positive
        result.IsSatisfiedBy(0).Should().BeTrue();   // not positive
    }

    [Fact]
    public void LogicalNot_WithNull_ShouldThrow()
    {
        var act = () => Specification<int>.LogicalNot(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AndOperator_WithNullLeft_ShouldThrow()
    {
        var spec = new IsPositiveSpecification();

        var act = () => { var result = null! & spec; };

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AndOperator_WithNullRight_ShouldThrow()
    {
        var spec = new IsPositiveSpecification();

        var act = () => { var result = spec & null!; };

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void OrOperator_WithNullLeft_ShouldThrow()
    {
        var spec = new IsPositiveSpecification();

        var act = () => { var result = null! | spec; };

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void OrOperator_WithNullRight_ShouldThrow()
    {
        var spec = new IsPositiveSpecification();

        var act = () => { var result = spec | null!; };

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void NotOperator_WithNull_ShouldThrow()
    {
        var act = () => { var result = !((Specification<int>)null!); };

        act.Should().Throw<ArgumentNullException>();
    }
}
