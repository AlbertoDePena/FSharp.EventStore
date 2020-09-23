namespace EventStore.DataAccessTypes

open System

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

type StreamName = StreamName of string

type Version = Version of int32

type GetStream = StreamName -> Async<Stream option>

type GetAllStreams = unit -> Async<Stream list>

type GetEvents = StreamName -> Version -> Async<Event list>

type GetSnapshots = StreamName -> Async<Snapshot list>

type DeleteSnapshots = StreamName -> Async<unit>

type AppendEvents = Stream -> Event list -> Async<unit>

type CreateSnapshot = Snapshot -> Async<unit>