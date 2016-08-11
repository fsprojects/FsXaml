module FsXaml.UiTests.FsharpAttachedPropertyTests

open System
open System.Diagnostics

open NUnit.Framework
open TestStack.White
open TestStack.White.UIItems

let startInfo = StartInfo.create @"..\..\..\demos\FsharpAttachedProperty\bin\Release\FsharpAttachedProperty.exe"

[<Test>]
let ``loads``() = 
    Assert.True(System.IO.File.Exists(startInfo.FileName))
    // not a very useful test, just tests that nothing is horribly wrong
    use application = Application.AttachOrLaunch(startInfo)
    let window = application.GetWindow("Bind icon with attached property")
    Assert.NotNull(window.Get<Button>("IconButton"))