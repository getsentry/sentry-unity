using System;
using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Json;
using Sentry.Unity.Tests.SharedClasses;

namespace Sentry.Unity.Tests.Json;

public class SafeSerializerTests
{
    [Test]
    public void SerializeExtraValue_ValueSerializable_ReturnsSerializedValue()
    {
        var testLogger = new TestLogger();

        var actualValue = SafeSerializer.SerializeSafely(new { Member = "testString" }, testLogger);

        Assert.NotNull(actualValue);
        Assert.AreEqual("{\"Member\":\"testString\"}", actualValue);
        Assert.IsEmpty(testLogger.Logs);
    }

    [Test]
    public void SerializeExtraValue_ValueIsString_ReturnsSameString()
    {
        var testLogger = new TestLogger();

        var expectedValue = "testString";
        var actualValue = SafeSerializer.SerializeSafely(expectedValue, testLogger);

        Assert.NotNull(actualValue);
        Assert.AreEqual(expectedValue, actualValue);
        Assert.IsEmpty(testLogger.Logs);
    }

    [Test]
    [TestCase(321)]
    [TestCase(-9870L)]
    [TestCase(234.12)]
    [TestCase(123 + 144D)]
    public void SerializeExtraValue_NumericValueType_ReturnsValueAsToString(object valueType)
    {
        var testLogger = new TestLogger();

        var actualValue = SafeSerializer.SerializeSafely(valueType, testLogger);

        Assert.NotNull(actualValue);
        Assert.AreEqual(valueType.ToString(), actualValue);
        Assert.IsEmpty(testLogger.Logs);
    }

    [Test]
    public void SerializeExtraValue_ValueNotSerializable_ReturnsNull()
    {
        var testLogger = new TestLogger();

        var actualValue = SafeSerializer.SerializeSafely(new SerializationTestClass(), testLogger);

        Assert.IsNull(actualValue);
        Assert.AreEqual(1, testLogger.Logs.Count);
        var (_, _, exception) = testLogger.Logs.Single();
        Assert.NotNull(exception);
        Assert.IsAssignableFrom<DivideByZeroException>(exception!.InnerException);
    }

    private class SerializationTestClass
    {
        public string Member => throw new DivideByZeroException();
    }
}
