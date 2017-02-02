namespace Views

open FsXaml

open System.Windows

type MainWindowBase = XAML<"MainWindow.xaml">

// This is to demonstrate being able to add in "code behind"
type MainWindow() =
    inherit MainWindowBase()

    let shutdown _ =
        MessageBox.Show "Thank you for playing." 
        |> ignore
        Application.Current.Shutdown()

    override this.OnLoaded (_,_) =
        // Subscribe to an event handler on the ExitButton
        this.ExitButton.Click.Add shutdown
        

