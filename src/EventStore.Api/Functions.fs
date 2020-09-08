namespace EventStore.Api

open System
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open EventStore.DataAccess
open EventStore.PrivateTypes
open EventStore.Domain
open EventStore.Extensions
open Newtonsoft.Json
open FsToolkit.ErrorHandling
open System.IO
open System.Web.Http

[<RequireQualifiedAccess>]
module Functions =

    let dbConnectionString = 
        Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
        |> DbConnectionString

    let toActionResult (logger : ILogger) asyncResult = async {
        let! result = asyncResult

        let actionResult =
            match result with
            | Ok data -> OkObjectResult(data) :> IActionResult
            | Error domainError ->
                match domainError with
                | DomainError.ValidationError errorMessage -> BadRequestObjectResult(errorMessage) :> IActionResult
                | DomainError.StreamNotFound -> NotFoundObjectResult("Stream not found") :> IActionResult
                | DomainError.InvalidVersion -> BadRequestObjectResult("Invalid stream version") :> IActionResult
                | DomainError.DatabaseError ex -> 
                    logger.LogError(ex, ex.Message)
                    InternalServerErrorResult() :> IActionResult

        return actionResult
    }

    [<FunctionName("GetAllStreams")>]
    let GetAllStreams 
        ([<HttpTrigger(AuthorizationLevel.Function, "get", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =

        dbConnectionString
        |> Repository.getAllStreams 
        |> Service.getAllStreams
        |> AsyncResult.map (List.map StreamDto.fromModel)
        |> (toActionResult logger)
        |> Async.StartAsTask
        
    [<FunctionName("GetStream")>]
    let GetStream 
        ([<HttpTrigger(AuthorizationLevel.Function, "get", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =

        let getStream (query : StreamQuery) =
            let getStream = Repository.getStream dbConnectionString
            Service.getStream getStream query
            
        let query = 
            request.TryGetQueryStringValue "streamName" 
            |> Option.defaultValue String.Empty
            |> Query.toStreamQueryModel
            |> Result.mapError DomainError.ValidationError
            |> Async.singleton

        query
        |> AsyncResult.bind getStream
        |> AsyncResult.map StreamDto.fromModel
        |> (toActionResult logger)
        |> Async.StartAsTask 
        
    [<FunctionName("GetSnapshots")>]
    let GetSnapshots 
        ([<HttpTrigger(AuthorizationLevel.Function, "get", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =

        let getSnapshots (query : SnapshotsQuery) =
            let getSnapshots = Repository.getSnapshots dbConnectionString
            Service.getSnapshots getSnapshots query  

        let query =
            request.TryGetQueryStringValue "streamName" 
            |> Option.defaultValue String.Empty
            |> Query.toSnapshotsQueryModel
            |> Result.mapError DomainError.ValidationError
            |> Async.singleton
        
        query
        |> AsyncResult.bind getSnapshots
        |> AsyncResult.map (List.map SnapshotDto.fromModel)
        |> (toActionResult logger)
        |> Async.StartAsTask     
        
    [<FunctionName("GetEvents")>]
    let GetEvents 
        ([<HttpTrigger(AuthorizationLevel.Function, "get", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =

        let getEvents (query : EventsQuery) =
            let getEvents = Repository.getEvents dbConnectionString
            Service.getEvents getEvents query

        let streamName =
            request.TryGetQueryStringValue "streamName" 
            |> Option.defaultValue String.Empty

        let startAtVersion =
            request.TryGetQueryStringValue "startAtVersion" 
            |> Option.map int32 
            |> Option.defaultValue 0

        let query =
            Query.toEventsQueryModel streamName startAtVersion
            |> Result.mapError DomainError.ValidationError
            |> Async.singleton

        query
        |> AsyncResult.bind getEvents
        |> AsyncResult.map (List.map EventDto.fromModel)
        |> (toActionResult logger)
        |> Async.StartAsTask              

    [<FunctionName("DeleteSnapshots")>]
    let DeleteSnapshots 
        ([<HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =

        let deleteSnapshots (query : SnapshotsQuery) =
            let deleteSnapshots = Repository.deleteSnapshots dbConnectionString
            Service.deleteSnapshots deleteSnapshots query 

        let query =
            request.TryGetQueryStringValue "streamName" 
            |> Option.defaultValue String.Empty
            |> Query.toSnapshotsQueryModel
            |> Result.mapError DomainError.ValidationError
            |> Async.singleton

        query
        |> AsyncResult.bind deleteSnapshots
        |> (toActionResult logger)
        |> Async.StartAsTask   
        
    [<FunctionName("CreateSnapshot")>]
    let CreateSnapshot 
        ([<HttpTrigger(AuthorizationLevel.Function, "post", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =
        
        let createSnapshot (model : CreateSnapshot) =
            let getStream = Repository.getStream dbConnectionString
            let createSnapshot = Repository.createSnapshot dbConnectionString
            Service.createSnapshot getStream createSnapshot model  

        use reader = new StreamReader(request.Body)

        reader.ReadToEndAsync() 
        |> Async.AwaitTask
        |> Async.map (JsonConvert.DeserializeObject<CreateSnapshotDto> >> CreateSnapshotDto.toModel >> Result.mapError DomainError.ValidationError)
        |> AsyncResult.bind createSnapshot
        |> (toActionResult logger)
        |> Async.StartAsTask 

    [<FunctionName("AppendEvents")>]
    let AppendEvents 
        ([<HttpTrigger(AuthorizationLevel.Function, "post", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =
        
        let appendEvents (model : AppendEvents) =
            let getStream = Repository.getStream dbConnectionString
            let appendEvents = Repository.appendEvents dbConnectionString
            Service.appendEvents getStream appendEvents model  

        use reader = new StreamReader(request.Body)

        reader.ReadToEndAsync() 
        |> Async.AwaitTask
        |> Async.map (JsonConvert.DeserializeObject<AppendEventsDto> >> AppendEventsDto.toModel >> Result.mapError DomainError.ValidationError)
        |> AsyncResult.bind appendEvents
        |> (toActionResult logger)
        |> Async.StartAsTask 