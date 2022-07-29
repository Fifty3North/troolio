export enum MessageLogAction
{
  HandleOrchestratedCommand,
  HandleCommand,
  DispatchEvent,
  Orchestration,
  Projection,
  ReadModel,
  StoreEvent
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

export enum LocalVariables{
  UserId = "UserId",
  DeviceId = "DeviceId"
}

export enum Tabs{
  Debug=2,
  ShoppingLists=3
}