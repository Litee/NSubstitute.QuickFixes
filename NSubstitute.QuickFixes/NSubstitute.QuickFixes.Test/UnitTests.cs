﻿using ApprovalTests;
using ApprovalTests.Reporters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;

namespace NSubstitute.QuickFixes.Test
{
    [TestClass]
    [UseReporter(typeof(DiffReporter))]
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

            Approvals.Verify(VerifyCSharpFix(test, true));
        }

        [TestMethod]
        public void ShouldNotGenerateExistingFields()
        {
            var test = @"
    using System;
    using NSubstitute;

    namespace ConsoleApplication1
    {
        class FirstService
        {
            public FirstService(ISecondService secondService, IThirdService thirdService) {
            }
        }

        interface ISecondService { }

        interface IThirdService { }

        class MyTest
        {
            IThirdService _thirdServiceMock;

            MyTest() {
                var sut = new FirstService();
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
                            new DiagnosticResultLocation("Test0.cs", 22, 27)
                        }
            };

            VerifyCSharpDiagnostic(test, true, expected);

            Approvals.Verify(VerifyCSharpFix(test, true));
        }

        [TestMethod]
        public void ShouldGenerateMocksForFuncFactories()
        {
            var test = @"
    using System;
    using NSubstitute;

    namespace ConsoleApplication1
    {
        class FirstService
        {
            public FirstService(Func<ISecondService> secondServiceFactory) {
            }
        }

        interface ISecondService { }

        class MyTest
        {

            MyTest() {
                var sut = new FirstService();
            }
        }
    }";
            Approvals.Verify(VerifyCSharpFix(test, true));
        }

        [TestMethod]
        public void ShouldGenerateMocksForNewParameters()
        {
            var test = @"
    using System;
    using NSubstitute;

    namespace ConsoleApplication1
    {
        class FirstService
        {
            public FirstService(ISecondService secondService, IThirdService thirdService) {
            }
        }

        interface ISecondService { }

        interface IThirdService { }

        class MyTest
        {

            MyTest() {
                var sut = new FirstService(null);
            }
        }
    }";
            Approvals.Verify(VerifyCSharpFix(test, true));
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