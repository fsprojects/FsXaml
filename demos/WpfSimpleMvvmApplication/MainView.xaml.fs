namespace Views

open FsXaml

// Demonstrates technique for adding "code behind" logic
// Use a different name for the base class, then inherit to add code behind
type ErrorView = XAML<"ErrorView.xaml">
type MainViewBase = XAML<"MainView.xaml">

// Inherited class is MainView, which is referred to/used in MainWindow.xaml directly
type MainView() =
    inherit MainViewBase()

    // Note the event handler for the XAML-specified event.    
    // Unlike in C#, the type provider exposes this as a virtual method,
    // which you can override as needed
    override this.OnFullNameDoubleClick (_,_) =
        System.Windows.MessageBox.Show "You double clicked on Full Name!"
        |> ignore
