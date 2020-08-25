namespace EventStore.Api

open System
open EventStore.Domain
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
    
    let fromModel (stream : EventStore.Domain.Stream) : StreamDto = {
        StreamId = String50.value stream.StreamId
        Name = String256.value stream.Name
        Version = NonNegativeInt.value stream.Version
        CreatedAt = Timestamp.value stream.CreatedAt
        UpdatedAt = stream.UpdatedAt |> Option.map Timestamp.value |> Option.toNullable
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

    let fromModel (event : EventStore.Domain.Event) : EventDto = {
        EventId = String50.value event.EventId
        StreamId = String50.value event.StreamId
        Version = NonNegativeInt.value event.Version
        Type = String256.value event.Type
        Data = StringMax.value event.Data
        CreatedAt = Timestamp.value event.CreatedAt
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

    let fromModel (snapshot : EventStore.Domain.Snapshot) : SnapshotDto = {
        SnapshotId = String50.value snapshot.SnapshotId
        StreamId = String50.value snapshot.StreamId
        Version = NonNegativeInt.value snapshot.Version
        Description = String256.value snapshot.Description
        Data = StringMax.value snapshot.Data
        CreatedAt = Timestamp.value snapshot.CreatedAt
    }

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