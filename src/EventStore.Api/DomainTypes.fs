namespace EventStore.Domain

open System
open EventStore.Extensions
open EventStore.DataAccess

[<RequireQualifiedAccess>]
type DomainError =
    | ValidationError of errorMessage : string           
    | RecordNotFound of description : string
    | ConcurrencyError of error : string
    | DatabaseError of ex : Exception

type Stream = {
    StreamId : int64
    Version : int32
    Name : string
    CreatedAt : DateTimeOffset
    UpdatedAt : DateTimeOffset option }

type Event = {
    EventId : int64
    StreamId : int64
    Version : int32
    Data : string
    Type : string    
    CreatedAt : DateTimeOffset }

type Snapshot = {
    SnapshotId : int64
    StreamId : int64
    Version : int32
    Data : string
    Description : string    
    CreatedAt : DateTimeOffset } 

type NewEvent = {
    Data : string
    Type : string }

type AppendEvents = {
    ExpectedVersion : int32
    StreamName : string
    Events : NewEvent list }

type AddSnapshot = {
    StreamName : string
    Description : string
    Data : string }

[<RequireQualifiedAccess>] 
module Validation =

    let validateStreamName (StreamName streamName) =
        let propertyName = "Stream Name"
        Strings.createRequired propertyName streamName
        |> Result.bind (Strings.validateMaxLength propertyName 256)
        |> Result.map StreamName

    let validateVersion (Version version) =
        if version < 0
        then Error "The provided version is not valid"
        else Ok (Version version)