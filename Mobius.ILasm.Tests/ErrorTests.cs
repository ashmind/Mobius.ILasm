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

            Assert.Equal(new[] { "Duplicate method declaration: instance System.Void Test()" }, errors);
        }

        [Fact]
        public void Duplicate_TopLevelMethod_IsReported()
        {
            var errors = AssembleAndGetErrors(@"
                .method void Test() cil managed {}
                .method void Test() cil managed {}
            ");

            Assert.Equal(new[] { "Duplicate method declaration: instance System.Void Test()" }, errors);
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

            Assert.Equal(new[] { "Duplicate field declaration: System.Int32 f" }, errors);
        }

        [Fact]
        public void MissingManifestResource_IsReported()
        {
            var errors = AssembleAndGetErrors(@".mresource public NoSuchFile.txt {}");

            Assert.Equal(new[] { "Resource file 'NoSuchFile.txt' was not found" }, errors);
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

            Assert.Equal(new[] { "Byte array argument of ldc.r4 must include at least 4 bytes" }, errors);
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

            Assert.Equal(new[] { "Byte array argument of ldc.r8 must include at least 8 bytes" }, errors);
        }

        [Fact]
        public void LocalsInit_StartingWithComma_IsReported()
        {
            var errors = AssembleAndGetErrors(@"
                .method void M() cil managed
                {
                    .locals init ( ,int32 a )
                    ret
                }
            ");

            Assert.Equal(new[] { "Unexpected syntax: missing first item" }, errors);
        }

        [Fact]
        public void Box_InvalidTypeRefSyntax_IsReported()
        {
            var errors = AssembleAndGetErrors(@"
                .method void M() cil managed
                {
                    box [mscorlib]
                    ret
                }
            ");

            Assert.Equal(new[] { "Failed to parse String '[' as BaseTypeRef" }, errors);
        }

        [Fact]
        public void Call_InvalidMethodRefSyntax_IsReported()
        {
            var errors = AssembleAndGetErrors(@"
                .method void M() cil managed
                {
                    call void [System.Console]::WriteLine(int32)
                    ret
                }
            ");

            Assert.Equal(new[] {
                "Failed to parse String '[' as BaseTypeRef",
                "Failed to parse CallConv 'Default' as BaseMethodRef"
            }, errors);
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
