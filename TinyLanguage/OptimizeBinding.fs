﻿module OptimizeBinding

open BindingTree

let private optimizeInc (binding: Binding) : Binding =
    match binding with 
    | IncBinding (IntBinding number) 
        -> IntBinding (number + 1)
    | IncBinding _
    | BoolBinding _ 
    | IntBinding _  
    | StringBinding _ 
    | VariableBinding _
    | FunctionBinding _
    | InvokeBinding _
    | DefBinding _
    | ErrorBinding _  
    | EmptyBinding _    
        -> binding 


let private optimizeNode (binding: Binding) : Binding =
    binding |> optimizeInc // could chain other optimizers here


// now recursively optimize the entire tree
let rec optimize (binding: Binding) : Binding =
    let bindingWithOptimizedLeaves = 
        match binding with
        | FunctionBinding ( UserFunction ( Argument = argument; Body = body; ResultType = resultType ) )
            -> FunctionBinding ( UserFunction (argument, body |> optimize, resultType ) )
        | IncBinding argument 
            -> IncBinding (argument |> optimize)
        | InvokeBinding { FunctionName = name; Function = func; Argument = argument }
            -> 
                let optimizedFunction =
                    match func with 
                    | UserFunction  ( Argument = argument; Body = body; ResultType = resultType )
                        -> ( UserFunction (argument, body |> optimize, resultType ) )
                    | Inc 
                        -> Inc
                let optimizedArgument = 
                    match argument with
                    | None 
                        -> argument
                    | Some argumentBinding
                        -> Some (argumentBinding |> optimize)
                InvokeBinding { FunctionName = name; Function = optimizedFunction; Argument = optimizedArgument }
        | _ 
            -> binding
    bindingWithOptimizedLeaves |> optimizeNode