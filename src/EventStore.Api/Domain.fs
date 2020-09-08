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

    let getStream (getStream : Repository.GetStream) (query : StreamQuery) =
        query
        |> getStream
        |> AsyncResult.mapError DomainError.DatabaseError
        |> AsyncResult.bind (Async.singleton >> AsyncResult.requireSome DomainError.StreamNotFound)

    let getSnapshots (getSnapshots : Repository.GetSnapshots) (query : SnapshotsQuery) =
        query
        |> getSnapshots
        |> AsyncResult.mapError DomainError.DatabaseError

    let getEvents (getEvents : Repository.GetEvents) (query : EventsQuery) = 
        query
        |> getEvents
        |> AsyncResult.mapError DomainError.DatabaseError

    let deleteSnapshots (deleteSnapshots : Repository.DeleteSnapshots) (query : SnapshotsQuery) =
        query
        |> deleteSnapshots 
        |> AsyncResult.mapError DomainError.DatabaseError

    let createSnapshot (getStream : Repository.GetStream) (createSnapshot : Repository.CreateSnapshot) (model : CreateSnapshot) =        
        let toSnapshot (model : CreateSnapshot) (stream : Stream) : Snapshot = {
            SnapshotId = Guid.NewGuid().ToString("D")
            StreamId = stream.StreamId
            Version = stream.Version
            Description = String256.value model.Description
            Data = StringMax.value model.Data
            CreatedAt = DateTimeOffset.UtcNow }

        let query : StreamQuery = { StreamName = model.StreamName }

        query
        |> getStream
        |> AsyncResult.mapError DomainError.DatabaseError
        |> AsyncResult.bind (Async.singleton >> AsyncResult.requireSome DomainError.StreamNotFound)
        |> AsyncResult.map (toSnapshot model)
        |> AsyncResult.bind (createSnapshot >> AsyncResult.mapError DomainError.DatabaseError)

    let appendEvents (getStream : Repository.GetStream) (appendEvents : Repository.AppendEvents) (model : AppendEvents) =
        
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

            if stream.Version <> (NonNegativeInt.value model.ExpectedVersion) then 
                Error (DomainError.InvalidVersion)
            else
                let toEvent (index : int) (eventModel : NewEvent) = {
                    EventId = Guid.NewGuid().ToString("D")
                    StreamId = stream.StreamId
                    Version = stream.Version + index + 1
                    Type = String256.value eventModel.Type
                    Data = StringMax.value eventModel.Data
                    CreatedAt = DateTimeOffset.UtcNow }

                Ok (stream, model.Events |> List.mapi toEvent)

        let query : StreamQuery = { StreamName = model.StreamName }

        query
        |> getStream
        |> AsyncResult.mapError DomainError.DatabaseError
        |> AsyncResult.bind (toEvents model >> Async.singleton)
        |> AsyncResult.bind (fun (stream, events) -> appendEvents stream events |> AsyncResult.mapError DomainError.DatabaseError)
