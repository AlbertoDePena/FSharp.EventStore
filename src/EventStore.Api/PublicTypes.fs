namespace EventStore.PublicTypes

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

type UnvalidatedNewEvent = {
    Data : string
    Type : string }

type UnvalidatedAppendEvents = {
    ExpectedVersion : int32
    StreamName : string
    Events : UnvalidatedNewEvent list }

type UnvalidatedCreateSnapshot = {
    StreamName : string
    Description : string
    Data : string }

type UnvalidatedStreamQuery = { StreamName : string }    

type UnvalidatedSnapshotsQuery = { StreamName : string }

type UnvalidatedEventsQuery = { 
    StreamName : string
    StartAtVersion : int32 }