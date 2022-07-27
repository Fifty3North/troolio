import {EntityType, MessageLogAction} from "./Enums";

export const NodeAttributes= [
  {
    Id:MessageLogAction.DispatchEvent,
    Type: EntityType.Event,
    IconOfNode: 'mouse-pointer',
    TypeOfNode: 'node--event',
    IconColor: 'node-icon--event',
    titleContent: 'Event Title',
    subTitle: 'DispatchEvent',
  },
  {
    Id:MessageLogAction.StoreEvent,
    Type: EntityType.Event,
    IconOfNode: 'mouse-pointer',
    TypeOfNode: 'node--event',
    IconColor: 'node-icon--event',
    titleContent: 'Event Title',
    subTitle: 'StoreEvent',
  },
  {
    Id: MessageLogAction.HandleCommand,
    Type: EntityType.Command,
    IconOfNode: 'code',
    TypeOfNode: 'node--command',
    IconColor: 'node-icon--command',
    titleContent: 'Command Title',
    subTitle: 'Command',
  },
  {
    Id: MessageLogAction.HandleOrchestratedCommand,
    Type: EntityType.Command,
    IconOfNode: 'code',
    TypeOfNode: 'node--command',
    IconColor: 'node-icon--command',
    subTitle: 'OrchestratedCommand',
  },
  {
    Id: EntityType.Error,
    Type: EntityType.Error,
    IconOfNode: 'alert-circle',
    TypeOfNode: 'node--error',
    IconColor: 'node-icon--error',
    subTitle: 'Error',
  },
  {
    Id:MessageLogAction.Projection,
    Type: EntityType.DbProjection,
    IconOfNode: 'database',
    TypeOfNode: 'node--db',
    IconColor: 'node-icon--db',
    subTitle: 'Projection',
  },
  //todo 
  {
    Id:MessageLogAction.Orchestration,
    Type: EntityType.RESTAPI,
    IconOfNode: 'server',
    TypeOfNode: 'node--api',
    IconColor: 'node-icon--api',
    subTitle: 'Orchestration',
  },
  //todo
  {
    Id:MessageLogAction.ReadModel,
    Type: EntityType.Query,
    IconOfNode: 'help-circle',
    TypeOfNode: 'node--query',
    IconColor: 'node-icon--query',
    subTitle: 'ReadModel',
  },
];