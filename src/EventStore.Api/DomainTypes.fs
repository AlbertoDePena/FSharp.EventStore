namespace EventStore.DomainTypes

open System
open FsToolkit.ErrorHandling

type StringUnbounded = private StringUnbounded of string

[<RequireQualifiedAccess>]
module StringUnbounded =

    let value (StringUnbounded x) = x

    let tryCreate value =
        if String.IsNullOrWhiteSpace(value)
        then None
        else Some (StringUnbounded value) 

type String50 = private String50 of string

[<RequireQualifiedAccess>]
module String50 =

    let value (String50 x) = x

    let tryCreate value =
        if String.IsNullOrWhiteSpace(value)
        then None
        else if value.Length > 50
        then None
        else Some (String50 value)

type String256 = private String256 of string

[<RequireQualifiedAccess>]
module String256 =

    let value (String256 x) = x

    let tryCreate value =
        if String.IsNullOrWhiteSpace(value)
        then None
        else if value.Length > 256
        then None
        else Some (String256 value)            

type NonNegativeInt = private NonNegativeInt of Int32

[<RequireQualifiedAccess>]
module NonNegativeInt =

    let value (NonNegativeInt x) = x

    let tryCreate (value : int32) =
        if value < 0
        then None
        else Some (NonNegativeInt value)

[<RequireQualifiedAccess>]
type DomainError =
    | ValidationError of errorMessage : string           
    | StreamNotFound
    | InvalidVersion
    | DatabaseError of ex : Exception

type NewEvent = {
    Data : StringUnbounded
    Type : String256 }

type AppendEvents = {
    ExpectedVersion : NonNegativeInt
    StreamName : String256
    Events : NewEvent list }

type CreateSnapshot = {
    StreamName : String256
    Description : String256
    Data : StringUnbounded }

type StreamQuery = { StreamName : String256 }    

type SnapshotsQuery = { StreamName : String256 }

type EventsQuery = { 
    StreamName : String256
    StartAtVersion : NonNegativeInt }

// public types

type UnvalidatedNewEvent = {
    Data : string
    Type : string }

[<RequireQualifiedAccess>]
module UnvalidatedNewEvent =

    let validate (model : UnvalidatedNewEvent) : Result<NewEvent, string> = result {
        let! eventType =
            String256.tryCreate model.Type
            |> Result.requireSome "Event type is required and it must be at most 256 characters"
        
        let! data = 
            StringUnbounded.tryCreate model.Data
            |> Result.requireSome "Event data is required"

        return { Type = eventType; Data = data }
    }

type UnvalidatedAppendEvents = {
    ExpectedVersion : int32
    StreamName : string
    Events : UnvalidatedNewEvent list }

[<RequireQualifiedAccess>]
module UnvalidatedAppendEvents =

    let validate (model : UnvalidatedAppendEvents) : Result<AppendEvents, string> = result {        
        let! streamName = 
            String256.tryCreate model.StreamName
            |> Result.requireSome "Stream name is required and it must be at most 256 characters"
        
        let! version =
            NonNegativeInt.tryCreate model.ExpectedVersion
            |> Result.requireSome "Invalid stream version"

        do! model.Events |> Result.requireNotEmpty "Cannot append to stream without events" 

        let! events = 
            model.Events 
            |> List.traverseResultM UnvalidatedNewEvent.validate
            
        return { StreamName = streamName; Events = events; ExpectedVersion = version }
    }

type UnvalidatedCreateSnapshot = {
    StreamName : string
    Description : string
    Data : string }

[<RequireQualifiedAccess>]
module UnvalidatedCreateSnapshot =

    let validate (model : UnvalidatedCreateSnapshot) : Result<CreateSnapshot, string> = result {
        let! streamName = 
            String256.tryCreate model.StreamName
            |> Result.requireSome "Stream name is required and it must be at most 256 characters"

        let! description =
            String256.tryCreate model.Description
            |> Result.requireSome "Snapshot description is required and it must be at most 256 characters"
        
        let! data = 
            StringUnbounded.tryCreate model.Data
            |> Result.requireSome "Snapshot data is required"

        return { StreamName = streamName; Description = description; Data = data }
    }

type UnvalidatedStreamQuery = { StreamName : string }    


[<RequireQualifiedAccess>]
module UnvalidatedStreamQuery =

    let validate (model : UnvalidatedStreamQuery) : Result<StreamQuery, string> = result {
        let! streamName = 
            String256.tryCreate model.StreamName
            |> Result.requireSome "Stream name is required and it must be at most 256 characters"

        return { StreamName = streamName }
    }

type UnvalidatedSnapshotsQuery = { StreamName : string }

[<RequireQualifiedAccess>]
module UnvalidatedSnapshotsQuery =

    let validate (model : UnvalidatedSnapshotsQuery) : Result<SnapshotsQuery, string> = result {
        let! streamName = 
            String256.tryCreate model.StreamName
            |> Result.requireSome "Stream name is required and it must be at most 256 characters"

        return { StreamName = streamName }
    }

type UnvalidatedEventsQuery = { 
    StreamName : string
    StartAtVersion : int32 }

[<RequireQualifiedAccess>]
module UnvalidatedEventsQuery =

    let validate (model : UnvalidatedEventsQuery) : Result<EventsQuery, string> = result {
        let! streamName = 
            String256.tryCreate model.StreamName
            |> Result.requireSome "Stream name is required and it must be at most 256 characters"

        let! version =
            NonNegativeInt.tryCreate model.StartAtVersion
            |> Result.requireSome "Invalid stream version"

        return { StreamName = streamName; StartAtVersion = version }
    }