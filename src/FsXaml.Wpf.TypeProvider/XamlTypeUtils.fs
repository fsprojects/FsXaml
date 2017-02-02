namespace FsXaml

open System
open System.Reflection
open System.Xaml
open Microsoft.FSharp.Quotations
open ProviderImplementation.ProvidedTypes

module internal XamlTypeUtils =
    let wpfAssembly = typeof<System.Windows.Controls.Button>.Assembly

    [<Literal>]
    let AccessorName = "__xaml_accessor";

    [<Literal>]
    let InitializedComponentFieldName = "__components_initialized";  

    let asInterfaceImplementation (m : ProvidedMethod) =
        m.SetMethodAttrs( 
            MethodAttributes.Private 
            ||| MethodAttributes.HideBySig 
            ||| MethodAttributes.NewSlot 
            ||| MethodAttributes.Virtual 
            ||| MethodAttributes.Final)
        m

    let withEmptyInvokeCode (m : ProvidedMethod) =
        m.InvokeCode <- fun _ -> <@@ () @@>
        m

    let withMethodDocComments comments (m : ProvidedMethod) =
        m.AddXmlDoc comments
        m

    let withPropertyDocComments comments (p : ProvidedProperty) =
        p.AddXmlDoc comments
        p

    let private addAccessorsForElements (providedType : ProvidedTypeDefinition) xamlInfo =          
        let accessorType =
            match xamlInfo.RootNodeType with
            | FrameworkElement -> typeof<NamedNodeAccessor>                              
            | ResourceDictionary -> typeof<KeyNodeAccessor>                              
            | _ -> failwith "Unsupported node type"
        let elements = xamlInfo.Members
        
        let getAccessorField = providedType.GetField(AccessorName, BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance)
        let accessorMethod = accessorType.GetMethod("GetChild")

        let createMemberAccessorGetter (node : string * XamlType) (args:Expr list) =
            let name,xamlType = node
            let this = args.[0]
            let thisAsBaseType = Expr.Coerce(this, providedType.BaseType)            
            let accessorField = Expr.FieldGet(this, getAccessorField)
            let nameOfXamlProperty = Expr.Value(name)
            let callAccessorMethod = Expr.Call(accessorField, accessorMethod, [thisAsBaseType ; nameOfXamlProperty])
            let castToProperType = Expr.Coerce(callAccessorMethod, xamlType.UnderlyingType)
            castToProperType 

        for accessorPropertyToCreate in elements do
            let name,xamlType = accessorPropertyToCreate
            let property = 
                ProvidedProperty(name, xamlType.UnderlyingType, GetterCode = createMemberAccessorGetter accessorPropertyToCreate)
                |> withPropertyDocComments (sprintf "Gets the %s named %s" xamlType.UnderlyingType.Name name)
            providedType.AddMember property          

    let private addEventHandler (providedType : ProvidedTypeDefinition) name (xamlType : XamlType) =
        let eventHandlerType = xamlType.UnderlyingType
        // Sanity check that we're actually an event handler
        if eventHandlerType.BaseType.IsAssignableFrom(typeof<MulticastDelegate>) then
            let invokeMethodInfo = eventHandlerType.GetMethod "Invoke"
            let eventHandlerParams =
                invokeMethodInfo.GetParameters()
                |> Array.map (fun pi -> ProvidedParameter(pi.Name, pi.ParameterType))
                |> List.ofArray
            let handler = 
                ProvidedMethod(name, eventHandlerParams, typeof<System.Void>)
                |> withEmptyInvokeCode
                |> withMethodDocComments (sprintf "Handles the %s event" name)

            handler.SetMethodAttrs(MethodAttributes.Virtual ||| MethodAttributes.NewSlot ||| MethodAttributes.Public ||| MethodAttributes.Abstract)
            
            providedType.AddMember handler     
                               
    let createProvidedType assembly nameSpace typeName rootTypeInXaml resourcePath (initializeComponentMethod : ProvidedMethod) (initializedField : ProvidedField) xamlInfo =
        let providedType = ProvidedTypeDefinition(assembly, nameSpace, typeName, Some(rootTypeInXaml), IsErased = false)
        providedType.AddXmlDoc (sprintf "%s defined in %s" rootTypeInXaml.Name resourcePath)
                    
        // If our xamlInfo contains event handlers, we write the class as abstract
        let typeAttributes =
            if not (List.isEmpty xamlInfo.Events) then                
                (TypeAttributes.Public ||| TypeAttributes.Class ||| TypeAttributes.Abstract)                                                
            else
                (TypeAttributes.Public ||| TypeAttributes.Class)
        providedType.SetAttributes typeAttributes
                    
        let baseConstructorInfo = rootTypeInXaml.GetConstructor(BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance, null, [| |], null)
        let providedConstructor = ProvidedConstructor([])
        providedConstructor.BaseConstructorCall <- fun args -> baseConstructorInfo, args                         
        // Constructor calls this.InitializeComponent()
        providedConstructor.InvokeCode <-
            fun args ->       
                match args with
                | [this] -> Expr.Call(this, initializeComponentMethod, [ ])                                 
                | _ -> failwith "Wrong constructor arguments"
        providedType.AddMember providedConstructor

        // Setup InitializeComponent now
        initializeComponentMethod.InvokeCode <-
            fun args ->
                match args with 
                | [this] ->
                    let o = Expr.Coerce(this, typeof<obj>)
                    let isInit = Expr.FieldGet(this, initializedField)
                    let setInit = Expr.FieldSet(this, initializedField, Expr.Value(true))
                    <@@
                        if (not (%%isInit : bool)) then
                            (%%setInit)                                            
                            InjectXaml.from resourcePath (%%o : obj)                                            
                    @@>
                | _ -> failwith "Wrong constructor arguments"
                                        
        let addAccessors rootNodeType =
            let accessorType = 
                match rootNodeType with
                | RootNodeType.ResourceDictionary -> typeof<KeyNodeAccessor>
                | RootNodeType.FrameworkElement   -> typeof<NamedNodeAccessor>
                | _ -> typeof<obj>
            let accessorField = ProvidedField(AccessorName, accessorType)
            providedType.AddMember accessorField                        
            addAccessorsForElements providedType xamlInfo
                            
        // If we're a framework element (UserControl/Window/etc), we can add named elements,
        // otherwise, we don't bother
        match xamlInfo.RootNodeType, xamlInfo.Members with
        | _, [] -> ()
        | RootNodeType.FrameworkElement, _   -> addAccessors RootNodeType.FrameworkElement
        | RootNodeType.ResourceDictionary, _ -> addAccessors RootNodeType.ResourceDictionary
        | _ -> ()

        // Add our event handlers
        xamlInfo.Events |> List.iter (fun (n, typ) -> addEventHandler providedType n typ)                     

        providedType