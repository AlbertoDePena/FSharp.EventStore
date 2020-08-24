namespace EventStore.Extensions

open System.Threading.Tasks

[<RequireQualifiedAccess>]
module Async =
    
    /// <summary>
    /// Async.StartAsTask and up-cast from Task<unit> to plain Task.
    /// </summary>
    /// <param name="computation">The asynchronous computation.</param>
    let AsTask (computation : Async<unit>) = Async.StartAsTask computation :> Task
   