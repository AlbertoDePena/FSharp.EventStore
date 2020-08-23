namespace EventStore.Domain

open System
open EventStore.Extensions
open EventStore.DataAccess
open FsToolkit.ErrorHandling

[<RequireQualifiedAccess>]
type DomainError =
    | ValidationError of errorMessage : string           
    | StreamNotFound of streamName : string
    | InvalidVersion
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

type CreateSnapshot = {
    StreamName : string
    Description : string
    Data : string }

[<RequireQualifiedAccess>] 
module Validation =

    let private toOption value =
        if String.IsNullOrWhiteSpace(value) then
           None
        else Some value

    let private string256 =
        toOption >> Option.filter (fun x -> x.Length = 256)

    let checkStreamName (StreamName streamName) =
        streamName
        |> string256
        |> Option.map StreamName
        |> Result.requireSome "Stream name is required and it must be at most 256 characters"

    let checkVersion (Version version) =
        if version < 0
        then Error "The provided version is not valid"
        else Ok (Version version)

    let validateSnapshot (snapshot : CreateSnapshot) = result {
        let! streamName = 
            string256 snapshot.StreamName
            |> Result.requireSome "Stream name is required and it must be at most 256 characters"

        let! description =
            string256 snapshot.Description
            |> Result.requireSome "Snapshot description is required and it must be at most 256 characters"

        return { snapshot with StreamName = streamName; Description = description }
    }        