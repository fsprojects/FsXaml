namespace FsXaml

open System
open System.IO
open System.Xml
open System.Windows

open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Quotations

open ProviderImplementation.ProvidedTypes
open FsXaml.TypeProviders.Helper

open System.Reflection

[<assembly:AssemblyVersion("1.9.0.4")>]
[<assembly:AssemblyFileVersion("1.9.0.4")>]
do()

module XamlTypeUtils =
    let internal wpfAssembly = typeof<System.Windows.Controls.Button>.Assembly

    [<Literal>]
    let internal AccessorName = "__xaml_accessor";

    type internal FilePosition =  
       { Line: int; 
         Column: int;
         FileName: string }

    type internal XamlNode =
        { Position: FilePosition
          IsRoot: bool
          Name: string
          NodeType : Type }

    let internal posOfReader filename (xaml:XmlReader) = 
        let lineInfo = xaml :> obj :?> IXmlLineInfo
        { Line = lineInfo.LineNumber
          Column = lineInfo.LinePosition
          FileName = filename }
    
    // I didn't remove support for Application/ResourceDictionary here.
    // It might be nice to try to reimplement this if/when we figure out
    // how to make it more robust.
    type RootNodeType =
    | FrameworkElement
    | ResourceDictionary
    | Application

    let internal createXamlNode (schemaContext: Xaml.XamlSchemaContext) filename isRoot (xaml:XmlReader) (rootNodeType : RootNodeType option) =
        let pos = posOfReader filename xaml
        try 
            let name =                        
                match rootNodeType with
                | Some ResourceDictionary 
                | Some Application ->
                    match xaml.GetAttribute("x:Key") with
                    | name when not(String.IsNullOrWhiteSpace(name)) -> Some name
                    | _ -> if isRoot then Some "Root" else None
                | Some FrameworkElement ->
                    match xaml.GetAttribute("Name") with
                    | name when not(String.IsNullOrWhiteSpace(name)) -> Some name
                    | _ ->
                        match xaml.GetAttribute("x:Name") with
                        | name when not(String.IsNullOrWhiteSpace(name)) -> Some name
                        | _ -> if isRoot then Some "Root" else None
                | None ->
                    match xaml.GetAttribute("x:Name") with
                    | name when not(String.IsNullOrWhiteSpace(name)) -> Some name
                    | _ -> if isRoot then Some "Root" else None

            match name with
            | None -> None
            | Some name -> 
                let propertyType =
                    // Add in other missing resource types as discovered.  For now, Color + Brush types aren't found, and need to be handled
                    match xaml.LocalName with
                    | "Color" -> typeof<System.Windows.Media.Color>
                    | "SolidColorBrush" -> typeof<System.Windows.Media.SolidColorBrush>
                    | "BitmapCacheBrush" -> typeof<System.Windows.Media.BitmapCacheBrush>
                    | "LinearGradientBrush" -> typeof<System.Windows.Media.LinearGradientBrush>
                    | "RadialGradientBrush" -> typeof<System.Windows.Media.RadialGradientBrush>
                    | "DrawingBrush" -> typeof<System.Windows.Media.DrawingBrush>
                    | "ImageBrush" -> typeof<System.Windows.Media.ImageBrush>
                    | "VisualBrush" -> typeof<System.Windows.Media.VisualBrush>
                    | _ ->
                        let r = schemaContext.GetAllXamlTypes(xaml.NamespaceURI)
                        let xamltype = r |> Seq.tryFind (fun xt -> xt.Name = xaml.LocalName)
                        match xamltype with
                        | None   -> typeof<obj>
                        | Some t -> t.UnderlyingType
                { Position = pos
                  IsRoot = isRoot
                  Name = name
                  NodeType = propertyType }
                |> Some
        with
        | :? XmlException -> failwithf "Error near %A" pos

    let internal readXamlFile (schemaContext: Xaml.XamlSchemaContext) filename (xaml:XmlReader) =    
        seq {
            let isRoot = ref true
            let fileType = ref FrameworkElement
            while xaml.Read() do
                match xaml.NodeType with
                | XmlNodeType.Element ->
                    match !isRoot with
                    | true -> 
                        let node = createXamlNode schemaContext filename (!isRoot) xaml None
                        match node with
                        | Some node ->
                            yield node
                            
                            // If we're a RD or application change us
                            if (node.NodeType = typeof<ResourceDictionary>) then 
                                fileType := ResourceDictionary
                            else if (node.NodeType = typeof<Application>) then 
                                fileType := Application
                            isRoot := false
                        | None -> ()
                    | false -> 
                        let node = createXamlNode schemaContext filename (!isRoot) xaml (Some !fileType)
                        match node with
                        | Some node -> yield node
                        | None -> ()
                | XmlNodeType.EndElement | XmlNodeType.Comment | XmlNodeType.Text -> ()
                | unexpected -> failwithf "Unexpected node type %A at %A" unexpected (posOfReader filename xaml) }

    let createXmlReader(textReader:TextReader) =
        XmlReader.Create(textReader, XmlReaderSettings(IgnoreProcessingInstructions = true, IgnoreWhitespace = true))

    let internal readElements (schemaContext: Xaml.XamlSchemaContext) (reader: TextReader) fileName =
        let elements = 
            reader
            |> createXmlReader 
            |> readXamlFile schemaContext fileName
            |> Seq.toList
        elements    

    let internal addFrameworkElementAccessorPropertiesToXamlType (typeContainingAccessor : Type) (xamlType : ProvidedTypeDefinition) elements =
        let fi = typeContainingAccessor.GetField(AccessorName, BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance)
        let mi = typeof<NamedNodeAccessor>.GetMethod("GetChild")

        let accessExpr node (args:Expr list) =
            let name = node.Name
            let this = args.[0]
            let thisAsBase = Expr.Coerce(this, typeContainingAccessor)
            let field = Expr.FieldGet(thisAsBase, fi)
            let arg = Expr.Value(name)
            let expr = Expr.Call(field, mi, [arg])
            Expr.Coerce(expr, node.NodeType)

        for node in elements do
            let property = 
                ProvidedProperty(
                    propertyName = node.Name,
                    propertyType = node.NodeType,
                    GetterCode = accessExpr node)
            // property.AddXmlDoc(sprintf "Gets the %s element" node.Name)
            // property.AddDefinitionLocation(node.Position.Line,node.Position.Column,node.Position.FileName)
            xamlType.AddMember property            

    let internal addAccessorTypeFromElements (outerType : ProvidedTypeDefinition) elements =        
        let root = List.head elements                
                
        // Exclude the Root element from generation
        let elementsToGenerate = 
            elements
            |> Seq.filter (fun x -> not x.IsRoot)

        addFrameworkElementAccessorPropertiesToXamlType outerType outerType elementsToGenerate 

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
        
    let ss = 
        let scontext = Xaml.XamlSchemaContextSettings()
        scontext.FullyQualifyAssemblyNamesInClrNamespaces <- false
        scontext.SupportMarkupExtensionsWithDuplicateArity <- false
        scontext

    let schemaContext = System.Xaml.XamlSchemaContext(assemblies, ss)

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

                use reader = new StreamReader(resolvedFileName)                            
                let elements = XamlTypeUtils.readElements schemaContext reader resolvedFileName
                let root = List.head elements
                
                let assemblyPath =
                    let tempFolderName = Path.GetTempPath()                    
                    let filename = "fsxaml_" + Path.GetRandomFileName() + ".dll"
                    Path.Combine(tempFolderName, filename)
                                            
                let tempAssembly = ProvidedAssembly(assemblyPath)

                let outerType =
                    let createWithoutAccessors feType =                        
                        let providedType = ProvidedTypeDefinition(assembly, nameSpace, typeName, Some(feType), IsErased = false)
                        providedType.SetAttributes (TypeAttributes.Public ||| TypeAttributes.Class)
                        let baseConstructorInfo = feType.GetConstructor(BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance, null, [| |], null)
                        let ctor = ProvidedConstructor([])
                        ctor.BaseConstructorCall <- fun args -> baseConstructorInfo, args                         

                        ctor.InvokeCode <-
                            fun args ->       
                                match args with
                                | [this] ->
                                    let o = Expr.Coerce(this, typeof<obj>)
                                    <@@
                                        InjectXaml.from resourcePath (%%o : obj)
                                    @@>
                                | _ -> failwith "Wrong constructor arguments"

                        providedType.AddMember ctor
                        providedType

                    let createWithAccessors feType =
                        let accessorType = typeof<NamedNodeAccessor>
                        let accessorConstructorArgType = typeof<FrameworkElement>
                        
                        let providedType = ProvidedTypeDefinition(assembly, nameSpace, typeName, Some(feType), IsErased = false)
                        providedType.SetAttributes (TypeAttributes.Public ||| TypeAttributes.Class)
                        let baseConstructorInfo = feType.GetConstructor(BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance, null, [| |], null)
                        let ctor = ProvidedConstructor([])
                        ctor.BaseConstructorCall <- fun args -> baseConstructorInfo, args                         

                        let addingNamedElements = elements |> Seq.exists (fun x -> not x.IsRoot)
                        
                        match addingNamedElements with
                        | false -> 
                            // If we didn't add any named elements, we don't need an accessor field or constructor info
                            ctor.InvokeCode <- 
                                fun args ->
                                    match args with
                                    | [this] ->                                          
                                        let o = Expr.Coerce(this, typeof<obj>)                                            
                                        <@@                                                        
                                            InjectXaml.from resourcePath (%%o : obj)
                                        @@>
                                    | _ -> failwith "Wrong constructor arguments"
                        | true ->
                            // We added named elements, so add the accessor field we need
                            let accessorField = ProvidedField(XamlTypeUtils.AccessorName, accessorType)
                            providedType.AddMember accessorField
                            ctor.InvokeCode <- 
                                fun args ->
                                    match args with
                                    | [this] ->                                          
                                        let o = Expr.Coerce(this, typeof<obj>)                                            
                                        let accessCtor = accessorType.GetConstructor([| accessorConstructorArgType |])
                                        let accessCtorArg = Expr.Coerce(this, accessorConstructorArgType)
                                        let access = Expr.NewObject(accessCtor, [ accessCtorArg ])
                                        let setfield = Expr.FieldSet(this, accessorField, access)
                                        <@@
                                            (%%setfield)                                    
                                            InjectXaml.from resourcePath (%%o : obj)
                                        @@>
                                    | _ -> failwith "Wrong constructor arguments"

                            XamlTypeUtils.addAccessorTypeFromElements providedType elements                        

                        providedType.AddMember ctor
                        
                        providedType

                    // If we're a framework element (UserControl/Window/etc), we can add named elements,
                    // otherwise, we don't bother
                    match root.NodeType with
                    | fe when typeof<FrameworkElement>.IsAssignableFrom fe -> createWithAccessors fe                         
                    | other -> createWithoutAccessors other
                
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