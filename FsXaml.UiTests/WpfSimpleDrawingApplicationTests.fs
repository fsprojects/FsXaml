module FsXaml.UiTests.WpfSimpleDrawingApplicationTests

open System
open System.Diagnostics

open NUnit.Framework
open TestStack.White
open TestStack.White.UIItems

let startInfo = StartInfo.create @"..\..\..\demos\WpfSimpleDrawingApplication\bin\Release\WpfSimpleDrawingApplication.exe"

[<Test>]
let ``loads``() = 
    Assert.True(System.IO.File.Exists(startInfo.FileName))
    // not a very useful test, just tests that nothing is horribly wrong
    use application = Application.AttachOrLaunch(startInfo)
    let window = application.GetWindow("Drawing using EventToFSharpEvent")
    Assert.NotNull(window)

