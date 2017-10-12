#r "presentationframework"
#r "PresentationCore"
#r "WindowsBase"
#r "System.ObjectModel"
#r "System.Xaml"

#I __SOURCE_DIRECTORY__
#r "../bin/fsxaml.wpf/fsxaml.wpf.dll"
#r "../bin/fsxaml.wpf.typeprovider/fsxaml.wpf.typeprovider.dll"

#r @"..\packages\Gjallarhorn\lib\portable-net45+win8+wp8+wpa81\Gjallarhorn.dll"
#r @"..\packages\Gjallarhorn.Bindable\lib\portable-net45+win8+wp8+wpa81\Gjallarhorn.Bindable.dll"
#r @"..\packages\Gjallarhorn.Bindable.Wpf\lib\net45\Gjallarhorn.Bindable.Wpf.dll"

// Define your library scripting code here

open FsXaml
open Gjallarhorn.Bindable
open Gjallarhorn
open System.Windows

Gjallarhorn.Wpf.Platform.install true |> ignore
// Create our application once, and force it to not shutdown.  
// This lets us interact with the Windows we create from within FSI,
// create them multiple times, etc.
let app = Application(ShutdownMode = ShutdownMode.OnExplicitShutdown)

// Create our Window type - Note use of XamlFileLocation
let [<Literal>] XamlFile = __SOURCE_DIRECTORY__ + "/FsXamlInteractiveWindow.xaml"
type MainWindow = XAML<XamlFileLocation = XamlFile>

// Model, message, and update function
type Model = { Value : int }
type Msg = 
    | Increment
    | Decrement
    | Reset
let update msg model =
    match msg with
    | Increment -> { model with Value = model.Value + 1 }
    | Decrement -> { model with Value = model.Value - 1 }
    | Reset -> { Value = 0 }

// Create our Gjallarhorn component for binding. 
// Converts from ISignal<Model> -> IObservable<Msg> using BindingSource
let makeBinding (bindingSource : BindingSource) (click : ISignal<Model>) =    

    bindingSource.ConstantToView ("XAML loaded from: " + XamlFile, "LoadInfo")

    click
    |> Signal.map (fun c -> string c.Value)
    |> Binding.toView bindingSource "Value"

    let i = 
        bindingSource
        |> Binding.createCommand "Increment"    
        |> Observable.map (fun _ -> Increment)    
    let d = 
        bindingSource
        |> Binding.createCommand "Decrement"    
        |> Observable.map (fun _ -> Decrement)    
    Observable.merge i d // Output our message stream
    
let context = Binding.createSource ()
let model = Mutable.createAsync { Value = 0 }
let postMessage = update >> model.Update >> ignore
makeBinding context model
|> Observable.add postMessage

MainWindow(DataContext = context, Topmost = true).Show() 



