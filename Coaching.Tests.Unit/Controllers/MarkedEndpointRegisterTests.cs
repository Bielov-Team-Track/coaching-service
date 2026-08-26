using System.Reflection;
using Coaching.Controllers.V1;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Shared.Enums;
using Shared.Microservices.Guardian;

namespace Coaching.Tests.Unit.Controllers;

/// <summary>
/// The frozen register of coaching routes that accept X-Acting-As, asserted against the
/// controllers. A route that accepts the header and is missing from this list answers 400; a
/// route that is on this list and loses its attribute silently reads the actor's own account again.
/// </summary>
[TestFixture]
[Category("Unit")]
public class MarkedEndpointRegisterTests
{
    private static readonly (Type Controller, string Method, GuardianPermission Permission, ConsentType? Consent)[] Register =
    [
        (typeof(PlansController), nameof(PlansController.GetEventPlan), GuardianPermission.None, null),
    ];

    private static IEnumerable<TestCaseData> RegisterCases() =>
        Register.Select(r => new TestCaseData(r.Controller, r.Method, r.Permission, r.Consent)
            .SetName($"{{m}}_{r.Controller.Name}_{r.Method}"));

    [TestCaseSource(nameof(RegisterCases))]
    public void MarkedAction_DeclaresTheRegistersPermissionAndConsent(
        Type controller, string method, GuardianPermission permission, ConsentType? consent)
    {
        // Arrange
        var action = Resolve(controller, method);

        // Act
        var attribute = action.GetCustomAttribute<AcceptsSubjectAttribute>();

        // Assert
        attribute.Should().NotBeNull($"{controller.Name}.{method} accepts X-Acting-As");
        attribute!.Permission.Should().Be(permission);
        attribute.Consent.Should().Be(consent);
    }

    [TestCaseSource(nameof(RegisterCases))]
    public void MarkedAction_TakesItsSubjectFromTheValidatedHeader(
        Type controller, string method, GuardianPermission permission, ConsentType? consent)
    {
        // Arrange
        _ = permission;
        _ = consent;
        var action = Resolve(controller, method);

        // Act
        var subjectParameters = action.GetParameters()
            .Where(p => p.GetCustomAttribute<FromSubjectAttribute>() is not null)
            .ToList();

        // Assert
        subjectParameters.Should().HaveCount(1, $"{controller.Name}.{method} acts on exactly one subject");
        subjectParameters[0].ParameterType.Should().Be(typeof(Guid));
    }

    [Test]
    public void UnmarkedActions_DoNotBindASubject()
    {
        // Arrange
        var marked = Register.Select(r => (r.Controller, r.Method)).ToHashSet();

        // Act
        var strays = Controllers()
            .SelectMany(c => c.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(m => !marked.Contains((m.DeclaringType!, m.Name)))
            .Where(m => m.GetCustomAttribute<AcceptsSubjectAttribute>() is not null
                        || m.GetParameters().Any(p => p.GetCustomAttribute<FromSubjectAttribute>() is not null))
            .Select(m => $"{m.DeclaringType!.Name}.{m.Name}")
            .ToList();

        // Assert
        strays.Should().BeEmpty("only the frozen register accepts X-Acting-As");
    }

    [Test]
    public void EveryMarkedAction_IsRoutable()
    {
        // Arrange
        var actions = Register.Select(r => Resolve(r.Controller, r.Method));

        // Act
        var unroutable = actions
            .Where(a => !a.GetCustomAttributes<HttpMethodAttribute>().Any())
            .Select(a => $"{a.DeclaringType!.Name}.{a.Name}")
            .ToList();

        // Assert
        unroutable.Should().BeEmpty();
    }

    // Every controller in the service, not a hand-kept list: a stray mark anywhere is a stray.
    private static IEnumerable<Type> Controllers() =>
        typeof(PlansController).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t));

    private static MethodInfo Resolve(Type controller, string method) =>
        controller.GetMethod(method, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        ?? throw new InvalidOperationException($"{controller.Name}.{method} does not exist");
}
