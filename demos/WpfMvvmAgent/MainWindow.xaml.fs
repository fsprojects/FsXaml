namespace Views

open FsXaml

open System.Windows

type MainWindow = XAML<"MainWindow.xaml", true>

// This is to demonstrate being able to add in "code behind"
type MainWindowViewController() =
    inherit WindowViewController<MainWindow>()

    let shutdown _ =
        MessageBox.Show "Thank you for playing." 
        |> ignore
        Application.Current.Shutdown()

    override __.OnInitialized window =            
        // Subscribe to an event handler on the ExitButton
        window.ExitButton.Click.Add shutdown

