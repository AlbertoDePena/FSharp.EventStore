namespace EventStore.Extensions

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http

[<RequireQualifiedAccess>]
module Async =
    
    /// <summary>
    /// Async.StartAsTask and up-cast from Task<unit> to plain Task.
    /// </summary>
    /// <param name="computation">The asynchronous computation.</param>
    let AsTask (computation : Async<unit>) = Async.StartAsTask computation :> Task
   
[<AutoOpen>]
module HttpRequestExtensions =

    type HttpRequest with

        /// Try to get the Bearer token from the Authorization header
        member this.TryGetBearerToken () =
            this.Headers 
            |> Seq.tryFind (fun q -> q.Key = "Authorization")
            |> Option.map (fun q -> if Seq.isEmpty q.Value then String.Empty else q.Value |> Seq.head)
            |> Option.map (fun h -> h.Substring("Bearer ".Length).Trim())

        member this.TryGetQueryStringValue (name : string) =
            let hasValue, values = this.Query.TryGetValue(name)
            if hasValue
            then values |> Seq.tryHead
            else None

        member this.TryGetHeaderValue (name : string) =
            let hasHeader, values = this.Headers.TryGetValue(name)
            if hasHeader
            then values |> Seq.tryHead
            else None