namespace FsXaml

open System
open System.IO

open System.Xaml
open System.Windows

open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Quotations

open ProviderImplementation.ProvidedTypes
open FsXaml.TypeProviders.Helper

open System.Reflection


module XamlTypeUtils =
    let internal wpfAssembly = typeof<System.Windows.Controls.Button>.Assembly

    [<Literal>]
    let internal AccessorName = "__xaml_accessor";

    [<Literal>]
    let internal InitializedComponentFieldName = "__components_initialized";  

    let internal addFrameworkElementAccessorPropertiesToXamlType (typeContainingAccessor : Type) (xamlType : ProvidedTypeDefinition) (accessorType : Type) (elements : (string * XamlType) list) =
        let fi = typeContainingAccessor.GetField(AccessorName, BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance)
        let mi = accessorType.GetMethod("GetChild")

        let accessExpr (node : string * XamlType) (args:Expr list) =
            let name,tp = node
            let this = args.[0]
            let thisAsBase = Expr.Coerce(this, typeContainingAccessor)
            let field = Expr.FieldGet(thisAsBase, fi)
            let arg = Expr.Value(name)
            let expr = Expr.Call(field, mi, [arg])
            Expr.Coerce(expr, tp.UnderlyingType)

        for node in elements do
            let name,tp = node
            let property = 
                ProvidedProperty(
                    propertyName = name,
                    propertyType = tp.UnderlyingType,
                    GetterCode = accessExpr node)
            xamlType.AddMember property          

    let internal addAccessorTypeFromElements (outerType : ProvidedTypeDefinition) xamlInfo =          
        let accessorType =
            match xamlInfo.RootNodeType with
            | FrameworkElement -> typeof<NamedNodeAccessor>                              
            | ResourceDictionary -> typeof<KeyNodeAccessor>                              
            | _ -> failwith "Unsupported node type"
        addFrameworkElementAccessorPropertiesToXamlType outerType outerType accessorType xamlInfo.Members

[<TypeProvider>]
type public XamlTypeProvider(config : TypeProviderConfig) as this = 
    inherit TypeProviderForNamespaces()

    let assembly = Assembly.GetExecutingAssembly()
    let nameSpace = this.GetType().Namespace
    let providerType = ProvidedTypeDefinition(assembly, nameSpace, "XAML", Some typeof<obj>, IsErased = false)

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
            parameters = [ ProvidedStaticParameter("XamlResourceLocation", typeof<string>) ], 
            instantiationFunction = (fun typeName parameterValues ->   
                let resourcePath = string parameterValues.[0]                
                let resolvedFileName = findConfigFile config.ResolutionFolder resourcePath
                watchForChanges this resolvedFileName |> Option.iter fileSystemWatchers.Add

                use reader = File.OpenRead resolvedFileName
                let xamlInfo = XamlParser.parseXaml reader
                let rootType = xamlInfo.RootType.UnderlyingType

                let assemblyPath =
                    let tempFolderName = Path.GetTempPath()                    
                    let filename = "fsxaml_" + Path.GetRandomFileName() + ".dll"
                    Path.Combine(tempFolderName, filename)
                                            
                let tempAssembly = ProvidedAssembly(assemblyPath)

                let outerType (ic : ProvidedMethod) (initialized : ProvidedField) (oninitialize : ProvidedMethod) =
                    let providedType = ProvidedTypeDefinition(assembly, nameSpace, typeName, Some(rootType), IsErased = false)
                    providedType.SetAttributes (TypeAttributes.Public ||| TypeAttributes.Class)
                    let baseConstructorInfo = rootType.GetConstructor(BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance, null, [| |], null)
                    let ctor = ProvidedConstructor([])
                    ctor.BaseConstructorCall <- fun args -> baseConstructorInfo, args                         
                    // Constructor calls this.InitializeComponent()
                    ctor.InvokeCode <-
                        fun args ->       
                            match args with
                            | [this] -> Expr.Call(this, ic, [ ])                                 
                            | _ -> failwith "Wrong constructor arguments"
                    providedType.AddMember ctor
                    // Setup InitializeComponent now
                    let setupWithoutAccessors () =
                        ic.InvokeCode <-
                            fun args ->
                                match args with 
                                | [this] ->
                                    let o = Expr.Coerce(this, typeof<obj>)
                                    let isInit = Expr.FieldGet(this, initialized)
                                    let setInit = Expr.FieldSet(this, initialized, Expr.Value(true))
                                    <@@
                                        if (not (%%isInit : bool)) then
                                            (%%setInit)                                            
                                            InjectXaml.from resourcePath (%%o : obj)
                                            (%%Expr.Call(this, oninitialize, [ ]))
                                    @@>
                                | _ -> failwith "Wrong constructor arguments"
                    let setupWithAccessors rootNodeType =
                        let accessorType, accessorConstructorArgType = 
                            match rootNodeType with
                            | RootNodeType.ResourceDictionary -> 
                                typeof<KeyNodeAccessor>, typeof<ResourceDictionary>
                            | RootNodeType.FrameworkElement ->
                                typeof<NamedNodeAccessor>, typeof<FrameworkElement>
                        let accessorField = ProvidedField(XamlTypeUtils.AccessorName, accessorType)
                        providedType.AddMember accessorField

                        ic.InvokeCode <-
                            fun args ->
                                match args with 
                                | [this] ->
                                    let o = Expr.Coerce(this, typeof<obj>)
                                    let isInit = Expr.FieldGet(this, initialized)
                                    let setInit = Expr.FieldSet(this, initialized, Expr.Value(true))
                                    let accessCtor = accessorType.GetConstructor([| accessorConstructorArgType |])
                                    let accessCtorArg = Expr.Coerce(this, accessorConstructorArgType)
                                    let access = Expr.NewObject(accessCtor, [ accessCtorArg ])
                                    let setfield = Expr.FieldSet(this, accessorField, access)
                                    <@@
                                        if (not (%%isInit : bool)) then                                            
                                            (%%setInit)
                                            (%%setfield)                                    
                                            InjectXaml.from resourcePath (%%o : obj)
                                            (%%Expr.Call(this, oninitialize, [ ]))
                                    @@>
                                | _ -> failwith "Wrong constructor arguments"
                        XamlTypeUtils.addAccessorTypeFromElements providedType xamlInfo
                            
                    // If we're a framework element (UserControl/Window/etc), we can add named elements,
                    // otherwise, we don't bother
                    match xamlInfo.RootNodeType, xamlInfo.Members with
                    | _, [] -> setupWithoutAccessors()
                    | RootNodeType.FrameworkElement, _ -> setupWithAccessors RootNodeType.FrameworkElement
                    | RootNodeType.ResourceDictionary, _ -> setupWithAccessors RootNodeType.ResourceDictionary
                    | _ -> setupWithoutAccessors()

                    providedType
                
                // Implement IComponentConnector                
                let icc = typeof<System.Windows.Markup.IComponentConnector>

                let initialized = ProvidedField(XamlTypeUtils.InitializedComponentFieldName, typeof<bool>)

                let oninitialize = ProvidedMethod("OnInitialize", [ ], typeof<System.Void>)
                oninitialize.SetMethodAttrs(MethodAttributes.Virtual ||| MethodAttributes.NewSlot ||| MethodAttributes.Public)
                oninitialize.InvokeCode <- fun _ -> <@@ () @@>                

                // Make InitializeComponent public, since that matches C# expectations
                // However, we're making it virtual, so subclasses can do extra work before/after if desired
                let icc_ic = icc.GetMethod("InitializeComponent")
                let ic = ProvidedMethod("InitializeComponent", [ ], typeof<System.Void>)
                ic.SetMethodAttrs( 
                    MethodAttributes.Private 
                    ||| MethodAttributes.HideBySig 
                    ||| MethodAttributes.NewSlot 
                    ||| MethodAttributes.Virtual 
                    ||| MethodAttributes.Final)
                let icc_con = icc.GetMethod("Connect")

                let con = ProvidedMethod("Connect", [ ProvidedParameter("connectionId", typeof<int>) ; ProvidedParameter("target", typeof<obj>) ], typeof<System.Void>)
                con.SetMethodAttrs( 
                    MethodAttributes.Private 
                    ||| MethodAttributes.HideBySig 
                    ||| MethodAttributes.NewSlot 
                    ||| MethodAttributes.Virtual 
                    ||| MethodAttributes.Final)

                let outerType = outerType ic initialized oninitialize
                
                let createHandler name (typ : XamlType) =
                    let eht = typ.UnderlyingType
                    if eht.BaseType.IsAssignableFrom(typeof<MulticastDelegate>) then
                        let inv = eht.GetMethod "Invoke"
                        let evParams =
                            inv.GetParameters()
                            |> Array.map (fun pi -> ProvidedParameter(pi.Name, pi.ParameterType))
                            |> List.ofArray
                        let m = ProvidedMethod(name, evParams, typeof<System.Void>)
                        m.SetMethodAttrs(MethodAttributes.Virtual ||| MethodAttributes.NewSlot ||| MethodAttributes.Public)
                        m.InvokeCode <- fun _ -> <@@ () @@>
                        outerType.AddMember m
                        Some m
                    else
                        None

                let handlers = xamlInfo.Events
                        
                handlers 
                    |> List.map (fun (n, typ) -> createHandler n typ) 
                    |> ignore // TODO: Remove this?

                con.InvokeCode <- fun _ -> <@@ () @@>

                outerType.AddMember initialized
                outerType.AddMember oninitialize

                outerType.AddInterfaceImplementation icc
                outerType.DefineMethodOverride(ic, icc_ic)
                outerType.AddMember ic

                outerType.DefineMethodOverride(con, icc_con)
                outerType.AddMember con 

                tempAssembly.AddTypes <| [ outerType ]
                outerType))


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