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
open System.IO

[<CLIMutable>]
type StreamDto = {
    StreamId : string
    Version : int32
    Name : string
    CreatedAt : DateTimeOffset
    UpdatedAt : DateTimeOffset Nullable }

[<CLIMutable>]    
type EventDto = {
    EventId : string
    StreamId : string
    Version : int32
    Data : string
    Type : string    
    CreatedAt : DateTimeOffset }

[<CLIMutable>]
type SnapshotDto = {
    SnapshotId : string
    StreamId : string
    Version : int32
    Data : string
    Description : string    
    CreatedAt : DateTimeOffset }

[<CLIMutable>]
type NewEventDto = {
    Data : string
    Type : string }

[<CLIMutable>]    
type AppendEventsDto = {
    ExpectedVersion : int32
    StreamName : string
    Events : NewEventDto array }

[<CLIMutable>]
type CreateSnapshotDto = {
    StreamName : string
    Description : string
    Data : string }

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
                    BadRequestObjectResult("A database error has occurred") :> IActionResult

        return actionResult
    }

    [<FunctionName("GetAllStreams")>]
    let GetAllStreams 
        ([<HttpTrigger(AuthorizationLevel.Function, "get", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =

        dbConnectionString
        |> Repository.getAllStreams 
        |> Service.getAllStreams
        |> (toActionResult logger)
        |> Async.StartAsTask
        
    [<FunctionName("GetStream")>]
    let GetStream 
        ([<HttpTrigger(AuthorizationLevel.Function, "get", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =

        let getStream (query : UnvalidatedStreamQuery) =
            let getStream = Repository.getStream dbConnectionString
            Service.getStream getStream query
            
        let query : UnvalidatedStreamQuery = { 
            StreamName = 
                request.TryGetQueryStringValue "streamName" 
                |> Option.defaultValue String.Empty } 

        getStream query
        |> (toActionResult logger)
        |> Async.StartAsTask 
        
    [<FunctionName("GetSnapshots")>]
    let GetSnapshots 
        ([<HttpTrigger(AuthorizationLevel.Function, "get", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =

        let getSnapshots (query : UnvalidatedSnapshotsQuery) =
            let getSnapshots = Repository.getSnapshots dbConnectionString
            Service.getSnapshots getSnapshots query  

        let query : UnvalidatedSnapshotsQuery = { 
            StreamName = 
                request.TryGetQueryStringValue "streamName" 
                |> Option.defaultValue String.Empty } 

        getSnapshots query
        |> (toActionResult logger)
        |> Async.StartAsTask     
        
    [<FunctionName("GetEvents")>]
    let GetEvents 
        ([<HttpTrigger(AuthorizationLevel.Function, "get", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =

        let getEvents (query : UnvalidatedEventsQuery) =
            let getEvents = Repository.getEvents dbConnectionString
            Service.getEvents getEvents query

        let query : UnvalidatedEventsQuery = { 
            StreamName = 
                request.TryGetQueryStringValue "streamName" 
                |> Option.defaultValue String.Empty
            StartAtVersion = 
                request.TryGetQueryStringValue "startAtVersion" 
                |> Option.map int32 
                |> Option.defaultValue 0 } 

        getEvents query
        |> (toActionResult logger)
        |> Async.StartAsTask              

    [<FunctionName("DeleteSnapshots")>]
    let DeleteSnapshots 
        ([<HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =

        let deleteSnapshots (query : UnvalidatedSnapshotsQuery) =
            let deleteSnapshots = Repository.deleteSnapshots dbConnectionString
            Service.deleteSnapshots deleteSnapshots query 

        let query : UnvalidatedSnapshotsQuery = { 
            StreamName = 
                request.TryGetQueryStringValue "streamName" 
                |> Option.defaultValue String.Empty } 

        deleteSnapshots query
        |> (toActionResult logger)
        |> Async.StartAsTask   
        
    [<FunctionName("CreateSnapshot")>]
    let CreateSnapshot 
        ([<HttpTrigger(AuthorizationLevel.Function, "post", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =
        
        let createSnapshot (model : UnvalidatedCreateSnapshot) =
            let getStream = Repository.getStream dbConnectionString
            let createSnapshot = Repository.createSnapshot dbConnectionString
            Service.createSnapshot getStream createSnapshot model  

        let toModel (dto : CreateSnapshotDto) : UnvalidatedCreateSnapshot = { 
            Data = dto.Data
            Description = dto.Description
            StreamName = dto.StreamName }

        use reader = new StreamReader(request.Body)

        reader.ReadToEndAsync() 
        |> Async.AwaitTask
        |> Async.map (JsonConvert.DeserializeObject<CreateSnapshotDto> >> toModel)
        |> Async.bind createSnapshot
        |> (toActionResult logger)
        |> Async.StartAsTask 

    [<FunctionName("AppendEvents")>]
    let AppendEvents 
        ([<HttpTrigger(AuthorizationLevel.Function, "post", Route = null)>] request: HttpRequest) 
        (logger: ILogger) =
        
        let appendEvents (model : UnvalidatedAppendEvents) =
            let getStream = Repository.getStream dbConnectionString
            let appendEvents = Repository.appendEvents dbConnectionString
            Service.appendEvents getStream appendEvents model  

        let toEventModel (dto : NewEventDto) : UnvalidatedNewEvent = {
            Type = dto.Type
            Data = dto.Data }

        let toModel (dto : AppendEventsDto) : UnvalidatedAppendEvents = { 
            Events = dto.Events |> Array.map toEventModel |> Array.toList
            ExpectedVersion = dto.ExpectedVersion
            StreamName = dto.StreamName }

        use reader = new StreamReader(request.Body)

        reader.ReadToEndAsync() 
        |> Async.AwaitTask
        |> Async.map (JsonConvert.DeserializeObject<AppendEventsDto> >> toModel)
        |> Async.bind appendEvents
        |> (toActionResult logger)
        |> Async.StartAsTask 