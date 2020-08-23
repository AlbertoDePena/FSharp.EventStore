namespace EventStore.DataAccess

open System
open System.Data
open Dapper
open FsToolkit.ErrorHandling

[<RequireQualifiedAccess>]
module Repository =

    let private storedProcedure = Nullable CommandType.StoredProcedure

    let getStream (connection : IDbConnection) (StreamName streamName) =
            
        let toOption (stream : Stream) =
            if isNull (box stream)
            then None
            else Some stream

        let param = {| StreamName = streamName |}

        connection.QuerySingleOrDefaultAsync<Stream>("dbo.GetStream", param, commandType = storedProcedure)
        |> AsyncResult.ofTask
        |> AsyncResult.map toOption

    let getAllStreams (connection : IDbConnection) =
        connection.QueryAsync<Stream>("dbo.GetAllStreams", commandType = storedProcedure)
        |> Async.AwaitTask
        |> Async.map Seq.toList

    let getEvents (connection : IDbConnection) (StreamName streamName) (Version startAtVersion) =
        let param = {| StreamName = streamName; StartAtVersion = startAtVersion |}

        connection.QueryAsync<Event>("dbo.GetEvents", param, commandType = storedProcedure)
        |> AsyncResult.ofTask
        |> AsyncResult.map Seq.toList

    let getSnapshots (connection : IDbConnection) (StreamName streamName) =
        let param = {| StreamName = streamName |}

        connection.QueryAsync<Snapshot>("dbo.GetSnapshots", param, commandType = storedProcedure)
        |> AsyncResult.ofTask
        |> AsyncResult.map Seq.toList

    let addStream (connection : IDbConnection) (transaction : IDbTransaction) (stream : Stream) =
        let param = {| Name = stream.Name; Version = stream.Version |}

        connection.ExecuteScalarAsync<int64>("dbo.AddStream", param, transaction, commandType = storedProcedure)
        |> AsyncResult.ofTask
        |> AsyncResult.map UniqueId

    let addEvent (connection : IDbConnection) (transaction : IDbTransaction) (event : Event) =
        let param = {| 
            StreamId = event.StreamId
            Type = event.Type
            Data = event.Data
            Version = event.Version |}

        connection.ExecuteScalarAsync<int64>("dbo.AddEvent", param, transaction, commandType = storedProcedure)
        |> AsyncResult.ofTask
        |> AsyncResult.map UniqueId

    let addSnapshot (connection : IDbConnection) (snapshot : Snapshot) =
        let param = {| 
            StreamId = snapshot.StreamId
            Description = snapshot.Description
            Data = snapshot.Data
            Version = snapshot.Version |}

        connection.ExecuteScalarAsync<int64>("dbo.AddSnapshot", param, commandType = storedProcedure)
        |> AsyncResult.ofTask
        |> AsyncResult.map UniqueId

    let deleteSnapshots (connection : IDbConnection) (StreamName streamName) =
        let param = {| StreamName = streamName |}

        connection.ExecuteAsync("dbo.DeleteSnapshots", param, commandType = storedProcedure)
        |> AsyncResult.ofTask
        |> AsyncResult.ignore

    let updateStream (connection : IDbConnection) (transaction : IDbTransaction) (stream : Stream) =
        let param = {| StreamId = stream.StreamId; Version = stream.Version |}

        connection.ExecuteAsync("dbo.UpdateStream", param, transaction, commandType = storedProcedure)
        |> AsyncResult.ofTask
        |> AsyncResult.ignore