namespace EventStore.Api

open System
open EventStore.DataAccess
open EventStore.Domain

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

    let toModel (dto : NewEventDto) : UnvalidatedNewEvent = {
        Type = dto.Type
        Data = dto.Data }

[<CLIMutable>]    
type AppendEventsDto = {
    ExpectedVersion : int32
    StreamName : string
    Events : NewEventDto array }

[<RequireQualifiedAccess>]    
module AppendEventsDto =

    let toModel (dto : AppendEventsDto) : UnvalidatedAppendEvents = { 
        Events = dto.Events |> Array.map NewEventDto.toModel |> Array.toList
        ExpectedVersion = dto.ExpectedVersion
        StreamName = dto.StreamName }

[<CLIMutable>]
type CreateSnapshotDto = {
    StreamName : string
    Description : string
    Data : string }

[<RequireQualifiedAccess>]    
module CreateSnapshotDto =

    let toModel (dto : CreateSnapshotDto) : UnvalidatedCreateSnapshot = { 
        Data = dto.Data
        Description = dto.Description
        StreamName = dto.StreamName }

[<RequireQualifiedAccess>]
module StreamQueryDto =
    
    let toModel (streamName : string) : UnvalidatedStreamQuery = { StreamName = streamName }

[<RequireQualifiedAccess>]
module SnapshotsQueryDto =
    
    let toModel (streamName : string) : UnvalidatedSnapshotsQuery = { StreamName = streamName }

[<RequireQualifiedAccess>]
module EventsQueryDto =
    
    let toModel (streamName : string) (startAtVersion : int32) : UnvalidatedEventsQuery = {
        StreamName = streamName
        StartAtVersion = startAtVersion
    }