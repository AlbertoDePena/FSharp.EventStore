namespace EventStore.Domain

open System
open FsToolkit.ErrorHandling
open EventStore.Extensions
open EventStore.DataAccess
open EventStore.DomainTypes
open EventStore.PublicTypes

[<RequireQualifiedAccess>]
module Service =

    let getAllStreams (getAllStreams : Repository.GetAllStreams) = 
        getAllStreams ()
        |> AsyncResult.mapError DomainError.DatabaseError
        
    let getStream (getStream : Repository.GetStream) (query : UnvalidatedStreamQuery) =
        query
        |> UnvalidatedStreamQuery.validate
        |> Result.map (fun q -> String256.value q.StreamName |> StreamName)
        |> Result.mapError DomainError.ValidationError
        |> Async.singleton
        |> AsyncResult.bind (
            getStream >> 
            AsyncResult.mapError DomainError.DatabaseError)
        |> AsyncResult.bind (
            Async.singleton 
            >> AsyncResult.requireSome DomainError.StreamNotFound)

    let getSnapshots (getSnapshots : Repository.GetSnapshots) (query : UnvalidatedSnapshotsQuery) =
        query
        |> UnvalidatedSnapshotsQuery.validate
        |> Result.map (fun q -> String256.value q.StreamName |> StreamName)
        |> Result.mapError DomainError.ValidationError
        |> Async.singleton
        |> AsyncResult.bind (
            getSnapshots 
            >> AsyncResult.mapError DomainError.DatabaseError)

    let getEvents (getEvents : Repository.GetEvents) (query : UnvalidatedEventsQuery) = 
        query
        |> UnvalidatedEventsQuery.validate
        |> Result.map (fun q -> (String256.value q.StreamName |> StreamName, NonNegativeInt.value q.StartAtVersion |> Version))
        |> Result.mapError DomainError.ValidationError
        |> Async.singleton
        |> AsyncResult.bind (fun (streamName, startAtVersion) -> 
            getEvents streamName startAtVersion 
            |> AsyncResult.mapError DomainError.DatabaseError)

    let deleteSnapshots (deleteSnapshots : Repository.DeleteSnapshots) (query : UnvalidatedSnapshotsQuery) =
        query
        |> UnvalidatedSnapshotsQuery.validate
        |> Result.map (fun q -> String256.value q.StreamName |> StreamName)
        |> Result.mapError DomainError.ValidationError
        |> Async.singleton
        |> AsyncResult.bind (
            deleteSnapshots 
            >> AsyncResult.mapError DomainError.DatabaseError)

    let createSnapshot (getStream : Repository.GetStream) (createSnapshot : Repository.CreateSnapshot) (model : UnvalidatedCreateSnapshot) =        
        let toSnapshot (model : CreateSnapshot) (stream : EventStore.DataAccess.Stream) : EventStore.DataAccess.Snapshot = {
            SnapshotId = Guid.NewGuid().ToString("D")
            StreamId = stream.StreamId
            Version = stream.Version
            Description = String256.value model.Description
            Data = StringUnbounded.value model.Data
            CreatedAt = DateTimeOffset.UtcNow }

        model
        |> UnvalidatedCreateSnapshot.validate
        |> Result.mapError DomainError.ValidationError
        |> Async.singleton
        |> AsyncResult.bind (fun model ->
            let streamName = String256.value model.StreamName |> StreamName
            getStream streamName
            |> AsyncResult.mapError DomainError.DatabaseError
            |> AsyncResult.bind (fun streamOption ->
                Async.singleton streamOption
                |> AsyncResult.requireSome DomainError.StreamNotFound
                |> AsyncResult.bind (fun stream -> 
                    toSnapshot model stream 
                    |> createSnapshot 
                    |> AsyncResult.mapError DomainError.DatabaseError)))
        

    let appendEvents (getStream : Repository.GetStream) (appendEvents : Repository.AppendEvents) (model : UnvalidatedAppendEvents) =
        
        let toEvents (model : AppendEvents) (streamOption : EventStore.DataAccess.Stream option) =            
            let stream =
                match streamOption with
                | Some stream -> stream
                | None -> { 
                    StreamId = Guid.NewGuid().ToString("D")
                    Name = String256.value model.StreamName
                    Version = 0
                    CreatedAt = DateTimeOffset.UtcNow 
                    UpdatedAt = Nullable<DateTimeOffset>() }

            if stream.Version <> (NonNegativeInt.value model.ExpectedVersion) then 
                Error (DomainError.InvalidVersion)
            else
                let toEvent (index : int) (eventModel : NewEvent) : EventStore.DataAccess.Event = {
                    EventId = Guid.NewGuid().ToString("D")
                    StreamId = stream.StreamId
                    Version = stream.Version + index + 1
                    Type = String256.value eventModel.Type
                    Data = StringUnbounded.value eventModel.Data
                    CreatedAt = DateTimeOffset.UtcNow }

                Ok (stream, model.Events |> List.mapi toEvent)

        model
        |> UnvalidatedAppendEvents.validate
        |> Result.mapError DomainError.ValidationError
        |> Async.singleton
        |> AsyncResult.bind (fun model ->
            let streamName = String256.value model.StreamName |> StreamName
            getStream streamName
            |> AsyncResult.mapError DomainError.DatabaseError
            |> AsyncResult.bind (fun streamOption ->
                toEvents model streamOption 
                |> Async.singleton
                |> AsyncResult.bind (fun (stream, events) ->
                    appendEvents stream events 
                    |> AsyncResult.mapError DomainError.DatabaseError)))
