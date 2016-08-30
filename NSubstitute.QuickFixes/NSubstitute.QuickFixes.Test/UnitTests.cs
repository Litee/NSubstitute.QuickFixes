﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;

namespace NSubstitute.QuickFixes.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {

        //No diagnostics expected to show up
        [TestMethod]
        public void ShouldSuggestNoFixesIfNoNSubstitute()
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        class MyService
        {
        }

        class MyTest
        {
            MyTest() {
                var sut = new MyService();
            }
        }
    }";

            VerifyCSharpDiagnostic(test, false);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void ShouldGenerateMocks()
        {
            var test = @"
    using System;
    using NSubstitute;

    namespace ConsoleApplication1
    {
        class MyService
        {
            public MyService(IMyAnotherService myAnotherService) {
            }
        }

        interface IMyAnotherService
        {
        }

        class MyTest
        {
            MyTest() {
                var sut = new MyService();
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "NSHA100",
                Message = String.Format("Mocks can be generated"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 20, 27)
                        }
            };

            VerifyCSharpDiagnostic(test, true, expected);

            var fixtest = @"
    using System;
    using NSubstitute;

    namespace ConsoleApplication1
    {
        class MyService
        {
            public MyService(IMyAnotherService myAnotherService) {
            }
        }

        interface IMyAnotherService
        {
        }

        class MyTest
        {
        private IMyAnotherService _myAnotherServiceMock;

        MyTest() {
            _myAnotherServiceMock = Substitute.For<IMyAnotherService>();
            var sut = new MyService(_myAnotherServiceMock);
            }
        }
    }";
            VerifyCSharpFix(test, fixtest, true);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new NSubstituteHelperCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new NSubstituteHelperAnalyzer();
        }
    }
}