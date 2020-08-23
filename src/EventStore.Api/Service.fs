namespace EventStore.Domain

open System
open EventStore.Extensions
open EventStore.DataAccess
open FsToolkit.ErrorHandling

[<RequireQualifiedAccess>]
module Service =

    let private validateStreamName (StreamName streamName) =
        streamName
        |> Validation.string256
        |> Option.map StreamName
        |> Result.requireSome "Stream name is required and it must be at most 256 characters"
        |> Result.mapError DomainError.ValidationError
        |> Async.singleton

    let private checkVersion =
        Validation.checkVersion
        >> Result.mapError DomainError.ValidationError
        >> Async.singleton        
   
    let getAllStreams (getAllStreams : EventStore.DataAccess.GetAllStreams) = 
        getAllStreams ()
        |> AsyncResult.mapError DomainError.DatabaseError

    let getStream (getStream : EventStore.DataAccess.GetStream) streamName = 
        validateStreamName streamName
        |> AsyncResult.bind (getStream >> AsyncResult.mapError DomainError.DatabaseError)

    let getSnapshots (getSnapshots : EventStore.DataAccess.GetSnapshots) streamName = 
        validateStreamName streamName
        |> AsyncResult.bind (getSnapshots >> AsyncResult.mapError DomainError.DatabaseError)

    let getEvents (getEvents : EventStore.DataAccess.GetEvents) streamName startAtVersion = asyncResult {          
        let! streamName = validateStreamName streamName
        let! startAtVersion = checkVersion startAtVersion
              
        let! result = 
            getEvents streamName startAtVersion
            |> AsyncResult.mapError DomainError.DatabaseError

        return result
    }

    let deleteSnapshots (deleteSnapshots : EventStore.DataAccess.DeleteSnapshots) streamName = 
        validateStreamName streamName
        |> AsyncResult.bind (deleteSnapshots >> AsyncResult.mapError DomainError.DatabaseError)

    let createSnapshot 
        (getStream : EventStore.DataAccess.GetStream) 
        (createSnapshot : EventStore.DataAccess.CreateSnapshot) 
        (newSnapshot : EventStore.Domain.CreateSnapshot) = asyncResult {
           
        let buildSnapshot (stream : EventStore.DataAccess.Stream) = {
            SnapshotId = Guid.NewGuid().ToString("D")
            StreamId = stream.StreamId
            Version = stream.Version
            Data = newSnapshot.Data
            Description = newSnapshot.Description
            CreatedAt =  DateTimeOffset.UtcNow
        }
        
        let! streamOption =
            getStream (StreamName newSnapshot.StreamName)
            |> AsyncResult.mapError DomainError.DatabaseError

        let result =
            match streamOption with
            | None -> 
                newSnapshot.StreamName
                |> DomainError.StreamNotFound 
                |> AsyncResult.returnError
            | Some stream ->
                stream
                |> buildSnapshot 
                |> createSnapshot 
                |> AsyncResult.mapError DomainError.DatabaseError

        return result
    }