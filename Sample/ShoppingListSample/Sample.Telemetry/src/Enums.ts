export enum MessageLogAction
{
    HandleOrchestratedCommand,
    HandleCommand,
    DispatchEvent,
    Orchestration,
    Projection,
    ReadModel
}

export enum MessageLogStatus
{
    Started,
    Completed,
    Error
}
export enum TraceLevel
{
  None,
  Critical,
  Error,
  Debug,
  Information,
  Tracing
}

export enum EntityType {
    Actor,
    Command,
    Event,
    Query,
    UI,
    State,
    Constraint,
    Error,
    DbProjection,
    WebProjection,
    Webhook,
    Timer,
    Check,
    RESTAPI
  };