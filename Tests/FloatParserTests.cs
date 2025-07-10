using NUnit.Framework;
using Shouldly;
using TinySystemInfo;

namespace Tests;

public class FloatParserTests
{
    public class ParseMethod : FloatParserTests
    {
        [TestCase("1.0", 1.0f)]
        [TestCase("1.945", 1.945f)]
        [TestCase("0.99", 0.99f)]
        public void Supports_English_Format(string value, float expected)
        {
            FloatParser.Parse(value).ShouldBe(expected);
        }

        [TestCase("1,0", 1.0f)]
        [TestCase("1,945", 1.945f)]
        [TestCase("0,99", 0.99f)]
        public void Supports_German_Format(string value, float expected)
        {
            FloatParser.Parse(value).ShouldBe(expected);
        }
    }
}