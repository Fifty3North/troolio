<template lang="pug">
.shoppingLists
    .header
        .form
            input.form-control.add-task(type='text' v-model="ShoppingListName" placeholder='New Shopping List...' @keydown.enter="CreateNewShoppingList")
            button.btn.btn-primary(v-on:click="CreateNewShoppingList") Create New
    .data
        ShoppingList(v-for="list in ShoppingLists" 
            :shoppingList="list"
            :key="list.id"
            @add="AddItemToList(list.id, $event)"
            @check="CheckItem(list.id, $event)"
            @remove="RemoveItem(list.id, $event)" )
</template>
<script lang="ts">
export default {
    name:'ShoppingLists'
}
</script>
<script lang="ts" setup>
import axios from 'axios'
import { ref } from 'vue'
import {metaEnv} from "../globals";
import { Guid } from 'typescript-guid';
import { LocalVariables } from '../Enums'
import * as Interfaces from '../Interfaces';
import ShoppingList from './ShoppingList.vue';
const ShoppingLists = ref<Interfaces.ShoppingList[]>([]);
const ShoppingListName = ref('');

function GetHeaders():Interfaces.PayloadHeaders{
    const userId =  localStorage.getItem(LocalVariables.UserId) as string;
    const deviceId =  localStorage.getItem(LocalVariables.DeviceId) as string;
    const toReturn:Interfaces.PayloadHeaders = {
        'userId': userId,
        'deviceId': deviceId
    }
    return toReturn;
}

function CreateNewShoppingList(){
    if(!ShoppingListName.value.trim()){
        return;
    }
    
    const headers:Interfaces.PayloadHeaders = GetHeaders();
    const shoppingListId = Guid.create().toString();
    const payload:Interfaces.CreateShoppingListPayload = {
        title: ShoppingListName.value
    }
    
    axios.post(`${metaEnv.VITE_API_URL}ShoppingList/${shoppingListId}/CreateNewList`,payload, { headers }).then((response: any) => {
        if(response && response.status === 200){
            const newShoppingList:Interfaces.ShoppingList ={
                id:shoppingListId,
                title:payload.title,
                ownerId:headers.userId,
                collaborators:[],
                items:[]
            } 
            ShoppingLists.value.push(newShoppingList);
            ShoppingListName.value='';
        }
    },(error) => {
        console.log('error',error)
    })
}

//trims 0 from the start
const regexExp = RegExp('^[0]*')
function AddItemToList(listId:string,emitPayload:Interfaces.AddToShoppingListEmit){
    const headers:Interfaces.PayloadHeaders = GetHeaders();

    emitPayload.quantity = emitPayload.quantity.replace(regexExp,'')
    const payload:Interfaces.AddToShoppingListPayload = {
        description: emitPayload.description,
        quantity: emitPayload.quantity,
        itemId: Guid.create().toString()
    }
    
    axios.post(`${metaEnv.VITE_API_URL}ShoppingList/${listId}/AddItemToList`,payload, { headers }).then((response: any) => {
        if(response && response.status === 200){
            const newShoppingList:Interfaces.ShoppingListItem ={
                id:payload.itemId,
                crossedOff:false,
                quantity:payload.quantity,
                description:payload.description
            } 
            ShoppingLists.value.find(x=>x.id == listId)?.items.push(newShoppingList);     
        }
    },(error) => {
        console.log('error',error)
    })
}

function CheckItem(listId:string, itemId:string){
    const headers:Interfaces.PayloadHeaders = GetHeaders();
    const payload:Interfaces.CheckItemPayload = {
        itemId:itemId
    }
    
    axios.post(`${metaEnv.VITE_API_URL}ShoppingList/${listId}/CrossItemOffList`,payload, { headers }).then((response: any) => {
        if(response && response.status === 200){
            let found = ShoppingLists.value.find(x=>x.id == listId)?.items.find(i=>i.id == itemId);
            if(found != null){
                found.crossedOff = true;
            }
        }
    },(error) => {
        console.log('error',error)
    })
}

function RemoveItem(listId:string, itemId:string){
    const headers:Interfaces.PayloadHeaders = GetHeaders();
    const payload:Interfaces.RemoveItemPayload = { 
        itemId:itemId 
    }
    
    axios.post(`${metaEnv.VITE_API_URL}ShoppingList/${listId}/RemoveItemFromList`,payload, { headers }).then((response: any) => {
        if(response && response.status === 200){
            let shoppingList = ShoppingLists.value.find(x=>x.id == listId);
            const foundIndex = shoppingList?.items.findIndex(i=>i.id == itemId);
            if(shoppingList && foundIndex != null){
                shoppingList.items.splice(foundIndex,1);
            }
        }
    },(error) => {
        console.log('error',error)
    })
}

</script>
<style lang="scss" scoped>
.shoppingLists{
    height: 100%;
    width:100%;
    .header{
        display: flex;
        justify-content: center;
        padding:1rem;
        text-align: left;
        .form{
            display:flex;
            input{
                width: fit-content;
                margin-right: 10px;
            }
            button{
                
            }
        }
    }
    .data{
        max-height: 88%;
        overflow:auto;
    }
}
</style>