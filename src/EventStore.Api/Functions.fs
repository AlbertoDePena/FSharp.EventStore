namespace EventStore.Api

open System
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open EventStore.DataAccess
open EventStore.PrivateTypes
open EventStore.PublicTypes
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

        let getAll = Repository.getAllStreams dbConnectionString
        let streams = Service.getAllStreams getAll 

        let asyncResult = 
            streams 
            |> AsyncResult.map (List.map StreamDto.fromModel)

        toActionResult logger asyncResult |> Async.StartAsTask

    [<FunctionName("GetStream")>]
    let GetStream 
        ([<HttpTrigger(AuthorizationLevel.Function, "get", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =

        let getStream = Repository.getStream dbConnectionString
        let stream = Service.getStream getStream
            
        let query = 
            request.TryGetQueryStringValue "streamName" 
            |> Option.defaultValue String.Empty
            |> Query.toUnvalidatedStreamQuery

        let asyncResult = stream query |> AsyncResult.map StreamDto.fromModel

        toActionResult logger asyncResult |> Async.StartAsTask 
        
    [<FunctionName("GetSnapshots")>]
    let GetSnapshots 
        ([<HttpTrigger(AuthorizationLevel.Function, "get", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =

        let getSnapshots = Repository.getSnapshots dbConnectionString
        let snapshots = Service.getSnapshots getSnapshots  

        let query =
            request.TryGetQueryStringValue "streamName" 
            |> Option.defaultValue String.Empty
            |> Query.toUnvalidatedSnapshotsQuery
        
        let asyncResult = snapshots query |> AsyncResult.map (List.map SnapshotDto.fromModel)
        
        toActionResult logger asyncResult |> Async.StartAsTask     
        
    [<FunctionName("GetEvents")>]
    let GetEvents 
        ([<HttpTrigger(AuthorizationLevel.Function, "get", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =

        let getEvents = Repository.getEvents dbConnectionString
        let events = Service.getEvents getEvents

        let streamName =
            request.TryGetQueryStringValue "streamName" 
            |> Option.defaultValue String.Empty

        let startAtVersion =
            request.TryGetQueryStringValue "startAtVersion" 
            |> Option.map int32 
            |> Option.defaultValue 0

        let query = Query.toUnvalidatedEventsQuery streamName startAtVersion
        
        let asyncResult = events query |> AsyncResult.map (List.map EventDto.fromModel)
        
        toActionResult logger asyncResult |> Async.StartAsTask              

    [<FunctionName("DeleteSnapshots")>]
    let DeleteSnapshots 
        ([<HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =

        let deleteSnapshots = Repository.deleteSnapshots dbConnectionString
        let delete = Service.deleteSnapshots deleteSnapshots 

        let query =
            request.TryGetQueryStringValue "streamName" 
            |> Option.defaultValue String.Empty
            |> Query.toUnvalidatedSnapshotsQuery

        let asyncResult = delete query

        toActionResult logger asyncResult |> Async.StartAsTask   
        
    [<FunctionName("CreateSnapshot")>]
    let CreateSnapshot 
        ([<HttpTrigger(AuthorizationLevel.Function, "post", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =
        
        let getStream = Repository.getStream dbConnectionString
        let createSnapshot = Repository.createSnapshot dbConnectionString
        let create = Service.createSnapshot getStream createSnapshot  
        let toModel = JsonConvert.DeserializeObject<CreateSnapshotDto> >> CreateSnapshotDto.toUnvalidated

        use reader = new StreamReader(request.Body)
        
        let asyncResult = 
            reader.ReadToEndAsync() 
            |> Async.AwaitTask 
            |> Async.map toModel
            |> Async.bind create

        toActionResult logger asyncResult |> Async.StartAsTask 

    [<FunctionName("AppendEvents")>]
    let AppendEvents 
        ([<HttpTrigger(AuthorizationLevel.Function, "post", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =
        
        let getStream = Repository.getStream dbConnectionString
        let appendEvents = Repository.appendEvents dbConnectionString
        let append = Service.appendEvents getStream appendEvents  
        let toModel = JsonConvert.DeserializeObject<AppendEventsDto> >> AppendEventsDto.toUnvalidated

        use reader = new StreamReader(request.Body)

        let asyncResult = 
            reader.ReadToEndAsync() 
            |> Async.AwaitTask 
            |> Async.map toModel
            |> Async.bind append

        toActionResult logger asyncResult |> Async.StartAsTask