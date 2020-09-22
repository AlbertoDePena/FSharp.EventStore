namespace EventStore.Api

open System
open EventStore.PublicTypes

[<CLIMutable>]
type StreamDto = {
    StreamId : string
    Version : int32
    Name : string
    CreatedAt : DateTimeOffset
    UpdatedAt : DateTimeOffset Nullable }

[<RequireQualifiedAccess>]
module StreamDto =
    
    let fromDomain (stream : Stream) : StreamDto = {
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

    let fromDomain (event : Event) : EventDto = {
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

    let fromDomain (snapshot : Snapshot) : SnapshotDto = {
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

    let toUnvalidated (dto : NewEventDto) : UnvalidatedNewEvent = {
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

    let toUnvalidated (dto : AppendEventsDto) : UnvalidatedAppendEvents = {
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

    let toUnvalidated (dto : CreateSnapshotDto) : UnvalidatedCreateSnapshot = {
        StreamName = dto.StreamName
        Data = dto.Data
        Description = dto.Description
    }

[<RequireQualifiedAccess>]
module Query =
    
    let toUnvalidatedStreamQuery (streamName : string) : UnvalidatedStreamQuery = 
        { StreamName = streamName }

    let toUnvalidatedSnapshotsQuery (streamName : string) : UnvalidatedSnapshotsQuery = 
        { StreamName = streamName }
    
    let toUnvalidatedEventsQuery (streamName : string) (startAtVersion : int32) : UnvalidatedEventsQuery = 
        { StreamName = streamName; StartAtVersion = startAtVersion }