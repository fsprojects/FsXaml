namespace Views

open ViewModels
open FsXaml
open System.Threading
open System.Runtime.CompilerServices
open System.Windows
open System.Windows.Controls
open System.Windows.Data
open System.Windows.Media
open System.Windows.Threading

type MainView = XAML<"MainView.xaml", true>

// This is to demonstrate being able to add in "code behind"
// Note, in this case, this only displays a message when double clicking on 
// the full name text box
type MainViewController() =
    inherit UserControlViewController<MainView>()

    let showMessage _ =
        System.Windows.MessageBox.Show "You double clicked on Full Name!"
        |> ignore

    override this.OnLoaded view =                            
        // Subscribe to the double click event, but also unsubscribe when we unload
        view.tbFullName.MouseDoubleClick.Subscribe showMessage
        |> this.DisposeOnUnload



/// Below is WIP alternative to build UI
//type GridSizing =
//    | Auto
//    | Star
//    | StarM of multiplier : double
//    | Fixed of height : double
//
//type Binding =
//    | DirectToDC
//    | Path of path : string
//
//[<Extension>]
//type ContentControlExtensions () =
//    [<Extension>]
//    static member inline (+<) (parent : ContentControl, child : obj) =
//        parent.Content <- child
//        parent
//    [<Extension>]
//    static member inline Add (parent : #ContentControl, child : #UIElement) =
//        parent.Content <- child
//        parent
//
////[<Extension>]
////type DecoratorExtensions () =
////    [<Extension>]
////    static member inline (+<) (parent : Border, child : Grid) =
////        parent.Child <- child
////        parent
////    [<Extension>]
////    static member inline Add (parent : #Decorator, child : #UIElement) =
////        parent.Child <- child
////        parent
//
//[<AutoOpen>]
//module FsXamlHelpers =
//    let init f (control : #UIElement) =
//        f(control)
//        control
//
//    let ue (control : #UIElement) =
//        control :> UIElement
//    let setContent (parent: #ContentControl) child =
//        parent.Content <- child
//    
//    let decorate child (parent : #Decorator) =
//        parent.Child <- child
//        parent
//    let (+~<) (parent : #Decorator) child = decorate child parent
//    let (+~>) (parent : #Decorator) child =
//        parent.Child <- child
//        child
//
//    let content (parent: #ContentControl) child =
//        setContent parent child
//        parent
//
//    let (+<) (parent: #ContentControl) child =
//        setContent parent child
//        parent
//
//    let (+>) (parent : #ContentControl) child =
//        parent.Content <- child
//        child
//
//    let addChild child (parent : #Panel) =
//        parent.Children.Add child |> ignore
//        parent
//    let addChildren (children : #seq<#UIElement>) (parent : #Panel) =
//        children
//        |> Seq.iter (fun child -> parent.Children.Add child |> ignore)        
//        parent
//
//    let (++<) parent child = addChild child parent
//    let (++>) (parent : #Panel) child =
//        parent.Children.Add child |> ignore
//        child
//    let (+*<) (parent : #Panel) (children : #seq<#UIElement>) = addChildren children parent
//
//    let bind (binding : Binding) dependencyProperty mode (control : #DependencyObject) =
//        let binding =
//            match binding with
//            | DirectToDC -> Binding()
//            | Path path -> Binding(path)
//        binding.Mode <- mode
//        BindingOperations.SetBinding(control, dependencyProperty, binding) |> ignore
//        control
//
//    let el (control : #UIElement) =
//        control :> UIElement
//
//type XamlParser () =
//    let context = Markup.ParserContext()
//
//    do
//        context.XamlTypeMapper <- Markup.XamlTypeMapper([||])
//        context.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation")
//        context.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml")
//
//    member __.AddNamespaces namespaces =
//        namespaces
//        |> Seq.iter (fun (prefix, t : System.Type) -> 
//            context.XamlTypeMapper.AddMappingProcessingInstruction(prefix, t.Namespace, t.Assembly.FullName)
//            context.XmlnsDictionary.Add(prefix, prefix))
//
//    member __.AddNamespaces namespaces =
//        namespaces
//        |> Seq.iter (fun (prefix, ns) -> context.XmlnsDictionary.Add(prefix, ns))
//
//    member __.Load xaml : 'a =
//        unbox <| Markup.XamlReader.Parse(xaml, context)
//
//module Template =
//    type internal TemplateControlParent() =
//        inherit ContentControl()
//
//        static member val GeneratorProperty = DependencyProperty.Register("Generator", typeof<unit -> obj>, typeof<TemplateControlParent>, PropertyMetadata(null, PropertyChangedCallback(TemplateControlParent.FactoryChanged))) with get
//
//        static member FactoryChanged (instance : DependencyObject) (args : DependencyPropertyChangedEventArgs) =
//            let templateParent = instance :?> TemplateControlParent 
//            let generator = args.NewValue :?> (unit -> obj)
//            templateParent.Content <- generator()
//    let private createFactory f =
//        let factory = FrameworkElementFactory(typeof<TemplateControlParent>)
//        factory.SetValue(TemplateControlParent.GeneratorProperty, f)
//        factory
//
//    let createDataTemplate (f : unit -> obj) =
//        DataTemplate(typeof<DependencyObject>, VisualTree = createFactory f)        
//
//    let createControlTemplate (f : unit -> obj) =
//        ControlTemplate(VisualTree = createFactory f)        
//
//    let item f (control : #ItemsControl) =
//        let dt = createDataTemplate (fun _ -> f() |> box)
//        control.ItemTemplate <- dt
//        control
//
//    let parseItem (parser : XamlParser) xaml (control : #ItemsControl) =
//        let xaml' = sprintf "<DataTemplate>%s</DataTemplate>" xaml
//        let dt = parser.Load xaml'
//        control.ItemTemplate <- dt
//        control
//
//    let dataTemplate (viewModelType : System.Type) (viewType : System.Type) =
//        let xaml = sprintf """<DataTemplate DataType="{x:Type vm:%s}"><v:%s /></DataTemplate>""" viewModelType.Name viewType.Name
//        let parser = XamlParser()
//        parser.AddNamespaces [ ("vm", viewModelType) ; ("v" , viewType)]
//        parser.Load(xaml) :> DataTemplate
//
//module Grid =
//    let create (rows : GridSizing seq) (cols : GridSizing seq) =
//        let g = Grid()
//        let gsToGL gs =
//            match gs with
//            | Auto -> GridLength(1.0, GridUnitType.Auto)
//            | Star -> GridLength(1.0, GridUnitType.Star)
//            | StarM multiplier -> GridLength(multiplier, GridUnitType.Star)
//            | Fixed height -> GridLength(height, GridUnitType.Pixel)
//
//        rows
//        |> Seq.iter (fun r -> g.RowDefinitions.Add(RowDefinition(Height = (gsToGL r))))
//        cols
//        |> Seq.iter (fun c -> g.ColumnDefinitions.Add(ColumnDefinition(Width = (gsToGL c))))
//
//        g
//        
//    let setPos row col (control : #UIElement) =
//        Grid.SetColumn(control, col)
//        Grid.SetRow(control, row)
//        control
//    let setSpans rowSpan colSpan (control : #UIElement) =
//        Grid.SetColumnSpan(control, colSpan)
//        Grid.SetRowSpan(control, rowSpan)
//        control
//
//module Design =
//    let preview (controlCreate : unit -> #UIElement) =
//        let thS () = 
//            SynchronizationContext.SetSynchronizationContext(DispatcherSynchronizationContext Dispatcher.CurrentDispatcher)
//            let win = 
//                let control = controlCreate()
//                match box control with
//                | :? Window as window -> window
//                | _ -> Window(SizeToContent = SizeToContent.WidthAndHeight, Content = control, Topmost = true)
//            win.Closed.Add(fun _ -> System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvokeShutdown(System.Windows.Threading.DispatcherPriority.Background))
//            win.Show()
//            System.Windows.Threading.Dispatcher.Run()
//        let th = Thread(ThreadStart(thS))
//        th.SetApartmentState(ApartmentState.STA)
//        th.Start()
//
//type MainView (vm : MainViewModel) as self =
//    inherit UserControl(MinHeight = 200., MinWidth = 300., DataContext = vm)
//
//    let makeText header target row readonly : UIElement list =
//        let block = 
//            TextBlock(Text = header, Margin = Thickness(3.)) 
//            |> Grid.setPos row 0
//        let tb =
//            TextBox(FontSize=16., Margin = Thickness(3.))
//            |> bind (Path target) TextBox.TextProperty (if readonly then BindingMode.OneWay else BindingMode.TwoWay)
//            |> Grid.setPos row 1 
//
//        if readonly then
//            tb.Foreground <- Brushes.Gray
//            tb.IsTabStop <- false
//
//        [ block ; tb ]
//
//    let parser = XamlParser()
//   
//    do
//        self.Content <- 
//            Grid.create [Auto;Auto;Auto;Auto;Star;Auto] [Star;Star] 
//            |> addChildren (seq { 
//                yield! makeText "First:" "FirstName" 0 false 
//                yield! makeText "Last:" "LastName" 1 false 
//                yield! makeText "Full name:" "FullName" 2 true
//                yield ue(Border(Margin = Thickness(3.), 
//                            VerticalAlignment = VerticalAlignment.Stretch,
//                            BorderThickness = Thickness(1.),
//                            BorderBrush = Brushes.Gray)
//                        |> decorate (
//                            Grid.create [Auto;Star] []
//                            |> addChild (
//                                ListView(HorizontalContentAlignment=HorizontalAlignment.Stretch)
////                                |> Template.parseItem parser 
////                                    """<TextBlock 
////                                        Background="Red" 
////                                        Foreground="White" 
////                                        HorizontalAlignment="Stretch" 
////                                        Text="{Binding}" />""" 
//                                  |> Template.item (fun _ ->
//                                        TextBlock(
//                                            Background = Brushes.Red, 
//                                            Foreground = Brushes.White, 
//                                            HorizontalAlignment = HorizontalAlignment.Stretch)
//                                        |> bind DirectToDC TextBlock.TextProperty BindingMode.OneWay)
//                                |> bind (Path "EntityErrors") ListView.ItemsSourceProperty BindingMode.OneWay
//                            ) 
//                        ) 
//                        |> Grid.setPos 4 0
//                        |> Grid.setSpans 1 2 )
//                yield ue(Button(Margin=Thickness(3.), FontSize=16., Content=box("Ok"))
//                |> Grid.setPos 5 0
//                |> Grid.setSpans 1 2
//                |> bind (Path "OkCommand") Button.CommandProperty BindingMode.OneWay
//                |> bind (Path "FullName") Button.CommandParameterProperty BindingMode.OneWay)
//            })
//    new() = MainView(MainViewModel())