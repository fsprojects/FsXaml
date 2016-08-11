module FsXaml.UiTests.WpfSimpleMvvmApplicationTests

open System
open System.Diagnostics

open NUnit.Framework
open TestStack.White
open TestStack.White.UIItems
open TestStack.White.UIItems.Finders

let startInfo = StartInfo.create @"..\..\..\demos\WpfSimpleMvvmApplication\bin\Release\WpfSimpleMvvmApplication.exe"

[<Test>]
let ``enter first name and last name then click``() = 
    Assert.True(System.IO.File.Exists(startInfo.FileName))
    // not a very useful test, just tests that nothing is horribly wrong
    use application = Application.AttachOrLaunch(startInfo)
    let window = application.GetWindow("MVVM and XAML Type provider")
    window.WaitWhileBusy()
    let firstNameTextBox = window.Get<TextBox>("FirstName")
    let lastNameTextBox = window.Get<TextBox>("LastName")
    let fullNameTextBox = window.Get<TextBox>("tbFullName")
    let okButton = window.Get<Button>(SearchCriteria.ByText("Ok"))
//    let errorsListView = window.Get<ListView>("ErrorList")
    Assert.AreEqual(" ", fullNameTextBox.Text)
    Assert.AreEqual(false, okButton.Enabled)

    firstNameTextBox.Text <- "Johan"
    fullNameTextBox.Click()
    Assert.AreEqual("Johan ", fullNameTextBox.Text)
    Assert.AreEqual(false, okButton.Enabled)

    lastNameTextBox.Text <- "Larsson"
    fullNameTextBox.Click()
    Assert.AreEqual("Johan Larsson", fullNameTextBox.Text)
    Assert.AreEqual(true, okButton.Enabled)

    okButton.Click()
    let messageBox = window.MessageBox("");
    Assert.AreEqual("Hello, Johan Larsson",messageBox.Get<Label>(SearchCriteria.Indexed(0)).Text)
