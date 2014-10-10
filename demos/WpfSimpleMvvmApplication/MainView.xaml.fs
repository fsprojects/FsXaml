namespace Views

open FsXaml

type MainView = XAML<"MainView.xaml", true>

// This is to demonstrate being able to add in "code behind"
// Note, in this case, this only displays a message when double clicking on 
// the full name text box
type MainViewController() =
    inherit UserControlViewController<MainView>()

    let showMessage _ =
        System.Windows.MessageBox.Show "You double clicked on Full Name!"
        |> ignore

    override __.OnLoaded view =                                
        // Subscribe to the double click event
        view.tbFullName.MouseDoubleClick.Add showMessage

