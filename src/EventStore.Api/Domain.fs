namespace EventStore.Domain

open System
open EventStore.Extensions
open EventStore.DataAccess
open FsToolkit.ErrorHandling

[<RequireQualifiedAccess>]
module Service =

    let private createUniqueId () =
        Guid.NewGuid().ToString("D")

    let getAllStreams (getAllStreams : Repository.GetAllStreams) = 
        getAllStreams ()
        |> AsyncResult.mapError DomainError.DatabaseError

    let getStream (getStream : Repository.GetStream) (query : UnvalidatedStreamQuery) =
        let getStream =
            getStream
            >> AsyncResult.mapError DomainError.DatabaseError
            >> AsyncResult.bind (Async.singleton >> AsyncResult.requireSome DomainError.StreamNotFound)

        Mapper.toStreamQuery query
        |> Result.map (fun q -> String256.value q.StreamName |> StreamName)
        |> Result.mapError DomainError.ValidationError
        |> Async.singleton
        |> AsyncResult.bind getStream

    let getSnapshots (getSnapshots : Repository.GetSnapshots) (query : UnvalidatedSnapshotsQuery) =
        Mapper.toSnapshotsQuery query
        |> Result.map (fun q -> String256.value q.StreamName |> StreamName)
        |> Result.mapError DomainError.ValidationError
        |> Async.singleton
        |> AsyncResult.bind (getSnapshots >> AsyncResult.mapError DomainError.DatabaseError)

    let getEvents (getEvents : Repository.GetEvents) (query : UnvalidatedEventsQuery) = asyncResult {                 
        let! (streamName, startAtVersion) =
            Mapper.toEventsQuery query
            |> Result.map (fun q -> String256.value q.StreamName |> StreamName, NonNegativeInt.value q.StartAtVersion |> Version)
            |> Result.mapError DomainError.ValidationError

        let! result = 
            getEvents streamName startAtVersion
            |> AsyncResult.mapError DomainError.DatabaseError

        return result
    }

    let deleteSnapshots (deleteSnapshots : Repository.DeleteSnapshots) (query : UnvalidatedSnapshotsQuery) =
        Mapper.toSnapshotsQuery query
        |> Result.map (fun q -> String256.value q.StreamName |> StreamName)
        |> Result.mapError DomainError.ValidationError
        |> Async.singleton
        |> AsyncResult.bind (deleteSnapshots >> AsyncResult.mapError DomainError.DatabaseError)

    let createSnapshot (getStream : Repository.GetStream) (createSnapshot : Repository.CreateSnapshot) (model : UnvalidatedCreateSnapshot) =        
        let toSnapshot (newSnapshot : CreateSnapshot) (stream : Stream) : Snapshot = {
            SnapshotId = createUniqueId ()
            StreamId = stream.StreamId
            Version = stream.Version
            Description = String256.value newSnapshot.Description
            Data = StringMax.value newSnapshot.Data
            CreatedAt = DateTimeOffset.UtcNow
        }

        let createSnapshot (newSnapshot : CreateSnapshot) =
            String256.value newSnapshot.StreamName
            |> StreamName
            |> getStream
            |> AsyncResult.mapError DomainError.DatabaseError
            |> AsyncResult.bind (Async.singleton >> AsyncResult.requireSome DomainError.StreamNotFound)
            |> AsyncResult.map (toSnapshot newSnapshot)
            |> AsyncResult.bind (createSnapshot >> AsyncResult.mapError DomainError.DatabaseError)
        
        Mapper.toCreateSnapshot model  
        |> Result.mapError DomainError.ValidationError 
        |> Async.singleton  
        |> AsyncResult.bind createSnapshot
