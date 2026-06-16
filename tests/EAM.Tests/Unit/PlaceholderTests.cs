using EAM.Application.Common;
using FluentAssertions;
using Xunit;

namespace EAM.Tests.Unit;

/// <summary>
/// Skeleton unit test — proves the test project compiles and the runner is wired up.
/// Replace with real service tests as the application layer is implemented.
/// </summary>
public class PlaceholderTests
{
    [Fact]
    public void PageRequest_clamps_out_of_range_values()
    {
        var page = new PageRequest { Page = 0, Size = 9999 };

        page.Page.Should().Be(1);
        page.Size.Should().Be(20);
        page.Skip.Should().Be(0);
    }
}
