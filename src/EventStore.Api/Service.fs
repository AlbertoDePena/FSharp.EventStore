namespace EventStore.Domain

open System
open EventStore.Extensions
open EventStore.DataAccess
open FsToolkit.ErrorHandling

[<RequireQualifiedAccess>]
module Service =

    let private createUniqueId () =
        Guid.NewGuid().ToString("D")

    let getAllStreams (getAllStreams : EventStore.DataAccess.GetAllStreams) = 
        getAllStreams ()
        |> AsyncResult.mapError DomainError.DatabaseError

    let getStream (getStream : EventStore.DataAccess.GetStream) (query : UnvalidatedStreamQuery) =
        Mapper.toStreamQuery query
        |> Result.map (fun q -> String256.value q.StreamName |> StreamName)
        |> Result.mapError DomainError.ValidationError
        |> Async.singleton
        |> AsyncResult.bind (getStream >> AsyncResult.mapError DomainError.DatabaseError)

    let getSnapshots (getSnapshots : EventStore.DataAccess.GetSnapshots) (query : UnvalidatedSnapshotsQuery) =
        Mapper.toSnapshotsQuery query
        |> Result.map (fun q -> String256.value q.StreamName |> StreamName)
        |> Result.mapError DomainError.ValidationError
        |> Async.singleton
        |> AsyncResult.bind (getSnapshots >> AsyncResult.mapError DomainError.DatabaseError)

    let getEvents (getEvents : EventStore.DataAccess.GetEvents) (query : UnvalidatedEventsQuery) = asyncResult {                 
        let! (streamName, startAtVersion) =
            Mapper.toEventsQuery query
            |> Result.map (fun q -> String256.value q.StreamName |> StreamName, NonNegativeInt.value q.StartAtVersion |> Version)
            |> Result.mapError DomainError.ValidationError

        let! result = 
            getEvents streamName startAtVersion
            |> AsyncResult.mapError DomainError.DatabaseError

        return result
    }

    let deleteSnapshots (deleteSnapshots : EventStore.DataAccess.DeleteSnapshots) (query : UnvalidatedSnapshotsQuery) =
        Mapper.toSnapshotsQuery query
        |> Result.map (fun q -> String256.value q.StreamName |> StreamName)
        |> Result.mapError DomainError.ValidationError
        |> Async.singleton
        |> AsyncResult.bind (deleteSnapshots >> AsyncResult.mapError DomainError.DatabaseError)
