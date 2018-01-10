namespace FsXaml

open System
open System.Reflection
open System.Xaml
open Microsoft.FSharp.Quotations
open ProviderImplementation.ProvidedTypes

module internal XamlTypeUtils =
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

    let emptyInvokeCode = fun _ -> <@@ () @@>

    let withMethodDocComments comments (m : ProvidedMethod) =
        m.AddXmlDoc comments
        m

    let withPropertyDocComments comments (p : ProvidedProperty) =
        p.AddXmlDoc comments
        p

    let private addAccessorsForElements (providedType : ProvidedTypeDefinition) xamlInfo =          
        let accessorType = typeof<MemberAccessor>
        let accessorMethod =
            match xamlInfo.RootNodeType with
            | FrameworkElement -> "GetNamedMember"
            | ResourceDictionary -> "GetResourceByKey"
            | _ -> failwith "Unsupported node type"
        let elements = xamlInfo.Members
                
        let accessorMethodUntyped = accessorType.GetMethod(accessorMethod, BindingFlags.Static ||| BindingFlags.Public)        

        let createMemberAccessorGetter name (underlyingType : Type) (args:Expr list) =            
            let accessorMethod = accessorMethodUntyped.MakeGenericMethod(underlyingType)
            let this = args.[0]
            let thisAsBaseType = Expr.Coerce(this, providedType.BaseType)         
            let nameOfXamlProperty = Expr.Value(name)
            Expr.Call(accessorMethod, [thisAsBaseType ; nameOfXamlProperty])                        

        for accessorPropertyToCreate in elements do
            let name,xamlType = accessorPropertyToCreate
            let underlyingType, typeName = 
                match xamlType.UnderlyingType with
                | null -> typeof<obj>, xamlType.ToString()
                | t -> t, t.Name            
            let property = 
                ProvidedProperty(name, underlyingType, getterCode = createMemberAccessorGetter name underlyingType)
                |> withPropertyDocComments (sprintf "Gets the %s named %s" typeName name)
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
                ProvidedMethod(name, eventHandlerParams, typeof<System.Void>, invokeCode = emptyInvokeCode)
                |> withMethodDocComments (sprintf "Handles the %s event" name)

            handler.SetMethodAttrs(MethodAttributes.Virtual ||| MethodAttributes.NewSlot ||| MethodAttributes.Public ||| MethodAttributes.Abstract)
            
            providedType.AddMember handler     
                               
    let createProvidedType assembly nameSpace typeName rootTypeInXaml (path, loadFromResource) (initializeComponentMethod : ProvidedMethod) (connectMethod : ProvidedMethod) (initializedField : ProvidedField) xamlInfo =
        let providedType = 
            ProvidedTypeDefinition(assembly, nameSpace, typeName, Some(rootTypeInXaml), isErased = false)
        providedType.AddXmlDoc (sprintf "%s defined in %s" rootTypeInXaml.Name path)
                    
        providedType.AddMember initializeComponentMethod
        providedType.AddMember connectMethod 

        // If our xamlInfo contains event handlers, we write the class as abstract
        let typeAttributes =
            if not (List.isEmpty xamlInfo.Events) then                
                (TypeAttributes.Public ||| TypeAttributes.Class ||| TypeAttributes.Abstract)                                                
            else
                (TypeAttributes.Public ||| TypeAttributes.Class)
        providedType.SetAttributes typeAttributes
                    
        let baseConstructorInfo = rootTypeInXaml.GetConstructor(BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance, null, [| |], null)
        // Constructor calls this.InitializeComponent()
        let providedConstructor = 
            ProvidedConstructor([],
                invokeCode =
                    (fun args ->       
                        match args with
                        | [this] -> Expr.Call(this, initializeComponentMethod, [ ])                                 
                        | _ -> failwith "Wrong constructor arguments"))
        providedConstructor.BaseConstructorCall <- fun args -> baseConstructorInfo, args                         
        providedType.AddMember providedConstructor

        let addAccessors rootNodeType =
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