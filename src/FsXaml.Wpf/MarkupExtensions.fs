namespace FsXaml

open System
open System.Reflection
open System.Windows
open System.Windows.Data
open System.Windows.Input
open System.Windows.Markup

module ServiceProvider = 
    let private getService<'t> (serviceProvider : IServiceProvider) = serviceProvider.GetService typeof<'t> :?> 't
    let ProvideValueTarget serviceProvider = getService<IProvideValueTarget> serviceProvider
    let XamlTypeResolver serviceProvider = getService<IXamlTypeResolver> serviceProvider

type ToArrayConverter() = 
    static member Default = ToArrayConverter()
    interface IMultiValueConverter with
        member x.Convert(values : obj [], _, _, _) : obj = 
            // clone is needed here, if we return the same instance WPF ~fixes~ it by replaing it with an array of nulls.
            values.Clone()
        member x.ConvertBack(_, _, _, _) : obj [] = failwith "Not supported"

type ClassNameAndMethodName = 
    { QualifiedName : string
      MethoodName : string }

[<MarkupExtensionReturnType(typeof<MethodInfo>)>]
type FunctionExtension(name : String) = 
    inherit MarkupExtension()
    let name = name
    
    let classNameAndMethodName = 
        let m = System.Text.RegularExpressions.Regex.Match(name, @"^ *(?<qn>\w+:\w+)\.(?<method>\w+) *$")
        if m.Success then 
            Some { QualifiedName = m.Groups.["qn"].Value
                   MethoodName = m.Groups.["method"].Value }
        else
            DesignMode.failIfDesignModef 
                "Illegal method pattern %s. Expected a format like: local:MapModule.MapFunction" name
            None
    
    override this.ProvideValue(serviceProvider : IServiceProvider) = 
        let resolver = ServiceProvider.XamlTypeResolver serviceProvider
        if obj.ReferenceEquals(resolver, null) then null
        else
            match classNameAndMethodName with
            | Some x ->
                let typ = resolver.Resolve(x.QualifiedName)
                let mi = typ.GetMethod(x.MethoodName, BindingFlags.Static ||| BindingFlags.Public)
                if obj.ReferenceEquals(mi, null) then 
                    DesignMode.failIfDesignModef "Could find a static public method for %s" name
                mi :> _            
            | None -> null         

[<MarkupExtensionReturnType(typeof<RoutedEventHandler>)>]
type HandlerExtension(observerBinding : Binding, map : obj) as me = 
    inherit MarkupExtension()
    
    let getMapMethod (map : obj) = 
        let verifyMapMethod (mi : MethodInfo) = 
            let paramaters = mi.GetParameters()
            if not (paramaters.Length = 1) || not (typeof<EventArgs>.IsAssignableFrom(paramaters.[0].ParameterType)) then 
                DesignMode.failIfDesignModef 
                    "Invalid map method: %s first argument must be a subtype of System.EventArgs" 
                    (map.GetType().FullName)
            if mi.ReturnType = typeof<Void> then 
                DesignMode.failIfDesignModef "Invalid map method: %s cannot have returntype void" 
                    (map.GetType().FullName)
        
        let (|InvokeMethod|_|) (value : obj) = 
            if obj.ReferenceEquals(value, null) then None
            else 
                let invokeMethod = value.GetType().GetMethod("Invoke")
                if obj.ReferenceEquals(invokeMethod, null) then None
                else Some(invokeMethod)
        
        let invoke (mi : MethodInfo) self arg = mi.Invoke(self, [| arg |])
        match map with
        | :? MethodInfo as mi -> 
            verifyMapMethod mi
            invoke mi null
        | InvokeMethod(mi) -> 
            verifyMapMethod mi
            invoke mi map
        | :? FunctionExtension -> 
            DesignMode.failIfNotDesignMode "map cannot be of type FunctionExtension. (Should never get here)"
            fun e -> e
        | _ -> 
            DesignMode.failIfDesignModef "Invalid map method: %A" map
            fun e -> e
    
    let observerBinding = observerBinding
    let map = getMapMethod map
    static let HandlersProperty = 
        DependencyProperty.RegisterAttached
            ("Handlers", typeof<ResizeArray<HandlerExtension>>, typeof<HandlerExtension>, PropertyMetadata(null))
    static let ObserversProperty = 
        DependencyProperty.RegisterAttached
            ("Observers", typeof<obj []>, typeof<HandlerExtension>, PropertyMetadata(null))
    
    let bindObserver (element : FrameworkElement) = 
        let mutable handlers = element.GetValue(HandlersProperty) :?> ResizeArray<HandlerExtension>
        if obj.ReferenceEquals(handlers, null) then 
            handlers <- ResizeArray<HandlerExtension>()
            element.SetValue(HandlersProperty, handlers)
        handlers.Add me
        let binding = MultiBinding()
        binding.Converter <- ToArrayConverter.Default
        for handler in handlers do
            binding.Bindings.Add handler.ObserverBinding
        BindingOperations.SetBinding(element, ObserversProperty, binding)
    
    let getObserver (element : FrameworkElement) = 
        let handlers = element.GetValue(HandlersProperty) :?> ResizeArray<HandlerExtension>
        let index = handlers.IndexOf(me)
        let observers = element.GetValue(ObserversProperty) :?> obj []
        observers.[index]
    
    let onEvent (sender : obj) (e : RoutedEventArgs) = 
        let mapped = map (e)
        let observer = getObserver (sender :?> FrameworkElement)
        Reflection.invoke observer "OnNext" mapped |> ignore
    
    override this.ProvideValue(serviceProvider : IServiceProvider) = 
        let provideValueTarget = ServiceProvider.ProvideValueTarget serviceProvider
        bindObserver (provideValueTarget.TargetObject :?> FrameworkElement) |> ignore
        RoutedEventHandler onEvent :> _
    
    member __.ObserverBinding = observerBinding
