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

[<assembly:AssemblyVersion("0.9.9.0")>]
[<assembly:AssemblyFileVersion("0.9.9.0")>]
do()


module XamlTypeUtils =
    let internal wpfAssembly = typeof<System.Windows.Controls.Button>.Assembly

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
                    // TODO: Add in other missing resource types as discovered.  For now, Color + Brush types aren't found, and need to be handled
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

    let internal addFrameworkElementAccessorPropertiesToXamlType (typeContainingAccessor : Type) (xamlType : ProvidedTypeDefinition) elements  =
        let pi = typeContainingAccessor.GetProperty("Accessor")
        let mi = typeof<XamlFileAccessor>.GetMethod("GetChild")

        let accessExpr node (args:Expr list) =
            let name = node.Name
            let this = args.[0]
            let thisAsBase = Expr.Coerce(this, typeContainingAccessor)
            let prop = Expr.PropertyGet(thisAsBase, pi)
            let arg = Expr.Value(name)
            let expr = Expr.Call(prop, mi, [arg])
            Expr.Coerce(expr, node.NodeType)

        for node in elements do
            let property = 
                ProvidedProperty(
                    propertyName = node.Name,
                    propertyType = node.NodeType,
                    GetterCode = accessExpr node)
            property.AddXmlDoc(sprintf "Gets the %s element" node.Name)
            property.AddDefinitionLocation(node.Position.Line,node.Position.Column,node.Position.FileName)
            xamlType.AddMember property

    let internal addResourceDictionaryAccessorPropertiesToXamlType (typeContainingAccessor : Type) (xamlType : ProvidedTypeDefinition) elements =
        let pi = typeContainingAccessor.GetProperty("Accessor")        

        let accessExpr node (args:Expr list) =
            let name = node.Name            
            let this = Expr.Coerce(List.head args, typeContainingAccessor)
            let expr = <@@ (%%Expr.PropertyGet(this, pi) : XamlResourceAccessor).GetResource name @@>
            Expr.Coerce(expr, node.NodeType)
        
        for node in elements do
            let property = 
                ProvidedProperty(
                    propertyName = node.Name,
                    propertyType = node.NodeType,
                    GetterCode = accessExpr node)
            property.AddXmlDoc(sprintf "Gets the %s element" node.Name)
            property.AddDefinitionLocation(node.Position.Line,node.Position.Column,node.Position.FileName)
            xamlType.AddMember property        

    let internal addAccessorTypeFromElements (outerType : ProvidedTypeDefinition) elements =
        let root = List.head elements                
                
        // Exclude the Root element from generation
        let elementsToGenerate = 
            elements
            |> Seq.filter (fun x -> not x.IsRoot)

        let accessor = 
            match root.NodeType with
            | app when app = typeof<System.Windows.Application> ->
               addResourceDictionaryAccessorPropertiesToXamlType typeof<XamlAppFactory>
            | rd when rd = typeof<System.Windows.ResourceDictionary> -> 
                addResourceDictionaryAccessorPropertiesToXamlType typeof<XamlResourceFactory>                
            | _ -> 
                addFrameworkElementAccessorPropertiesToXamlType outerType.BaseType
        accessor outerType elementsToGenerate

[<TypeProvider>]
type public XamlTypeProvider(config : TypeProviderConfig) as this = 
    inherit TypeProviderForNamespaces()

    let assembly = Assembly.GetExecutingAssembly()
    let nameSpace = this.GetType().Namespace
    let providerType = ProvidedTypeDefinition(assembly, nameSpace, "XAML", Some typeof<obj>, IsErased = false)
    let fileSystemWatchers = ResizeArray<IDisposable>()
     
    let assemblies = 
        config.ReferencedAssemblies 
        |> Seq.map (fun r -> Assembly.Load(IO.File.ReadAllBytes r))
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
    do 
        providerType.DefineStaticParameters(
            parameters = [ ProvidedStaticParameter("XamlResourceLocation", typeof<string>) ; ProvidedStaticParameter("ExposeNamedProperties", typeof<bool>, false) ], 
            instantiationFunction = (fun typeName parameterValues ->   
                let resourcePath = string parameterValues.[0]
                let exposeParameters = unbox parameterValues.[1]
                let resolvedFileName = findConfigFile config.ResolutionFolder resourcePath
                watchForChanges this resolvedFileName |> Option.iter fileSystemWatchers.Add

                use reader = new StreamReader(resolvedFileName)                            
                let elements = XamlTypeUtils.readElements schemaContext reader resolvedFileName
                let root = List.head elements
                
                let tempAssembly = ProvidedAssembly(Path.ChangeExtension(Path.GetTempFileName(), ".dll"))

                let outerType =
                    let createFactoryType factoryType genericType =
                        let outertype = ProvidedTypeDefinition(assembly, nameSpace, typeName, Some(factoryType), IsErased = false)

                        let ctor = ProvidedConstructor([])
                        let ci = factoryType.GetConstructor(BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance, null, [| genericType ; typeof<string>|], null)
                        ctor.BaseConstructorCall <- fun args -> ci, args @ [Expr.Value(null) ; Expr.Value(resourcePath)]
                        ctor.InvokeCode <- fun _ -> <@@ () @@>
                        outertype.AddMember ctor

                        let ctor = ProvidedConstructor([ProvidedParameter("existingElement", genericType)])
                        ctor.BaseConstructorCall <- fun args -> ci, args @ [Expr.Value("")]
                        ctor.InvokeCode <- fun _ -> <@@ () @@>
                        outertype.AddMember ctor

                        if exposeParameters then
                            XamlTypeUtils.addAccessorTypeFromElements outertype elements

                        outertype

                    match root.NodeType with
                    | win when typeof<System.Windows.Window>.IsAssignableFrom win ->
                        createFactoryType (typedefof<XamlTypeFactory<_>>.MakeGenericType(win)) win
                    | app when typeof<System.Windows.Application>.IsAssignableFrom app ->
                        createFactoryType typeof<XamlAppFactory> app
                    | rd when rd = typeof<System.Windows.ResourceDictionary> ->
                        createFactoryType typeof<XamlResourceFactory> rd
                    | _ ->
                        createFactoryType typeof<XamlContainer> typeof<FrameworkElement>
                        // createContainer()
                
                tempAssembly.AddTypes <| [ outerType ]
                outerType))

        this.AddNamespace(nameSpace, [ providerType ])

    override this.ResolveAssembly(args) = 
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