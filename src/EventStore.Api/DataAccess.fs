namespace EventStore.DataAccess

open System
open System.Data
open Dapper
open FsToolkit.ErrorHandling
open System.Data.SqlClient
open EventStore.DataAccessTypes

[<RequireQualifiedAccess>]
module Database =

    let private storedProcedure = Nullable CommandType.StoredProcedure

    let private getDbConnection (DbConnectionString dbConnectionString) =
        let connection = new SqlConnection(dbConnectionString)
        connection.Open()
        connection :> IDbConnection

    let getStream dbConnectionString : GetStream = 
        fun (StreamName streamName) -> async {
            let toOption (x : Stream) =
                if isNull (box x)
                then None
                else Some x

            let param = {| StreamName = streamName |}

            use connection = getDbConnection dbConnectionString

            let! result =
                connection.QuerySingleOrDefaultAsync<Stream>("dbo.GetStream", param, commandType = storedProcedure)
                |> Async.AwaitTask
                |> Async.map toOption

            return result
    }

    let getAllStreams dbConnectionString : GetAllStreams = 
        fun () -> async {
            use connection = getDbConnection dbConnectionString

            let! result =
                connection.QueryAsync<Stream>("dbo.GetAllStreams", commandType = storedProcedure)
                |> Async.AwaitTask
                |> Async.map Seq.toList

            return result
    }

    let getEvents dbConnectionString : GetEvents = 
        fun (StreamName streamName) (Version startAtVersion) -> async {
            let param = {| 
                StreamName = streamName
                StartAtVersion = startAtVersion |}

            use connection = getDbConnection dbConnectionString

            let! result =
                connection.QueryAsync<Event>("dbo.GetEvents", param, commandType = storedProcedure)
                |> Async.AwaitTask
                |> Async.map Seq.toList
             
            return result
    }

    let getSnapshots dbConnectionString : GetSnapshots = 
        fun (StreamName streamName) -> async {
            let param = {| StreamName = streamName |}

            use connection = getDbConnection dbConnectionString

            let! result =
                connection.QueryAsync<Snapshot>("dbo.GetSnapshots", param, commandType = storedProcedure)
                |> Async.AwaitTask
                |> Async.map Seq.toList
             
            return result
    }

    let createSnapshot dbConnectionString : CreateSnapshot = 
        fun (snapshot : Snapshot) -> async {
            let param = {| 
                SnapshotId = snapshot.SnapshotId
                StreamId = snapshot.StreamId
                Description = snapshot.Description
                Data = snapshot.Data
                Version = snapshot.Version |}

            use connection = getDbConnection dbConnectionString

            let! result =
                connection.ExecuteScalarAsync<int64>("dbo.CreateSnapshot", param, commandType = storedProcedure)
                |> Async.AwaitTask
                |> Async.Ignore

            return result
    }

    let deleteSnapshots dbConnectionString : DeleteSnapshots = 
        fun (StreamName streamName) -> async {
            let param = {| StreamName = streamName |}

            use connection = getDbConnection dbConnectionString

            let! result =
                connection.ExecuteAsync("dbo.DeleteSnapshots", param, commandType = storedProcedure)
                |> Async.AwaitTask
                |> Async.Ignore

            return result                    
    }

    let appendEvents dbConnectionString : AppendEvents = 
        fun (stream : Stream) (events : Event list) -> async {
            use dt = new DataTable("NewEvent")

            dt.Columns.Add("EventId", typedefof<string>) |> ignore
            dt.Columns.Add("StreamId", typedefof<string>) |> ignore
            dt.Columns.Add("Type", typedefof<string>) |> ignore
            dt.Columns.Add("Data", typedefof<string>) |> ignore
            dt.Columns.Add("Version", typedefof<int32>) |> ignore

            events
            |> List.iter (fun e -> 
                dt.Rows.Add(
                    e.EventId, e.StreamId, e.Type, e.Data, e.Version) |> ignore)

            let param = {|
                StreamId = stream.StreamId
                StreamName = stream.Name
                Version = stream.Version
                NewEvents = dt.AsTableValuedParameter("NewEvent") |}

            use connection = getDbConnection dbConnectionString

            let! result =
                connection.ExecuteAsync("dbo.AppendEvents", param, commandType = storedProcedure)
                |> Async.AwaitTask
                |> Async.Ignore           

            return result
    }