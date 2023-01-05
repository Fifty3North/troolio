import { Guid } from "typescript-guid"
import  * as Enums from './Enums';
import {AxiosRequestHeaders} from 'axios'
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
    elapsed:number,
    messageType:string
}
export interface Message{
    headers:Headers
    payload:PayloadValue[]
}
export interface PayloadValue{
    name:any,
    value:any
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

export interface ShoppingList{
    id:string
    title:string
    ownerId:string,
    collaborators:string[]
    items:ShoppingListItem[]
    joinCode?:string
}
export interface ShoppingListItem{
    id:string
    description?:string
    crossedOff:Boolean
    quantity:string
}

export interface AddToShoppingListEmit{
    description:string, 
    quantity:string
}

export interface CreateShoppingListPayload{
    title:string
}

export interface ShoppingListItemPayload {
    itemId:string
}

export interface AddToShoppingListPayload extends AddToShoppingListEmit,ShoppingListItemPayload 
{
}

export interface CheckItemPayload extends ShoppingListItemPayload 
{
}

export interface RemoveItemPayload extends ShoppingListItemPayload 
{
}



export interface PayloadHeaders extends AxiosRequestHeaders {
    userId:string,
    deviceId:string
}