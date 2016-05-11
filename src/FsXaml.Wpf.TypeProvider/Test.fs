namespace VectorTP

open System
open System.Reflection
open Microsoft.FSharp.Core.CompilerServices

module TypeProviders =
    module Parameters =
        let createString name defaultValue optional =
            { new ParameterInfo() with
                override __.Name with get() = name
                override __.ParameterType with get() = typeof<string>
                override __.Position with get() = 0
                override __.RawDefaultValue with get() = defaultValue
                override __.DefaultValue with get() = defaultValue
                override __.Attributes with get() = if optional then ParameterAttributes.Optional else ParameterAttributes.None
            }
    
[<AbstractClass>]
type GenerateTypeProviderBase(namespaceName) =
    let invalidate = new Event<_,_>()
    let disposing = new Event<EventHandler,EventArgs>()
    let types = System.Collections.Generic.Dictionary<_,_>()

    abstract member GetStaticParameters : Type -> ParameterInfo array
    abstract member ApplyStaticArguments : Type * string[] * obj[] -> Type

    member __.AddType (newType : Type) = types.Add(newType.Name, newType)

    // This allows subclasses to hook into the disposal chain as needed
    [<CLIEvent>]
    member __.Disposing = disposing.Publish

    interface IProvidedNamespace with
        member __.ResolveTypeName(typeName) =
            match types.TryGetValue(typeName) with
            | false, _ -> failwith (sprintf "Type %s not specified" typeName)
            | true, t -> t

        member __.NamespaceName with get() = namespaceName
        
        member __.GetNestedNamespaces() =
            // We're not going to support nested namespaces (for now)
            [| |]

        member __.GetTypes() = 
            types.Values
            |> Array.ofSeq

    interface ITypeProvider with        
        member this.GetNamespaces() = 
            // We only support a single namespace, so return ourselves
            [| this |]
        
        member this.GetStaticParameters typeWithoutArguments = this.GetStaticParameters typeWithoutArguments
        member this.ApplyStaticArguments(typeWithoutArguments, typeNameWithArguments, staticArguments) = this.ApplyStaticArguments (typeWithoutArguments, typeNameWithArguments, staticArguments)

        member __.GetInvokerExpression(syntheticMethodBase, parameters) =
            // this method only needs to be implemented for a generated type provider if
            // you are using the declared types from in the same project or in a script
            //NotImplementedException(sprintf "Not Implemented: ITypeProvider.GetInvokerExpression(%A, %A)" syntheticMethodBase parameters) |> raise
            match syntheticMethodBase with
            | :? ConstructorInfo as ctor -> Quotations.Expr.NewObject(ctor, Array.toList parameters) 
            | :? MethodInfo as mi -> Quotations.Expr.Call(parameters.[0], mi, Array.toList parameters.[1..])
            | _ -> 
                raise 
                <| NotImplementedException(sprintf "Not Implemented: ITypeProvider.GetInvokerExpression(%A, %A)" syntheticMethodBase parameters)

        member __.GetGeneratedAssemblyContents(assembly) = IO.File.ReadAllBytes assembly.ManifestModule.FullyQualifiedName
        
        [<CLIEvent>]
        member __.Invalidate = invalidate.Publish                
        member this.Dispose() = disposing.Trigger(this, EventArgs.Empty)


module Helpers =
    let stringParameter index defaultVal =
        { new ParameterInfo() with
            override this.Name with get() = sprintf "axis%d" index
            override this.ParameterType with get() = typeof<string>
            override this.Position with get() = 0
            override this.RawDefaultValue with get() = defaultVal
            override this.DefaultValue with get() = defaultVal
            override this.Attributes with get() = ParameterAttributes.Optional
        }

    let makeClass body name =
        let code = "namespace Mindscape.Vectorama { public class " + name + " {" + Environment.NewLine + body + Environment.NewLine + "} }"
        let dllFile = System.IO.Path.GetTempFileName() + ".tp.dll"
        let csc = new Microsoft.CSharp.CSharpCodeProvider()
        let parameters = new System.CodeDom.Compiler.CompilerParameters()
        parameters.OutputAssembly <- dllFile
        parameters.CompilerOptions <- "/t:library"
        // Ignoring error checking
        let compilerResults = csc.CompileAssemblyFromSource(parameters, [| code |])
        let asm = compilerResults.CompiledAssembly
        asm.GetType("Mindscape.Vectorama." + name)

    let makeVector name argnames =
        let propNames =
            argnames
            |> Seq.filter (fun arg -> arg <> null && not (String.IsNullOrWhiteSpace(arg.ToString())))
            |> Seq.map (fun arg -> arg.ToString())
            |> Seq.toList
        let props =
            propNames
            |> List.map (fun arg -> "public double " + arg + " { get; set; }")
            |> String.concat Environment.NewLine
        let dotProductBody =
            propNames
            |> List.map (fun arg -> sprintf "this.%s * other.%s" arg arg)
            |> String.concat " + "
        let dotProduct = sprintf "public double DotProduct(%s other) { return %s; }" name dotProductBody
        let body = props + Environment.NewLine + dotProduct
        makeClass body name
 
open Helpers

// Cheating for simplicity
type Vector() = inherit obj()


[<TypeProvider>]
type VectorProvider() as self =
    inherit GenerateTypeProviderBase("Mindscape.Vectorama")

    do
        self.AddType typeof<Vector>

    override __.GetStaticParameters _ =
        [1..7] 
        |> List.map (fun i -> TypeProviders.Parameters.createString (sprintf "axis%d" i) "" true) 
        |> List.toArray
    
    override __.ApplyStaticArguments (_, typeNameWithArguments, staticArguments) =
        makeVector (Seq.last typeNameWithArguments) staticArguments
 
