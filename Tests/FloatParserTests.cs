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

        [TestCase("This is 0.95% true", 0.95f)]
        public void Parses_Within_Strings(string value, float expected)
        {
            FloatParser.Parse(value).ShouldBe(expected);
        }

        [TestCase("This is 0.95% or 0.98% true", 0.95f)]
        public void Uses_First_Float_In_Strings(string value, float expected)
        {
            FloatParser.Parse(value).ShouldBe(expected);
        }

        [Test]
        public void Throws_On_Unexpected_Format()
        {
            Action action = () => FloatParser.Parse("this is just a long value 1875 that cannot be parsed");
            action.ShouldThrow<FormatException>();
        }
	}
}