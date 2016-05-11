namespace Views

open FsXaml

open System.Windows

type MainWindowBase = XAML<"MainWindow.xaml">

// This is to demonstrate being able to add in "code behind"
type MainWindow() as self =
    inherit MainWindowBase()

    let shutdown _ =
        MessageBox.Show "Thank you for playing." 
        |> ignore
        Application.Current.Shutdown()

    do
        // Subscribe to an event handler on the ExitButton
        self.Loaded.Subscribe(fun _ -> 
            self.ExitButton.Click.Add shutdown
            ) |> ignore
        

