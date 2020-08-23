namespace EventStore.DataAccess

open System

type Stream = {
    StreamId : string
    Version : int32
    Name : string
    CreatedAt : DateTimeOffset
    UpdatedAt : DateTimeOffset Nullable }

type Event = {
    EventId : string
    StreamId : string
    Version : int32
    Data : string
    Type : string    
    CreatedAt : DateTimeOffset }

type Snapshot = {
    SnapshotId : string
    StreamId : string
    Version : int32
    Data : string
    Description : string    
    CreatedAt : DateTimeOffset }  

type DbConnectionString = DbConnectionString of string

type StreamName = StreamName of string

type Version = Version of int32

type RepositoryException = exn

type GetStream = StreamName -> Async<Result<Stream option, RepositoryException>>

type GetAllStreams = unit -> Async<Result<Stream list, RepositoryException>>

type GetEvents = StreamName -> Version -> Async<Result<Event list, RepositoryException>>

type GetSnapshots = StreamName -> Async<Result<Snapshot list, RepositoryException>>

type DeleteSnapshots = StreamName -> Async<Result<unit, RepositoryException>>

type AppendEvents = Stream -> Event list -> Async<Result<unit, RepositoryException>>

type CreateSnapshot = Snapshot -> Async<Result<unit, RepositoryException>>