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