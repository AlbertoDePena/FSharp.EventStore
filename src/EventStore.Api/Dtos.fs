namespace EventStore.Api

open System

[<CLIMutable>]
type StreamDto = {
    StreamId : string
    Version : int32
    Name : string
    CreatedAt : DateTimeOffset
    UpdatedAt : DateTimeOffset Nullable }

[<RequireQualifiedAccess>]
module StreamDto =
    
    let fromEntity (stream : EventStore.DataAccess.Stream) : StreamDto = {
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

    let fromEntity (event : EventStore.DataAccess.Event) : EventDto = {
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

    let fromEntity (snapshot : EventStore.DataAccess.Snapshot) : SnapshotDto = {
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

    let toUnvalidated (dto : NewEventDto) : EventStore.PublicTypes.UnvalidatedNewEvent = {
        Type = dto.Type
        Data = dto.Data
    }

[<CLIMutable>]    
type AppendEventsDto = {
    ExpectedVersion : int32
    StreamName : string
    Events : NewEventDto array }

[<RequireQualifiedAccess>]    
module AppendEventsDto =

    let toUnvalidated (dto : AppendEventsDto) : EventStore.PublicTypes.UnvalidatedAppendEvents = {
        StreamName = dto.StreamName
        ExpectedVersion = dto.ExpectedVersion
        Events = 
            dto.Events 
            |> Array.map NewEventDto.toUnvalidated 
            |> Array.toList
    }

[<CLIMutable>]
type CreateSnapshotDto = {
    StreamName : string
    Description : string
    Data : string }

[<RequireQualifiedAccess>]    
module CreateSnapshotDto =

    let toUnvalidated (dto : CreateSnapshotDto) : EventStore.PublicTypes.UnvalidatedCreateSnapshot = {
        StreamName = dto.StreamName
        Data = dto.Data
        Description = dto.Description
    }

[<RequireQualifiedAccess>]
module Query =
    
    let toUnvalidatedStreamQuery (streamName : string) : EventStore.PublicTypes.UnvalidatedStreamQuery = 
        { StreamName = streamName }

    let toUnvalidatedSnapshotsQuery (streamName : string) : EventStore.PublicTypes.UnvalidatedSnapshotsQuery = 
        { StreamName = streamName }
    
    let toUnvalidatedEventsQuery (streamName : string) (startAtVersion : int32) : EventStore.PublicTypes.UnvalidatedEventsQuery = 
        { StreamName = streamName; StartAtVersion = startAtVersion }