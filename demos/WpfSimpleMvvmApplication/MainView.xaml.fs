namespace Views

open FsXaml

type MainView = XAML<"MainView.xaml">

// This is to demonstrate being able to add in "code behind"
// Note, in this case, this only displays a message when double clicking on 
// the full name text box
type MainViewController() =

    let showMessage _ =
        System.Windows.MessageBox.Show "You double clicked on Full Name!"
        |> ignore

    interface IViewController with
        member this.Attach fe =
            // Use the TypeProvider's Accessor sub-type to gain access to named members
            let view = MainView.Accessor fe
            // Subscribe to the double click event
            view.tbFullName.MouseDoubleClick.Add showMessage

