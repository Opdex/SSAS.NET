using FluentAssertions;
using System;
using Xunit;

namespace SSAS.NET.Tests;

public class StratisIdTests
{
    [Fact]
    public void ToString_Callback_Return()
    {
        var stratisId = new StratisId("api.opdex.com/auth", "123456789", 1635200000);

        stratisId.ToString().Should().Be("api.opdex.com/auth?uid=123456789&exp=1635200000");
    }

    [Fact]
    public void ToUriString_Callback_ReturnWithScheme()
    {
        var stratisId = new StratisId("api.opdex.com/auth", "123456789", 1635200000);

        stratisId.ToUriString().Should().Be("sid:api.opdex.com/auth?uid=123456789&exp=1635200000");
    }

    [Fact]
    public void ToProtocolString_Callback_ReturnWithScheme()
    {
        var stratisId = new StratisId("api.opdex.com/auth", "123456789", 1635200000);

        stratisId.ToProtocolString().Should().Be("web+sid://api.opdex.com/auth?uid=123456789&exp=1635200000");
    }

    [Fact]
    public void Callback_WithScheme_TrimScheme()
    {
        var stratisId = new StratisId("https://api.opdex.com/auth", "123456789");

        stratisId.Callback.Should().Be("api.opdex.com/auth?uid=123456789");
    }

    [Fact]
    public void Callback_WithAuthorityIndicator_TrimAuthorityIndicator()
    {
        var stratisId = new StratisId("//api.opdex.com/auth", "123456789");

        stratisId.Callback.Should().Be("api.opdex.com/auth?uid=123456789");
    }

    [Fact]
    public void Callback_WithoutExp_Combine()
    {
        var stratisId = new StratisId("api.opdex.com/auth", "123456789");

        stratisId.Callback.Should().Be("api.opdex.com/auth?uid=123456789");
    }

    [Fact]
    public void Callback_WithExp_Combine()
    {
        var stratisId = new StratisId("api.opdex.com/auth", "123456789", 1635200000);

        stratisId.Callback.Should().Be("api.opdex.com/auth?uid=123456789&exp=1635200000");
    }

    [Fact]
    public void Uid_Constructor_Set()
    {
        var stratisId = new StratisId("api.opdex.com/auth", "123456789", 1635200000);

        stratisId.Uid.Should().Be("123456789");
    }

    [Fact]
    public void Expiry_ConstructorUnixTime_Set()
    {
        var expiry = DateTimeOffset.UtcNow.AddSeconds(-1);
        var stratisId = new StratisId("api.opdex.com/auth", "123456789", expiry.ToUnixTimeSeconds());

        var expiryWithSecondPrecision = new DateTime(expiry.Year, expiry.Month, expiry.Day, expiry.Hour, expiry.Minute, expiry.Second);
        stratisId.Expiry.Should().Be(expiryWithSecondPrecision);
    }

    [Fact]
    public void Expiry_ConstructorDateTimeOffset_Set()
    {
        var expiry = DateTimeOffset.UtcNow.AddSeconds(-1);
        var stratisId = new StratisId("api.opdex.com/auth", "123456789", expiry);

        var expiryWithSecondPrecision = new DateTime(expiry.Year, expiry.Month, expiry.Day, expiry.Hour, expiry.Minute, expiry.Second);
        stratisId.Expiry.Should().Be(expiryWithSecondPrecision);
    }

    [Fact]
    public void Expired_OneSecondBeforeNow_True()
    {
        var stratisId = new StratisId("api.opdex.com/auth", "123456789", DateTimeOffset.UtcNow.AddSeconds(-1).ToUnixTimeSeconds());

        stratisId.Expired.Should().Be(true);
    }

    [Fact]
    public void Expired_OneSecondFromNow_False()
    {
        var stratisId = new StratisId("api.opdex.com/auth", "123456789", DateTimeOffset.UtcNow.AddSeconds(1).ToUnixTimeSeconds());

        stratisId.Expired.Should().Be(false);
    }

    [Fact]
    public void Expired_NoExpiry_False()
    {
        var stratisId = new StratisId("api.opdex.com/auth", "123456789");

        stratisId.Expired.Should().Be(false);
    }

    [Fact]
    public void Equal_True()
    {
        var a = new StratisId("api.opdex.com/auth", "123456789", 1637240507);
        var b = new StratisId("api.opdex.com/auth", "123456789", 1637240507);

        a.Equals(b).Should().Be(true);
        a.Equals((object)b).Should().Be(true);
        (a != b).Should().Be(false);
        (a == b).Should().Be(true);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equal_False()
    {
        var a = new StratisId("api.opdex.com/auth", "123456789", 1637240507);
        var b = new StratisId("api.opdex.com/auth", "123456789");

        a.Equals(b).Should().Be(false);
        a.Equals((object)b).Should().Be(false);
        (a != b).Should().Be(true);
        (a == b).Should().Be(false);
        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }

    [Fact]
    public void EqualOperator_Null_Handle()
    {
        StratisId? a = null;
        StratisId? b = null;
        StratisId c = new ("api.opdex.com/auth", "123456789");

        (a == b).Should().Be(true);
        (a != b).Should().Be(false);
        (a == c).Should().Be(false);
        (a != c).Should().Be(true);
    }

    [Fact]
    public void TryParse_WithProtocol_True()
    {
        var canParse = StratisId.TryParse("web+sid://api.opdex.com/auth?uid=123456789&exp=1637240507", out var stratisId);

        canParse.Should().Be(true);
        stratisId.Should().Be(new StratisId("api.opdex.com/auth", "123456789", 1637240507));
    }

    [Fact]
    public void TryParse_WithScheme_True()
    {
        var canParse = StratisId.TryParse("sid:api.opdex.com/auth?uid=123456789&exp=1637240507", out var stratisId);

        canParse.Should().Be(true);
        stratisId.Should().Be(new StratisId("api.opdex.com/auth", "123456789", 1637240507));
    }

    [Fact]
    public void TryParse_NotAsUri_True()
    {
        var canParse = StratisId.TryParse("api.opdex.com/auth?uid=123456789&exp=1637240507", out var stratisId);

        canParse.Should().Be(true);
        stratisId.Should().Be(new StratisId("api.opdex.com/auth", "123456789", 1637240507));
    }

    [Fact]
    public void TryParse_WithAnchor_False()
    {
        var canParse = StratisId.TryParse("sid://api.opdex.com/auth?uid=123456789&exp=1637240507", out var stratisId);

        canParse.Should().Be(false);
        stratisId.Should().Be(null);
    }

    [Fact]
    public void TryParse_ExpBeforeUid_True()
    {
        var canParse = StratisId.TryParse("sid:api.opdex.com/auth?exp=1637240507&uid=123456789", out var stratisId);

        canParse.Should().Be(true);
        stratisId.Should().Be(new StratisId("api.opdex.com/auth", "123456789", 1637240507));
    }

    [Fact]
    public void TryParse_WithoutExp_True()
    {
        var canParse = StratisId.TryParse("sid:api.opdex.com/auth?uid=123456789", out var stratisId);

        canParse.Should().Be(true);
        stratisId.Should().Be(new StratisId("api.opdex.com/auth", "123456789"));
    }

    [Fact]
    public void TryParse_WithPort_True()
    {
        var canParse = StratisId.TryParse("sid:api.opdex.com:1111/auth?uid=123456789&exp=1637240507", out var stratisId);

        canParse.Should().Be(true);
        stratisId.Should().Be(new StratisId("api.opdex.com:1111/auth", "123456789", 1637240507));
    }

    [Fact]
    public void TryParse_WithoutUid_False()
    {
        var canParse = StratisId.TryParse("sid:api.opdex.com/auth?exp=1637240507", out var stratisId);

        canParse.Should().Be(false);
        stratisId.Should().Be(null);
    }

    [Fact]
    public void TryParse_UidNotInQueryString_False()
    {
        var canParse = StratisId.TryParse("sid:api.opdex.com/auth/uid/1637240507", out var stratisId);

        canParse.Should().Be(false);
        stratisId.Should().Be(null);
    }

    [Fact]
    public void TryParse_QueryStringInvalidFormat_False()
    {
        var canParse = StratisId.TryParse("sid:api.opdex.com/auth?uid=123456789?exp=1637240507", out var stratisId);

        canParse.Should().Be(false);
        stratisId.Should().Be(null);
    }
}
