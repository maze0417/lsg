using System;
using FluentAssertions;
using LSG.SharedKernel.Extensions;
using NUnit.Framework;

namespace LSG.UnitTests;

[TestFixture]
public class PlayerWeaponConvertTests
{
    [TestCase(1595323118, 18, "2020/07/21 17:18:38")]
    [TestCase(1595323718, 28, "2020/07/21 17:28:38")]
    public void CanConvertUnixTimeToNow(long timestamp, int min, string normal)
    {
        var dateTime = timestamp.UnixTimeToAsiaDateTimeOffset();

        Console.WriteLine(dateTime);
        dateTime.Year.Should().Be(2020);
        dateTime.Month.Should().Be(7);
        dateTime.Day.Should().Be(21);
        dateTime.Hour.Should().Be(17);
        dateTime.Minute.Should().Be(min);
        dateTime.Second.Should().Be(38);


        normal.Should().Be(timestamp.UnixTimeToAsiaDateTimeOffset().DateTime.ToNormal());
    }

    [TestCase(4, 1595484147, 1595570547)]
    [TestCase(3, 1595484147, 1595570547)]
    [TestCase(2, 1595484147, 1595570547)]
    [TestCase(1, 1595484147, 1595570547)]
    public void CanConvertTimeByDifferentWeapon(int weaponType, long startTime, long endTime)
    {
        var start = startTime.UnixTimeToUtcDateTimeOffset();
        var end = endTime.UnixTimeToUtcDateTimeOffset();


        Console.WriteLine(start);
        Console.WriteLine(end);
        (end - start).TotalDays.Should().Be(1);
    }

    [TestCase(1595496599, 1595496599, 1595497199)]
    public void CanConvertSceneInfoChanged(long serverTime, long startTime, long endTime)
    {
        var server = serverTime.UnixTimeToUtcDateTimeOffset();

        var start = startTime.UnixTimeToUtcDateTimeOffset();
        var end = endTime.UnixTimeToUtcDateTimeOffset();

        Console.WriteLine(server);
        Console.WriteLine(start);
        Console.WriteLine(end);
        (end - start).TotalMinutes.Should().Be(10);
    }
}