﻿namespace EventStore.Api

open System
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open EventStore.DataAccessTypes
open EventStore.DataAccess
open EventStore.DomainTypes
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

        let getAll = Database.getAllStreams dbConnectionString
        let streams = Service.getAllStreams getAll 

        let asyncResult = 
            streams 
            |> AsyncResult.map (List.map StreamDto.fromEntity)

        toActionResult logger asyncResult |> Async.StartAsTask

    [<FunctionName("GetStream")>]
    let GetStream 
        ([<HttpTrigger(AuthorizationLevel.Function, "get", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =

        let getStream = Database.getStream dbConnectionString
        let stream = Service.getStream getStream
            
        let query = 
            request.TryGetQueryStringValue "streamName" 
            |> Option.defaultValue String.Empty
            |> Query.toUnvalidatedStreamQuery

        let asyncResult = stream query |> AsyncResult.map StreamDto.fromEntity

        toActionResult logger asyncResult |> Async.StartAsTask 
        
    [<FunctionName("GetSnapshots")>]
    let GetSnapshots 
        ([<HttpTrigger(AuthorizationLevel.Function, "get", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =

        let getSnapshots = Database.getSnapshots dbConnectionString
        let snapshots = Service.getSnapshots getSnapshots  

        let query =
            request.TryGetQueryStringValue "streamName" 
            |> Option.defaultValue String.Empty
            |> Query.toUnvalidatedSnapshotsQuery
        
        let asyncResult = snapshots query |> AsyncResult.map (List.map SnapshotDto.fromEntity)
        
        toActionResult logger asyncResult |> Async.StartAsTask     
        
    [<FunctionName("GetEvents")>]
    let GetEvents 
        ([<HttpTrigger(AuthorizationLevel.Function, "get", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =

        let getEvents = Database.getEvents dbConnectionString
        let events = Service.getEvents getEvents

        let streamName =
            request.TryGetQueryStringValue "streamName" 
            |> Option.defaultValue String.Empty

        let startAtVersion =
            request.TryGetQueryStringValue "startAtVersion" 
            |> Option.map int32 
            |> Option.defaultValue 0

        let query = Query.toUnvalidatedEventsQuery streamName startAtVersion
        
        let asyncResult = events query |> AsyncResult.map (List.map EventDto.fromEntity)
        
        toActionResult logger asyncResult |> Async.StartAsTask              

    [<FunctionName("DeleteSnapshots")>]
    let DeleteSnapshots 
        ([<HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =

        let deleteSnapshots = Database.deleteSnapshots dbConnectionString
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
        
        let getStream = Database.getStream dbConnectionString
        let createSnapshot = Database.createSnapshot dbConnectionString
        let create = Service.createSnapshot getStream createSnapshot  
        let toUnvalidated = JsonConvert.DeserializeObject<CreateSnapshotDto> >> CreateSnapshotDto.toUnvalidated

        use reader = new StreamReader(request.Body)
        
        let asyncResult = 
            reader.ReadToEndAsync() 
            |> Async.AwaitTask 
            |> Async.map toUnvalidated
            |> Async.bind create

        toActionResult logger asyncResult |> Async.StartAsTask 

    [<FunctionName("AppendEvents")>]
    let AppendEvents 
        ([<HttpTrigger(AuthorizationLevel.Function, "post", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =
        
        let getStream = Database.getStream dbConnectionString
        let appendEvents = Database.appendEvents dbConnectionString
        let append = Service.appendEvents getStream appendEvents  
        let toUnvalidated = JsonConvert.DeserializeObject<AppendEventsDto> >> AppendEventsDto.toUnvalidated

        use reader = new StreamReader(request.Body)

        let asyncResult = 
            reader.ReadToEndAsync() 
            |> Async.AwaitTask 
            |> Async.map toUnvalidated
            |> Async.bind append

        toActionResult logger asyncResult |> Async.StartAsTask