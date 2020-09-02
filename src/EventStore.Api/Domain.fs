namespace EventStore.Domain

open System
open EventStore.Extensions
open EventStore.DataAccess
open FsToolkit.ErrorHandling

[<RequireQualifiedAccess>]
module Service =

    let getAllStreams (getAllStreams : Repository.GetAllStreams) = 
        getAllStreams ()
        |> AsyncResult.mapError DomainError.DatabaseError

    let getStream (getStream : Repository.GetStream) (query : UnvalidatedStreamQuery) =
        let getStream =
            getStream
            >> AsyncResult.mapError DomainError.DatabaseError
            >> AsyncResult.bind (Async.singleton >> AsyncResult.requireSome DomainError.StreamNotFound)

        Mapper.toStreamQuery query
        |> Result.mapError DomainError.ValidationError
        |> Result.map (fun q -> String256.value q.StreamName |> StreamName)        
        |> Async.singleton
        |> AsyncResult.bind getStream

    let getSnapshots (getSnapshots : Repository.GetSnapshots) (query : UnvalidatedSnapshotsQuery) =
        Mapper.toSnapshotsQuery query
        |> Result.mapError DomainError.ValidationError
        |> Result.map (fun q -> String256.value q.StreamName |> StreamName)        
        |> Async.singleton
        |> AsyncResult.bind (getSnapshots >> AsyncResult.mapError DomainError.DatabaseError)

    let getEvents (getEvents : Repository.GetEvents) (query : UnvalidatedEventsQuery) = asyncResult {                 
        let! (streamName, startAtVersion) =
            Mapper.toEventsQuery query
            |> Result.mapError DomainError.ValidationError
            |> Result.map (fun q -> String256.value q.StreamName |> StreamName, NonNegativeInt.value q.StartAtVersion |> Version)

        let! result = 
            getEvents streamName startAtVersion
            |> AsyncResult.mapError DomainError.DatabaseError

        return result
    }

    let deleteSnapshots (deleteSnapshots : Repository.DeleteSnapshots) (query : UnvalidatedSnapshotsQuery) =
        Mapper.toSnapshotsQuery query
        |> Result.mapError DomainError.ValidationError
        |> Result.map (fun q -> String256.value q.StreamName |> StreamName)        
        |> Async.singleton
        |> AsyncResult.bind (deleteSnapshots >> AsyncResult.mapError DomainError.DatabaseError)

    let createSnapshot (getStream : Repository.GetStream) (createSnapshot : Repository.CreateSnapshot) (model : UnvalidatedCreateSnapshot) =        
        let toSnapshot (model : CreateSnapshot) (stream : Stream) : Snapshot = {
            SnapshotId = Guid.NewGuid().ToString("D")
            StreamId = stream.StreamId
            Version = stream.Version
            Description = String256.value model.Description
            Data = StringMax.value model.Data
            CreatedAt = DateTimeOffset.UtcNow
        }

        let createSnapshot (model : CreateSnapshot) =
            String256.value model.StreamName
            |> StreamName
            |> getStream
            |> AsyncResult.mapError DomainError.DatabaseError
            |> AsyncResult.bind (Async.singleton >> AsyncResult.requireSome DomainError.StreamNotFound)
            |> AsyncResult.map (toSnapshot model)
            |> AsyncResult.bind (createSnapshot >> AsyncResult.mapError DomainError.DatabaseError)
        
        Mapper.toCreateSnapshot model  
        |> Result.mapError DomainError.ValidationError 
        |> Async.singleton  
        |> AsyncResult.bind createSnapshot

    let appendEvents (getStream : Repository.GetStream) (appendEvents : Repository.AppendEvents) (model : UnvalidatedAppendEvents) =
        
        let toEvents (model : AppendEvents) (streamOption : Stream option) =            
            let stream =
                match streamOption with
                | Some stream -> stream
                | None -> { 
                    StreamId = Guid.NewGuid().ToString("D")
                    Name = String256.value model.StreamName
                    Version = 0
                    CreatedAt = DateTimeOffset.UtcNow 
                    UpdatedAt = Nullable<DateTimeOffset>() }

            if stream.Version <> (NonNegativeInt.value model.ExpectedVersion)
            then Error (DomainError.InvalidVersion)
            else
                let toEvent (index : int) (eventModel : NewEvent) = {
                    EventId = Guid.NewGuid().ToString("D")
                    StreamId = stream.StreamId
                    Version = stream.Version + index + 1
                    Type = String256.value eventModel.Type
                    Data = StringMax.value eventModel.Data
                    CreatedAt = DateTimeOffset.UtcNow 
                }

                Ok (stream, model.Events |> List.mapi toEvent)

        let appendEvents (model : AppendEvents) =
            String256.value model.StreamName
            |> StreamName
            |> getStream
            |> AsyncResult.mapError DomainError.DatabaseError
            |> AsyncResult.bind (toEvents model >> Async.singleton)
            |> AsyncResult.bind (fun (stream, events) -> appendEvents stream events |> AsyncResult.mapError DomainError.DatabaseError)

        Mapper.toAppendEvents model
        |> Result.mapError DomainError.ValidationError 
        |> Async.singleton
        |> AsyncResult.bind appendEvents
