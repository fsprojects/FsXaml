namespace Views

open FsXaml

open System.Windows

type MainWindow = XAML<"MainWindow.xaml">

// This is to demonstrate being able to add in "code behind"
type MainWindowViewController() =

    let shutdown _ =
        MessageBox.Show "Thank you for playing." 
        |> ignore
        Application.Current.Shutdown()

    interface IViewController with
        member this.Attach fe =
            // Use the TypeProvider's Accessor sub-type to gain access to named members
            let window = MainWindow.Accessor fe
            
            // Subscribe to an event handler on the ExitButton
            window.ExitButton.Click.Add shutdown

