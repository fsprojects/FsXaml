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

    override this.OnInitialize() =
        // Subscribe to an event handler on the ExitButton
        this.Loaded.Add (fun _ -> this.ExitButton.Click.Add shutdown) 
        

