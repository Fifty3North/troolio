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
    correlationId:Guid, 
    userId:Guid,
    deviceId:Guid,
    messageId:Guid,
    transactionId?:Guid,
    causationId?:Guid|string,
}
export interface MessageLogListEntity extends MessageLog{
    children: MessageLogListEntity[]
}
export interface MessageCorrelationEntity {
    correlationId: Guid|string
    children : MessageLogListEntity[]
}