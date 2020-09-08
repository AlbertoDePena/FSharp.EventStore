namespace EventStore.Api

open System
open EventStore.DataAccess
open EventStore.PrivateTypes
open FsToolkit.ErrorHandling

[<CLIMutable>]
type StreamDto = {
    StreamId : string
    Version : int32
    Name : string
    CreatedAt : DateTimeOffset
    UpdatedAt : DateTimeOffset Nullable }

[<RequireQualifiedAccess>]
module StreamDto =
    
    let fromModel (stream : Stream) : StreamDto = {
        StreamId = stream.StreamId
        Name = stream.Name
        Version = stream.Version
        CreatedAt = stream.CreatedAt
        UpdatedAt = stream.UpdatedAt
    }

[<CLIMutable>]    
type EventDto = {
    EventId : string
    StreamId : string
    Version : int32
    Data : string
    Type : string    
    CreatedAt : DateTimeOffset }

[<RequireQualifiedAccess>]
module EventDto =

    let fromModel (event : Event) : EventDto = {
        EventId = event.EventId
        StreamId = event.StreamId
        Version = event.Version
        Type = event.Type
        Data = event.Data
        CreatedAt = event.CreatedAt
    }

[<CLIMutable>]
type SnapshotDto = {
    SnapshotId : string
    StreamId : string
    Version : int32
    Data : string
    Description : string    
    CreatedAt : DateTimeOffset }

[<RequireQualifiedAccess>]
module SnapshotDto =

    let fromModel (snapshot : Snapshot) : SnapshotDto = {
        SnapshotId = snapshot.SnapshotId
        StreamId = snapshot.StreamId
        Version = snapshot.Version
        Description = snapshot.Description
        Data = snapshot.Data
        CreatedAt = snapshot.CreatedAt
    }

[<CLIMutable>]
type NewEventDto = {
    Data : string
    Type : string }

[<RequireQualifiedAccess>]
module NewEventDto =

    let toModel (dto : NewEventDto) : Result<NewEvent, string> = result {
        let! eventType =
            String256.tryCreate dto.Type
            |> Result.requireSome "Event type is required and it must be at most 256 characters"
        
        let! data = 
            StringMax.tryCreate dto.Data
            |> Result.requireSome "Event data is required"

        return { Type = eventType; Data = data }
    }

[<CLIMutable>]    
type AppendEventsDto = {
    ExpectedVersion : int32
    StreamName : string
    Events : NewEventDto array }

[<RequireQualifiedAccess>]    
module AppendEventsDto =

    let toModel (dto : AppendEventsDto) : Result<AppendEvents, string> = result {        
        let! streamName = 
            String256.tryCreate dto.StreamName
            |> Result.requireSome "Stream name is required and it must be at most 256 characters"
        
        let! version =
            NonNegativeInt.tryCreate dto.ExpectedVersion
            |> Result.requireSome "Invalid stream version"

        do! dto.Events |> Result.requireNotEmpty "Cannot append to stream without events" 

        let! events = 
            dto.Events 
            |> Array.toList
            |> List.traverseResultM NewEventDto.toModel
            
        return { StreamName = streamName; Events = events; ExpectedVersion = version }
    }

[<CLIMutable>]
type CreateSnapshotDto = {
    StreamName : string
    Description : string
    Data : string }

[<RequireQualifiedAccess>]    
module CreateSnapshotDto =

    let toModel (dto : CreateSnapshotDto) : Result<CreateSnapshot, string> = result {
        let! streamName = 
            String256.tryCreate dto.StreamName
            |> Result.requireSome "Stream name is required and it must be at most 256 characters"

        let! description =
            String256.tryCreate dto.Description
            |> Result.requireSome "Snapshot description is required and it must be at most 256 characters"
        
        let! data = 
            StringMax.tryCreate dto.Data
            |> Result.requireSome "Snapshot data is required"

        return { StreamName = streamName; Description = description; Data = data }
    }

[<RequireQualifiedAccess>]
module Query =
    
    let toStreamQueryModel (streamName : string) : Result<StreamQuery, string> = result {
        let! streamName = 
            String256.tryCreate streamName
            |> Result.requireSome "Stream name is required and it must be at most 256 characters"

        return { StreamName = streamName }
    }

    let toSnapshotsQueryModel (streamName : string) : Result<SnapshotsQuery, string> = result {
        let! streamName = 
            String256.tryCreate streamName
            |> Result.requireSome "Stream name is required and it must be at most 256 characters"

        return { StreamName = streamName }
    }

    let toEventsQueryModel (streamName : string) (startAtVersion : int32) : Result<EventsQuery, string> = result {
        let! streamName = 
            String256.tryCreate streamName
            |> Result.requireSome "Stream name is required and it must be at most 256 characters"

        let! version =
            NonNegativeInt.tryCreate startAtVersion
            |> Result.requireSome "Invalid stream version"

        return { StreamName = streamName; StartAtVersion = version }
    }