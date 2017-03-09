#r "presentationframework"
#r "PresentationCore"
#r "WindowsBase"
#r "System.ObjectModel"
#r "System.Xaml"

#I __SOURCE_DIRECTORY__
#r "../bin/fsxaml.wpf/fsxaml.wpf.dll"
#r "../bin/fsxaml.wpf.typeprovider/fsxaml.wpf.typeprovider.dll"

#r @"..\packages\Gjallarhorn\lib\portable-net45+netcore45+wpa81+wp8+MonoAndroid1+MonoTouch1\Gjallarhorn.dll"
#r @"..\packages\Gjallarhorn.Bindable\lib\portable-net45+netcore45+wpa81+wp8+MonoAndroid1+MonoTouch1\Gjallarhorn.Bindable.dll"
#r @"..\packages\Gjallarhorn.Bindable.Wpf\lib\net45\Gjallarhorn.Bindable.Wpf.dll"

// Define your library scripting code here

open FsXaml
open Gjallarhorn.Bindable
open Gjallarhorn

Gjallarhorn.Wpf.Platform.install true |> ignore

let [<Literal>] XamlFile = __SOURCE_DIRECTORY__ + "/FsXamlInteractiveWindow.xaml"

type MainWindow = XAML<XamlFileLocation = XamlFile>

let makeBinding () =
    let bindingSource = Binding.createSource ()

    bindingSource.ConstantToView ("XAML loaded from: " + XamlFile, "Title")

    bindingSource
    |> Binding.createCommand "ClickCommand"
    |> Observable.map (fun _ -> 1)
    |> Observable.scan (+) 0
    |> Observable.map (sprintf "Clicks: %d")
    |> Signal.fromObservable "Click me"
    |> Binding.toView bindingSource "ButtonText"

    bindingSource

[1..2]
|> List.iter (fun _ -> MainWindow(DataContext = makeBinding()).ShowDialog() |> ignore)

