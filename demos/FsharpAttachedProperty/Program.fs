namespace FsharpAttachedProperty
open System
open System.Windows
open System.Windows.Data
open System.Windows.Input

type App = FsXaml.XAML<"App.xaml">
type MainWindow = FsXaml.XAML<"MainWindow.xaml">

module Program =
    [<STAThread>]
    [<EntryPoint>]
    let main _ = 
        let app = App()
        let window = MainWindow()
        app.Run(window) 
