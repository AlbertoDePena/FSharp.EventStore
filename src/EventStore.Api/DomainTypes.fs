namespace EventStore.Domain

open System
open FsToolkit.ErrorHandling

type String50 = private String50 of string

[<RequireQualifiedAccess>]
module String50 =

    let value (String50 x) = x

    let create value =
        if String.IsNullOrWhiteSpace(value)
        then None
        else if value.Length > 50
        then None
        else Some (String50 value)

type String256 = private String256 of string

[<RequireQualifiedAccess>]
module String256 =

    let value (String256 x) = x

    let create value =
        if String.IsNullOrWhiteSpace(value)
        then None
        else if value.Length > 256
        then None
        else Some (String256 value)            

type NonNegativeInt = private NonNegativeInt of Int32

[<RequireQualifiedAccess>]
module NonNegativeInt =

    let value (NonNegativeInt x) = x

    let create (value : int32) =
        if value < 0
        then None
        else Some (NonNegativeInt value)

type StringMax = private StringMax of string

[<RequireQualifiedAccess>]
module StringMax =

    let value (StringMax x) = x

    let create value =
        if String.IsNullOrWhiteSpace(value)
        then None
        else Some (StringMax value) 

type Timestamp = private Timestamp of DateTimeOffset

[<RequireQualifiedAccess>]
module Timestamp =

    let value (Timestamp x) = x

    let create (value : DateTimeOffset) =
        if value = DateTimeOffset.MinValue
        then None
        else if value = DateTimeOffset.MaxValue
        then None
        else Some (Timestamp value)

[<RequireQualifiedAccess>]
type DomainError =
    | ValidationError of errorMessage : string           
    | StreamNotFound
    | InvalidVersion
    | DatabaseError of ex : Exception

type Stream = {
    StreamId : String50
    Version : NonNegativeInt
    Name : String256
    CreatedAt : Timestamp
    UpdatedAt : Timestamp option }

type Event = {
    EventId : String50
    StreamId : String50
    Version : NonNegativeInt
    Data : StringMax
    Type : String256    
    CreatedAt : Timestamp }

type Snapshot = {
    SnapshotId : String50
    StreamId : String50
    Version : NonNegativeInt
    Data : StringMax
    Description : String256    
    CreatedAt : Timestamp } 

type NewEvent = {
    Data : StringMax
    Type : String256 }

type AppendEvents = {
    ExpectedVersion : NonNegativeInt
    StreamName : String256
    Events : NewEvent list }

type CreateSnapshot = {
    StreamName : String256
    Description : String256
    Data : StringMax }

type StreamQuery = { StreamName : String256 }    

type SnapshotsQuery = { StreamName : String256 }

type EventsQuery = { 
    StreamName : String256
    StartAtVersion : NonNegativeInt }

type UnvalidatedNewEvent = {
    Data : string
    Type : string }

type UnvalidatedAppendEvents = {
    ExpectedVersion : int32
    StreamName : string
    Events : UnvalidatedNewEvent list }

type UnvalidatedCreateSnapshot = {
    StreamName : string
    Description : string
    Data : string }

type UnvalidatedStreamQuery = { StreamName : string }    

type UnvalidatedSnapshotsQuery = { StreamName : string }

type UnvalidatedEventsQuery = { 
    StreamName : string
    StartAtVersion : int32 }

[<RequireQualifiedAccess>] 
module Mapper =

    let toCreateSnapshot (snapshot : UnvalidatedCreateSnapshot) : Result<CreateSnapshot, string> = result {
        let! streamName = 
            String256.create snapshot.StreamName
            |> Result.requireSome "Stream name is required and it must be at most 256 characters"

        let! description =
            String256.create snapshot.Description
            |> Result.requireSome "Snapshot description is required and it must be at most 256 characters"
        
        let! data = 
            StringMax.create snapshot.Data
            |> Result.requireSome "Snapshot data is required"

        return { StreamName = streamName; Description = description; Data = data }
    }     
    
    let toNewEvent (event : UnvalidatedNewEvent) : Result<NewEvent, string> = result {
        let! eventType =
            String256.create event.Type
            |> Result.requireSome "Event type is required and it must be at most 256 characters"
        
        let! data = 
            StringMax.create event.Data
            |> Result.requireSome "Event data is required"

        return { Type = eventType; Data = data }
    }

    let toAppendEvents (appendEvents : UnvalidatedAppendEvents) : Result<AppendEvents, string> = result {        
        let! streamName = 
            String256.create appendEvents.StreamName
            |> Result.requireSome "Stream name is required and it must be at most 256 characters"
        
        let! version =
            NonNegativeInt.create appendEvents.ExpectedVersion
            |> Result.requireSome "Stream version is not valid"

        do! appendEvents.Events |> Result.requireNotEmpty "Cannot append to stream without events" 

        let! events = 
            appendEvents.Events 
            |> List.traverseResultM toNewEvent
            
        return { StreamName = streamName; Events = events; ExpectedVersion = version }
    }

    let toStreamQuery (query : UnvalidatedStreamQuery) : Result<StreamQuery, string> = result {
        let! streamName = 
            String256.create query.StreamName
            |> Result.requireSome "Stream name is required and it must be at most 256 characters"

        return { StreamName = streamName }
    }

    let toSnapshotsQuery (query : UnvalidatedSnapshotsQuery) : Result<SnapshotsQuery, string> = result {
        let! streamName = 
            String256.create query.StreamName
            |> Result.requireSome "Stream name is required and it must be at most 256 characters"

        return { StreamName = streamName }
    }

    let toEventsQuery (query : UnvalidatedEventsQuery) : Result<EventsQuery, string> = result {
        let! streamName = 
            String256.create query.StreamName
            |> Result.requireSome "Stream name is required and it must be at most 256 characters"

        let! version =
            NonNegativeInt.create query.StartAtVersion
            |> Result.requireSome "Stream version is not valid"

        return { StreamName = streamName; StartAtVersion = version }
    }