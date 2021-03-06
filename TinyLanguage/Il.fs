﻿module Il

open System
open System.Reflection
open System.Reflection.Emit
open BindingTree
open Binder

type Instruction = 
    | Add 
    | Call         of System.Reflection.MethodInfo
    | Callvirt     of System.Reflection.MethodInfo
    | DeclareLocal of System.Type
    | Div
    | LdArg_0
    | Ldc_I4       of int
    | Ldc_I4_0
    | Ldc_I4_1
    | Ldc_I4_2
    | Ldc_I4_3
    | Ldc_I4_4
    | Ldc_I4_5
    | Ldc_I4_6
    | Ldc_I4_7
    | Ldc_I4_8
    | Ldloc        of int
    | Ldloc_0
    | Ldloc_1
    | Ldloc_2
    | Ldloc_3
    | Ldloc_S      of byte
    | Ldstr        of string
    | Mul
    | Neg
    | Newobj       of System.Reflection.ConstructorInfo
    | Nop
    | Pop
    | Refanyval
    | Ret
    | Stloc        of int
    | Stloc_0
    | Stloc_1
    | Stloc_2
    | Stloc_3
    | Stloc_S      of byte
    | Sub

type Method = {
    Name: string
    ArgumentType: BindingType option
    Instructions: Instruction list 
    ReturnType:   System.Type
} 
     
let private emit (ilg : Emit.ILGenerator) inst = 
    match inst with 
    | Add            -> ilg.Emit(OpCodes.Add)
    | Call mi        -> ilg.Emit(OpCodes.Call, mi)
    | Callvirt mi    -> ilg.Emit(OpCodes.Callvirt, mi)
    | DeclareLocal t -> ignore(ilg.DeclareLocal(t))
    | Div            -> ilg.Emit(OpCodes.Div)
    | LdArg_0        -> ilg.Emit(OpCodes.Ldarg_0)
    | Ldc_I4 n       -> ilg.Emit(OpCodes.Ldc_I4, n)
    | Ldc_I4_0       -> ilg.Emit(OpCodes.Ldc_I4_0)
    | Ldc_I4_1       -> ilg.Emit(OpCodes.Ldc_I4_1)
    | Ldc_I4_2       -> ilg.Emit(OpCodes.Ldc_I4_2)
    | Ldc_I4_3       -> ilg.Emit(OpCodes.Ldc_I4_3)
    | Ldc_I4_4       -> ilg.Emit(OpCodes.Ldc_I4_4)
    | Ldc_I4_5       -> ilg.Emit(OpCodes.Ldc_I4_5)
    | Ldc_I4_6       -> ilg.Emit(OpCodes.Ldc_I4_6)
    | Ldc_I4_7       -> ilg.Emit(OpCodes.Ldc_I4_7)
    | Ldc_I4_8       -> ilg.Emit(OpCodes.Ldc_I4_8)
    | Ldloc    i     -> ilg.Emit(OpCodes.Ldloc, i)
    | Ldloc_0        -> ilg.Emit(OpCodes.Ldloc_0)
    | Ldloc_1        -> ilg.Emit(OpCodes.Ldloc_1)
    | Ldloc_2        -> ilg.Emit(OpCodes.Ldloc_2)
    | Ldloc_3        -> ilg.Emit(OpCodes.Ldloc_3)
    | Ldloc_S  i     -> ilg.Emit(OpCodes.Ldloc_S, i)
    | Ldstr    s     -> ilg.Emit(OpCodes.Ldstr, s)
    | Mul            -> ilg.Emit(OpCodes.Mul)
    | Neg            -> ilg.Emit(OpCodes.Neg)
    | Newobj   ci    -> ilg.Emit(OpCodes.Newobj, ci)
    | Nop            -> ilg.Emit(OpCodes.Nop)
    | Pop            -> ilg.Emit(OpCodes.Pop)
    | Refanyval      -> ilg.Emit(OpCodes.Refanyval)
    | Ret            -> ilg.Emit(OpCodes.Ret)
    | Stloc    i     -> ilg.Emit(OpCodes.Stloc, i)
    | Stloc_0        -> ilg.Emit(OpCodes.Stloc_0)
    | Stloc_1        -> ilg.Emit(OpCodes.Stloc_1)
    | Stloc_2        -> ilg.Emit(OpCodes.Stloc_2)
    | Stloc_3        -> ilg.Emit(OpCodes.Stloc_3)
    | Stloc_S  i     -> ilg.Emit(OpCodes.Stloc_S, i)
    | Sub            -> ilg.Emit(OpCodes.Sub)

let private compileEntryPoint (moduleContainingMethod : ModuleBuilder) (methodToCall: MethodBuilder) = 
    let tb = 
        let className = "Program"
        let ta = TypeAttributes.NotPublic ||| TypeAttributes.AutoLayout ||| TypeAttributes.AnsiClass ||| TypeAttributes.BeforeFieldInit
        moduleContainingMethod.DefineType(className, ta)
    let mb = 
        let ma = MethodAttributes.Public ||| MethodAttributes.Static 
        let methodName = "Main"
        tb.DefineMethod(methodName, ma)
    let ilg = mb.GetILGenerator() |> emit
    let ci = methodToCall.ReflectedType.GetConstructor([||])
    ilg (Call methodToCall)
    if methodToCall.ReturnType <> null then
        ilg (DeclareLocal methodToCall.ReturnType)
        ilg Stloc_0
        ilg (Ldloc_S 0uy)
        let writeln = typeof<System.Console>.GetMethod("WriteLine", [| typeof<System.Int32> |])
        ilg (Call writeln)
    ilg Ret
    tb.CreateType() |> ignore
    mb

let rec typeOf = function
| BoolType                     -> typeof<bool>
| FunctionType (_, resultType) -> resultType |> typeOf
| IntType                      -> typeof<int>
| StringType                   -> typeof<string> 
| unexpected                   -> failwithf "Unexpected result type %A." unexpected 

let compileMethod (typeBuilder: TypeBuilder) (compiledMethod: Method) =
    let arguments =
        match compiledMethod.ArgumentType with
        | Some argumentType -> [| typeOf argumentType |]
        | None -> [||]
    let methodBuilder = 
        let methodAttributes = MethodAttributes.Public ||| MethodAttributes.Static ||| MethodAttributes.HideBySig
        typeBuilder.DefineMethod(compiledMethod.Name, methodAttributes, compiledMethod.ReturnType, arguments)
    let ilg = methodBuilder.GetILGenerator() |> emit
    Seq.iter ilg compiledMethod.Instructions 
    ilg Ret
    (compiledMethod.Name, methodBuilder)

let rec private findMain (methods : Method list) =
    match methods with
    | [] -> failwith "main method not found"
    | m :: rest when m.Name = "main" -> m
    | _ :: rest -> findMain rest

let private compileModule(moduleName: string, methods: Method list) =
    let moduleNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension moduleName
    let assemblyBuilder = 
        let assemblyName = AssemblyName(moduleNameWithoutExtension)
        AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave)
    let moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleName)
    let typeBuilder = 
        let className = "__CompiledMethods"
        let typeAttributes = TypeAttributes.Public ||| TypeAttributes.Abstract ||| TypeAttributes.Sealed ||| TypeAttributes.AutoLayout ||| TypeAttributes.AnsiClass ||| TypeAttributes.BeforeFieldInit
        moduleBuilder.DefineType(className, typeAttributes)
    let methodBuilders = 
        methods 
        |> List.map (compileMethod typeBuilder)
        |> Map.ofList
    let entryPointType = typeBuilder.CreateType()
    let entryPoint = compileEntryPoint moduleBuilder methodBuilders.["main"]
    assemblyBuilder.SetEntryPoint(entryPoint, PEFileKinds.ConsoleApplication)
    moduleBuilder.CreateGlobalFunctions()
    assemblyBuilder

let toAssemblyBuilder(methods: Method list) =
    let moduleName = "test.exe"
    compileModule(moduleName, methods)
