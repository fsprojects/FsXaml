namespace Views

open FsXaml

// Demonstrates technique for adding "code behind" logic
// Use a different name for the base class, then inherit to add code behind
type ErrorView = XAML<"ErrorView.xaml">
type MainViewBase = XAML<"MainView.xaml">

// Inherited class is MainView, which is referred to/used in MainWindow.xaml directly
type MainView() =
    inherit MainViewBase()

    let showMessage _ =
        System.Windows.MessageBox.Show "You double clicked on Full Name!"
        |> ignore

    // You can override OnInitialize to wire up custom event handlers and such
    // without requiring a self referencing type definition
    // This occurs immediately after the XAML load phase 
    override this.OnInitialize() =
        let subscribeToDoubleClick _ = this.tbFullName.MouseDoubleClick.Add showMessage
                    
        this.Loaded.Add subscribeToDoubleClick
