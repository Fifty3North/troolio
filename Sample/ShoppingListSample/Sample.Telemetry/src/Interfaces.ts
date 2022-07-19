import { Guid } from "typescript-guid"
import  * as Enums from './Enums';
export interface MessageLog {
    id: string,
    stream:string,
    message:Message,
    version:any,
    action:Enums.MessageLogAction,
    status:Enums.MessageLogStatus,
    actor:string,
    siloId:string,
    error:string,
    stackTrace:string,
    elapsed:number
}
export interface Message{
    headers:Headers
}
export interface Headers{
    correlationId:string, 
    userId:string,
    deviceId:string,
    messageId:string,
    transactionId?:string,
    causationId?:string,
}
export interface MessageLogListEntity extends MessageLog{
    children: MessageLogListEntity[],
    entity?:Entity
}
export interface MessageCorrelationEntity {
    correlationId: string
    children : MessageLogListEntity[]
}


export interface Entity {
    id: string,
    name: string,
    // x: number,
    // y: number,

    iconOfNode: string,
    typeOfNode: string,
    iconColor: string,
    subTitle: string,
    type:Enums.EntityType
}