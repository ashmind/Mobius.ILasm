using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mobius.ILasm.Core;
using Mobius.ILasm.interfaces;
using Moq;
using Xunit;

namespace Mobius.ILasm.Tests
{
    public class ErrorTests
    {
        [Fact]
        public void Duplicate_ClassMethod_IsReported()
        {
            var errors = AssembleAndGetErrors(@"
                .class C
                    extends System.Object
                {
                    .method void Test() cil managed {}
                    .method void Test() cil managed {}
                }
            ");

            Assert.Single(errors, "Duplicate method declaration: instance System.Void Test()");
        }

        [Fact]
        public void Duplicate_TopLevelMethod_IsReported()
        {
            var errors = AssembleAndGetErrors(@"
                .method void Test() cil managed {}
                .method void Test() cil managed {}
            ");

            Assert.Single(errors, "Duplicate method declaration: instance System.Void Test()");
        }

        [Fact]
        public void Duplicate_Field_IsReported()
        {
            var errors = AssembleAndGetErrors(@"
                .class C
                    extends System.Object
                {
                    .field int32 f
                    .field int32 f
                }
            ");

            Assert.Single(errors, "Duplicate field declaration: System.Int32 f");
        }

        [Fact]
        public void MissingManifestResource_IsReported()
        {
            var errors = AssembleAndGetErrors(@".mresource public NoSuchFile.txt {}");

            Assert.Single(errors, $"Resource file 'NoSuchFile.txt' was not found");
        }

        [Theory]
        [InlineData("")]
        [InlineData("01")]
        [InlineData("01 02 03")]
        public void Ldc_R4_InsuffientByteLength_IsReported(string bytes)
        {
            var errors = AssembleAndGetErrors(@"
                .method void M() cil managed
                {
                    ldc.r4 (" + bytes + @")
                    ret
                }
            ");

            Assert.Single(errors, "Byte array argument of ldc.r4 must include at least 4 bytes");
        }

        [Theory]
        [InlineData("")]
        [InlineData("01")]
        [InlineData("01 02 03 04 05 06 07")]
        public void Ldc_R8_InsuffientByteLength_IsReported(string bytes)
        {
            var errors = AssembleAndGetErrors(@"
                .method void M() cil managed
                {
                    ldc.r8 (" + bytes + @")
                    ret
                }
            ");

            Assert.Single(errors, "Byte array argument of ldc.r8 must include at least 8 bytes");
        }

        private static IReadOnlyList<string> AssembleAndGetErrors(string il)
        {
            var logger = new Mock<ILogger>();
            var driver = new Driver(logger.Object, Driver.Target.Dll);

            driver.Assemble(new[] { il }, new MemoryStream());
            return logger.Invocations
                .Where(i => i.Method.Name == nameof(ILogger.Error))
                .Select(i => (string)i.Arguments.Last())
                .ToList();
        }
    }
}
