namespace EventStore.Extensions

open System.Threading.Tasks

[<RequireQualifiedAccess>]
module Async =
    
    /// <summary>
    /// Async.StartAsTask and up-cast from Task<unit> to plain Task.
    /// </summary>
    /// <param name="task">The asynchronous computation.</param>
    let AsTask (task : Async<unit>) = Async.StartAsTask task :> Task
   