namespace EventStore.DataAccess

open System
open System.Data
open Dapper
open FsToolkit.ErrorHandling
open System.Data.SqlClient

[<RequireQualifiedAccess>]
module Repository =

    let private storedProcedure = Nullable CommandType.StoredProcedure

    let private getDbConnection (DbConnectionString dbConnectionString) =
        let connection = new SqlConnection(dbConnectionString)
        connection.Open()
        connection :> IDbConnection

    let getStream dbConnectionString : GetStream =
        fun (StreamName streamName) ->   
            let toOption (x : Stream) =
                if isNull (box x)
                then None
                else Some x

            let param = {| StreamName = streamName |}

            use connection = getDbConnection dbConnectionString

            connection.QuerySingleOrDefaultAsync<Stream>("dbo.GetStream", param, commandType = storedProcedure)
            |> AsyncResult.ofTask
            |> AsyncResult.map toOption

    let getAllStreams dbConnectionString : GetAllStreams =
        fun () ->
            use connection = getDbConnection dbConnectionString
            
            connection.QueryAsync<Stream>("dbo.GetAllStreams", commandType = storedProcedure)
            |> AsyncResult.ofTask
            |> AsyncResult.map Seq.toList

    let getEvents dbConnectionString: GetEvents =
        fun (StreamName streamName) (Version startAtVersion) ->
            let param = {| StreamName = streamName; StartAtVersion = startAtVersion |}

            use connection = getDbConnection dbConnectionString

            connection.QueryAsync<Event>("dbo.GetEvents", param, commandType = storedProcedure)
            |> AsyncResult.ofTask
            |> AsyncResult.map Seq.toList

    let getSnapshots dbConnectionString : GetSnapshots =
        fun (StreamName streamName) ->
            let param = {| StreamName = streamName |}

            use connection = getDbConnection dbConnectionString

            connection.QueryAsync<Snapshot>("dbo.GetSnapshots", param, commandType = storedProcedure)
            |> AsyncResult.ofTask
            |> AsyncResult.map Seq.toList

    let createSnapshot dbConnectionString : CreateSnapshot =
        fun snapshot ->
            let param = {| 
                StreamId = snapshot.StreamId
                Description = snapshot.Description
                Data = snapshot.Data
                Version = snapshot.Version |}

            use connection = getDbConnection dbConnectionString

            connection.ExecuteScalarAsync<int64>("dbo.CreateSnapshot", param, commandType = storedProcedure)
            |> AsyncResult.ofTask
            |> AsyncResult.ignore

    let deleteSnapshots dbConnectionString : DeleteSnapshots =
        fun (StreamName streamName) ->
            let param = {| StreamName = streamName |}

            use connection = getDbConnection dbConnectionString

            connection.ExecuteAsync("dbo.DeleteSnapshots", param, commandType = storedProcedure)
            |> AsyncResult.ofTask
            |> AsyncResult.ignore

    let appendEvents dbConnectionString : AppendEvents =
        fun stream events ->
            use dt = new DataTable("NewEvents")

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
                NewEvents = dt.AsTableValuedParameter("NewEvents") |}

            use connection = getDbConnection dbConnectionString

            connection.ExecuteAsync("dbo.AppendEvents", param, commandType = storedProcedure)
            |> AsyncResult.ofTask
            |> AsyncResult.ignore           