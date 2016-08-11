module FsXaml.UiTests.WpfSimpleDrawingApplication2Tests

open System
open System.Diagnostics

open NUnit.Framework
open TestStack.White
open TestStack.White.UIItems
open TestStack.White.UIItems.Finders

let startInfo = StartInfo.create @"..\..\..\demos\WpfSimpleDrawingApplication2\bin\Release\WpfSimpleDrawingApplication2.exe"

[<Test>]
let ``load and click charts``() = 
    Assert.True(System.IO.File.Exists(startInfo.FileName))
    // not a very useful test, just tests that nothing is horribly wrong
    use application = Application.AttachOrLaunch(startInfo)
    let window = application.GetWindow("Drawing using Handler")

    let functionChart = window.Get<GroupBox>(SearchCriteria.ByText("fsxaml:Function map"))
    functionChart.Click()

    let propertyChart = window.Get<GroupBox>(SearchCriteria.ByText("x:Static map"))
    propertyChart.Click()

    // checking that we did not crash.
    Assert.NotNull(window)