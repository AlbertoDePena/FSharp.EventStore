namespace EventStore.DataAccess

open System

type Stream = {
    StreamId : int64
    Version : int32
    Name : string
    CreatedAt : DateTimeOffset
    UpdatedAt : DateTimeOffset Nullable }

type Event = {
    EventId : int64
    StreamId : int64
    Version : int32
    Data : string
    Type : string    
    CreatedAt : DateTimeOffset }

type Snapshot = {
    SnapshotId : int64
    StreamId : int64
    Version : int32
    Data : string
    Description : string    
    CreatedAt : DateTimeOffset }  

type UniqueId = UniqueId of int64

type StreamName = StreamName of string

type Version = Version of int32
