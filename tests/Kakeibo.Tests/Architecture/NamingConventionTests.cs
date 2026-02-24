using NetArchTest.Rules;
using Xunit;

namespace Kakeibo.Tests.Architecture;

// Simplified architecture tests: naming conventions only.
// No cross-assembly boundary checks — single project, no modular concerns.
public class NamingConventionTests
{
    private static readonly Types ApiTypes =
        Types.InAssembly(typeof(Kakeibo.Api.Common.Endpoints.IEndpoint).Assembly);

    [Fact]
    public void Endpoints_Should_Have_Endpoint_Suffix()
    {
        var result = ApiTypes
            .That()
            .ImplementInterface(typeof(Kakeibo.Api.Common.Endpoints.IEndpoint))
            .Should()
            .HaveNameEndingWith("Endpoint")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void Validators_Should_Have_Validator_Suffix()
    {
        var result = ApiTypes
            .That()
            .Inherit(typeof(FluentValidation.AbstractValidator<>))
            .Should()
            .HaveNameEndingWith("Validator")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void EventHandlers_Should_Have_Handler_Suffix()
    {
        var result = ApiTypes
            .That()
            .ImplementInterface(typeof(Kakeibo.Api.Infrastructure.Events.IEventHandler<>))
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    private static string FormatFailures(NetArchTest.Rules.TestResult result) =>
        string.Join(", ", result.FailingTypeNames ?? []);
}
