module FsXaml.UiTests.FSharpDesktopUINumericUpDownTests

open System
open System.Diagnostics

open NUnit.Framework
open TestStack.White
open TestStack.White.UIItems

let startInfo = StartInfo.create @"..\..\..\demos\FSharpDesktopUINumericUpDown\bin\Release\FSharpDesktopUINumericUpDown.exe"

[<Test>]
let ``click up then down``() = 
    Assert.True(System.IO.File.Exists(startInfo.FileName))
    use application = Application.AttachOrLaunch(startInfo)
    let window = application.GetWindow("Up/Down")
    let upButton = window.Get<Button>("upButton")
    let downButton = window.Get<Button>("downButton")
    let input = window.Get<TextBox>("input")
    Assert.AreEqual("0", input.Text)
    
    upButton.Click()
    Assert.AreEqual("1", input.Text)

    downButton.Click()
    Assert.AreEqual("0", input.Text)