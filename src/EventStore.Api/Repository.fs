namespace EventStore.DataAccess

open System
open System.Data
open Dapper
open FsToolkit.ErrorHandling
open System.Data.SqlClient
open EventStore.Domain

[<CLIMutable>]
type Stream = {
    StreamId : string
    Version : int32
    Name : string
    CreatedAt : DateTimeOffset
    UpdatedAt : DateTimeOffset Nullable }

[<CLIMutable>]    
type Event = {
    EventId : string
    StreamId : string
    Version : int32
    Data : string
    Type : string    
    CreatedAt : DateTimeOffset }

[<CLIMutable>]
type Snapshot = {
    SnapshotId : string
    StreamId : string
    Version : int32
    Data : string
    Description : string    
    CreatedAt : DateTimeOffset }

type DbConnectionString = DbConnectionString of string

type RepositoryException = exn

[<RequireQualifiedAccess>]
module Database =

    let private storedProcedure = Nullable CommandType.StoredProcedure

    let private getDbConnection (DbConnectionString dbConnectionString) =
        let connection = new SqlConnection(dbConnectionString)
        connection.Open()
        connection :> IDbConnection

    let getStream dbConnectionString (query : StreamQuery) = async {
        let toOption (x : Stream) =
            if isNull (box x)
            then None
            else Some x

        let param = {| StreamName = String256.value query.StreamName |}

        use connection = getDbConnection dbConnectionString

        let! result =
            connection.QuerySingleOrDefaultAsync<Stream>("dbo.GetStream", param, commandType = storedProcedure)
            |> Async.AwaitTask
            |> Async.map toOption

        return result
    }

    let getAllStreams dbConnectionString = async {
        use connection = getDbConnection dbConnectionString

        let! result =
            connection.QueryAsync<Stream>("dbo.GetAllStreams", commandType = storedProcedure)
            |> Async.AwaitTask
            |> Async.map Seq.toList

        return result
    }

    let getEvents dbConnectionString (query : EventsQuery) = async {
        let param = {| 
            StreamName = String256.value query.StreamName
            StartAtVersion = NonNegativeInt.value query.StartAtVersion |}

        use connection = getDbConnection dbConnectionString

        let! result =
            connection.QueryAsync<Event>("dbo.GetEvents", param, commandType = storedProcedure)
            |> Async.AwaitTask
            |> Async.map Seq.toList
         
        return result
    }

    let getSnapshots dbConnectionString (query : SnapshotsQuery) = async {
        let param = {| StreamName = String256.value query.StreamName |}

        use connection = getDbConnection dbConnectionString

        let! result =
            connection.QueryAsync<Snapshot>("dbo.GetSnapshots", param, commandType = storedProcedure)
            |> Async.AwaitTask
            |> Async.map Seq.toList
         
        return result
    }

    let createSnapshot dbConnectionString (snapshot : Snapshot) = async {
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

    let deleteSnapshots dbConnectionString (query : SnapshotsQuery) = async {
        let param = {| StreamName = String256.value query.StreamName |}

        use connection = getDbConnection dbConnectionString

        let! result =
            connection.ExecuteAsync("dbo.DeleteSnapshots", param, commandType = storedProcedure)
            |> Async.AwaitTask
            |> Async.Ignore

        return result                    
    }

    let appendEvents dbConnectionString (stream : Stream) (events : Event list) = async {
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

[<RequireQualifiedAccess>]
module Repository =

    type GetStream = StreamQuery -> Async<Result<Stream option, RepositoryException>>

    type GetAllStreams = unit -> Async<Result<Stream list, RepositoryException>>

    type GetEvents = EventsQuery -> Async<Result<Event list, RepositoryException>>

    type GetSnapshots = SnapshotsQuery -> Async<Result<Snapshot list, RepositoryException>>

    type DeleteSnapshots = SnapshotsQuery -> Async<Result<unit, RepositoryException>>

    type AppendEvents = Stream -> Event list -> Async<Result<unit, RepositoryException>>

    type CreateSnapshot = Snapshot -> Async<Result<unit, RepositoryException>>

    let getStream dbConnectionString : GetStream =
        fun streamName -> 
            Database.getStream dbConnectionString streamName       
            |> Async.Catch
            |> Async.map Result.ofChoice   
         
    let getAllStreams dbConnectionString : GetAllStreams =
        fun () -> 
            Database.getAllStreams dbConnectionString
            |> Async.Catch
            |> Async.map Result.ofChoice
          
    let getEvents dbConnectionString: GetEvents =
        fun query -> 
            Database.getEvents dbConnectionString query
            |> Async.Catch
            |> Async.map Result.ofChoice

    let getSnapshots dbConnectionString : GetSnapshots =
        fun query -> 
            Database.getSnapshots dbConnectionString query
            |> Async.Catch
            |> Async.map Result.ofChoice

    let createSnapshot dbConnectionString : CreateSnapshot =
        fun snapshot -> 
            Database.createSnapshot dbConnectionString snapshot
            |> Async.Catch
            |> Async.map Result.ofChoice

    let deleteSnapshots dbConnectionString : DeleteSnapshots =
        fun query -> 
            Database.deleteSnapshots dbConnectionString query
            |> Async.Catch
            |> Async.map Result.ofChoice

    let appendEvents dbConnectionString : AppendEvents =
        fun stream events -> 
            Database.appendEvents dbConnectionString stream events
            |> Async.Catch
            |> Async.map Result.ofChoice