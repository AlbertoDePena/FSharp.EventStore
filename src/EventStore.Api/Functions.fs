namespace EventStore.Api

open System
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open EventStore.DataAccess
open EventStore.Domain
open EventStore.Extensions
open Newtonsoft.Json
open FsToolkit.ErrorHandling

[<AutoOpen>]
module Core =

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

[<RequireQualifiedAccess>]
module CompositionRoot =

    let dbConnectionString = 
        Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
        |> DbConnectionString

    let getAllStreams () =
        dbConnectionString
        |> Repository.getAllStreams
        |> Service.getAllStreams

    let getStream (query : UnvalidatedStreamQuery) =
        Service.getStream (Repository.getStream dbConnectionString) query

    let getEvents (query : UnvalidatedEventsQuery) =
        Service.getEvents (Repository.getEvents dbConnectionString) query  
        
    let getSnapshots (query : UnvalidatedSnapshotsQuery) =
        Service.getSnapshots (Repository.getSnapshots dbConnectionString) query  
        
    let deleteSnapshots (query : UnvalidatedSnapshotsQuery) =
        Service.deleteSnapshots (Repository.deleteSnapshots dbConnectionString) query  

[<RequireQualifiedAccess>]
module Functions =

    let toActionResult (logger : ILogger) asyncResult = async {
        let! result = asyncResult

        let actionResult =
            match result with
            | Ok data -> OkObjectResult(data) :> IActionResult
            | Error domainError ->
                match domainError with
                | DomainError.ValidationError errorMessage -> BadRequestObjectResult(errorMessage) :> IActionResult
                | DomainError.StreamNotFound streamName -> NotFoundObjectResult(streamName) :> IActionResult
                | DomainError.InvalidVersion -> BadRequestObjectResult("Invalid stream version") :> IActionResult
                | DomainError.DatabaseError ex -> 
                    logger.LogError(ex, ex.Message)
                    BadRequestObjectResult("A database error has occurred") :> IActionResult

        return actionResult
    }

    [<FunctionName("GetAllStreams")>]
    let GetAllStreams 
        ([<HttpTrigger(AuthorizationLevel.Function, "get", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =
        CompositionRoot.getAllStreams ()
        |> AsyncResult.map JsonConvert.SerializeObject
        |> (toActionResult logger)
        |> Async.StartAsTask
        
    [<FunctionName("GetStream")>]
    let GetStream 
        ([<HttpTrigger(AuthorizationLevel.Function, "get", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =
        let streamName = request.TryGetQueryStringValue "streamName" |> Option.defaultValue String.Empty
        let query : UnvalidatedStreamQuery = { StreamName = streamName } 

        CompositionRoot.getStream query
        |> (toActionResult logger)
        |> Async.StartAsTask 
        
    [<FunctionName("GetSnapshots")>]
    let GetSnapshots 
        ([<HttpTrigger(AuthorizationLevel.Function, "get", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =
        let streamName = request.TryGetQueryStringValue "streamName" |> Option.defaultValue String.Empty
        let query : UnvalidatedSnapshotsQuery = { StreamName = streamName } 

        CompositionRoot.getSnapshots query
        |> (toActionResult logger)
        |> Async.StartAsTask     
        
    [<FunctionName("GetEvents")>]
    let GetEvents 
        ([<HttpTrigger(AuthorizationLevel.Function, "get", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =
        let streamName = request.TryGetQueryStringValue "streamName" |> Option.defaultValue String.Empty
        let startAtVersion = request.TryGetQueryStringValue "startAtVersion" |> Option.map int32 |> Option.defaultValue 0
        let query : UnvalidatedEventsQuery = { StreamName = streamName; StartAtVersion = startAtVersion } 

        CompositionRoot.getEvents query
        |> (toActionResult logger)
        |> Async.StartAsTask              

    [<FunctionName("DeleteSnapshots")>]
    let DeleteSnapshots 
        ([<HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =
        let streamName = request.TryGetQueryStringValue "streamName" |> Option.defaultValue String.Empty
        let query : UnvalidatedSnapshotsQuery = { StreamName = streamName } 

        CompositionRoot.deleteSnapshots query
        |> (toActionResult logger)
        |> Async.StartAsTask        