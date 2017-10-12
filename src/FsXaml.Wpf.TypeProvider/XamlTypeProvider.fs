namespace FsXaml

open System
open System.IO
open System.Reflection

open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Quotations

open ProviderImplementation.ProvidedTypes

open FsXaml.TypeProviders.Helper

[<TypeProvider>]
type public XamlTypeProvider(config : TypeProviderConfig) as this = 
    inherit TypeProviderForNamespaces()

    let assembly = Assembly.GetExecutingAssembly()
    let nameSpace = this.GetType().Namespace
    let ctxt = ProvidedTypesContext.Create(config)
    let providerType = ctxt.ProvidedTypeDefinition(assembly, nameSpace, "XAML", Some typeof<obj>, isErased = false)

    let fileSystemWatchers = ResizeArray<IDisposable>()
     
    let assemblies = 
        config.ReferencedAssemblies
        |> Seq.choose (fun asm -> 
            try
                // This is a fix for #34: Previously, 64bit libraries could fail to load, which would make the type provider fail.
                // By filtering out assemblies that don't load, we should still allow all relevent WPF assemblies to load
                asm |> (IO.File.ReadAllBytes >> Assembly.Load >> Some)
            with 
            | _ -> None)
        |> Seq.append [XamlTypeUtils.wpfAssembly]        
        |> Array.ofSeq
        
    do
        this.Disposing.Add((fun _ ->
            for watcher in fileSystemWatchers do
                watcher.Dispose() 
            fileSystemWatchers.Clear()
        ))
    
        providerType.DefineStaticParameters(
            parameters =
                [
                    ctxt.ProvidedStaticParameter("XamlResourceLocation", typeof<string>, parameterDefaultValue = "")
                    ctxt.ProvidedStaticParameter("XamlFileLocation",     typeof<string>, parameterDefaultValue = "")
                ], 
            instantiationFunction = (fun typeName parameterValues ->   
                let resourcePath = string parameterValues.[0]
                let filePath     = string parameterValues.[1]

                let path, loadFromResource =
                    match resourcePath = "", filePath = "" with
                    |  true, true  ->
                        failwith "Must specify either XamlResourceLocation or XamlFileLocation (but not both)"
                    | false, false ->
                        failwith "Can't specify both XamlResourceLocation and XamlFileLocation (but must specify one or the other)"
                    |  true, false -> filePath, false
                    | false, true  -> resourcePath, true

                let resolvedFileName = findConfigFile config.ResolutionFolder path
                watchForChanges this resolvedFileName |> Option.iter fileSystemWatchers.Add

                use reader = File.OpenRead resolvedFileName
                let xamlInfo = XamlParser.parseXaml resourcePath reader
                let rootTypeInXaml = xamlInfo.RootType.UnderlyingType

                let assemblyPath =
                    let tempFolderName = Path.GetTempPath()                    
                    let filename = "fsxaml_" + Path.GetRandomFileName() + ".dll"
                    Path.Combine(tempFolderName, filename)
                                            
                let providedAssembly = ProvidedAssembly(ctxt)                
                
                // Implement IComponentConnector                
                let iComponentConnectorType = typeof<System.Windows.Markup.IComponentConnector>

                // Create a field for tracking whether we're initialized
                let initializedField = ctxt.ProvidedField(XamlTypeUtils.InitializedComponentFieldName, typeof<bool>)

                // Make InitializeComponent public, since that matches C# expectations
                // However, we're making it virtual, so subclasses can do extra work before/after if desired
                let initializeComponentInterface = iComponentConnectorType.GetMethod("InitializeComponent")
                let connectInterface = iComponentConnectorType.GetMethod("Connect")

                let initializeComponentMethod = 
                    ctxt.ProvidedMethod("InitializeComponent", [ ], typeof<System.Void>, 
                            invokeCode =
                                fun args ->
                                    match args with 
                                    | [this] ->
                                        let o = Expr.Coerce(this, typeof<obj>)
                                        let isInit = Expr.FieldGet(this, initializedField)
                                        let setInit = Expr.FieldSet(this, initializedField, Expr.Value(true))
                                        <@@
                                            if (not (%%isInit : bool)) then
                                                (%%setInit)                                            
                                                InjectXaml.from path loadFromResource (%%o : obj)                                            
                                        @@>
                                    | _ -> failwith "Wrong constructor arguments")
                                        
                    |> XamlTypeUtils.asInterfaceImplementation
                let connectMethod = 
                    ctxt.ProvidedMethod("Connect", [ ctxt.ProvidedParameter("connectionId", typeof<int>) ; ctxt.ProvidedParameter("target", typeof<obj>) ], typeof<System.Void>, invokeCode = XamlTypeUtils.emptyInvokeCode)
                    |> XamlTypeUtils.asInterfaceImplementation

                let generatedType = 
                    XamlTypeUtils.createProvidedType ctxt assembly nameSpace typeName rootTypeInXaml (path, loadFromResource) initializeComponentMethod initializedField xamlInfo 

                generatedType.AddMember initializedField                

                // Wire up IComponentConnector
                generatedType.AddInterfaceImplementation iComponentConnectorType
                generatedType.DefineMethodOverride(initializeComponentMethod, initializeComponentInterface)
                generatedType.AddMember initializeComponentMethod
                generatedType.DefineMethodOverride(connectMethod, connectInterface)
                generatedType.AddMember connectMethod 

                // Add the type to our assembly
                providedAssembly.AddTypes <| [ generatedType ]
                generatedType))


        this.AddNamespace(nameSpace, [ providerType ])

    override __.ResolveAssembly(args) = 
        let name = System.Reflection.AssemblyName(args.Name)
        let existingAssembly = 
            System.AppDomain.CurrentDomain.GetAssemblies()
            |> Seq.tryFind(fun a -> System.Reflection.AssemblyName.ReferenceMatchesDefinition(name, a.GetName()))
        match existingAssembly with
        | Some a -> a
        | None -> 
            // Fallback to default behavior
            base.ResolveAssembly(args)
        
[<assembly:TypeProviderAssembly>] 
do()