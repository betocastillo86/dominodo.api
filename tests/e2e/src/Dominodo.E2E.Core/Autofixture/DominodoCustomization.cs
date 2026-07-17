using AutoFixture;
using Dominodo.E2E.Core.Faker;

namespace Dominodo.E2E.Core.Autofixture;

/// <summary>
/// AutoFixture customization producing values that satisfy the API's validation rules.
/// Applied via <c>new Fixture().Customize(new DominodoCustomization())</c>. Kept independent
/// of the AutoFixture.NUnit3 <c>[AutoData]</c> attribute integration, which targets NUnit 3.
/// </summary>
public sealed class DominodoCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        var faker = new Bogus.Faker("en");
        fixture.Register(() => faker.E164Phone());
    }
}
