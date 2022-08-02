<template lang="pug">
.visualiser__controls
  select.form-select( aria-label="Field" v-model="selectedTraceLevel")
    option(:value="null" disabled) Select a trace level
    option(v-for="level in traceLevels" :value="level") {{level}}
  .disabled(v-if="!canFlush")
      button.btn.btn-primary(v-on:click="enableLogging")
        | Enable Logging  
  .enabled(v-else)
    button.btn.btn-primary.me-2.flush(v-on:click="flush")
      i.me-2(data-feather="life-buoy")
      span(v-if="!flushing")
        | Flush
      span(v-else)
        | Cancel
    button.btn.btn-primary(v-on:click="disableLogging")
      | Disable Logging

ul.messages(v-if="data != null && data.length > 0")  
  li(v-for="message in data")
      .row.g-2(v-if="message")
          .col.col-md-8.subtitle {{  message.messageType }}
          .col.col-md-4
              button.btn.btn-sm.btn-outline-light( v-on:click="selectMessage(message)")
                  i(data-feather="eye")
                        
</template>
<script setup lang="ts">
import { onMounted, onUpdated, ref} from "vue";
import feather from "feather-icons";
import * as Interfaces from '../Interfaces'
import * as Enums from '../Enums'
import {metaEnv} from "../globals";
import axios from 'axios'
const traceLevels = ref<any[]>([]);
const selectedTraceLevel = ref<Enums.TraceLevel|null>(null);
const data = ref<Interfaces.MessageLogListEntity[]>([])
const flushing = ref<any>('');
const canFlush = ref(false);

const emit = defineEmits(['selectMsg']);

function selectMessage(msg:Interfaces.MessageLogListEntity){
  emit('selectMsg',msg); 
}

function enableLogging(){
    axios.post(`${metaEnv.VITE_API_URL}Telemetry/enablelogging?logLevel=`+selectedTraceLevel.value).then((response: any) => {
      if(response && response.status === 200){
          canFlush.value = true;
      }
  },(error) => {
      console.log('error',error)
  })
}

function disableLogging(){
  if(flushing.value != null){
    stopFlush();
  }
  axios.post(`${metaEnv.VITE_API_URL}Telemetry/disablelogging`,selectedTraceLevel.value).then((response: any) => {
      if(response && response.status === 200){
        canFlush.value = false;
      }
  },(error) => {
      console.log('error',error)
  })
}

function flush(){
  if(!flushing.value){
    flushing.value = setInterval(()=>{
      axios.get(`${metaEnv.VITE_API_URL}Telemetry/flush`).then((response: any) => {
        if(response && response.status === 200){
          console.log('flush', response.data)
          data.value = [...data.value,...pushToList(JSON.parse(JSON.stringify(response?.data)))]
        }
      },(error) => {
        console.log('error',error)
      })
    },5000);
  }
  else{
    stopFlush();
  }
}

function stopFlush(){
    clearInterval(flushing.value);
    flushing.value = null;
}

function pushToList(data:Interfaces.MessageLogListEntity[]){
  let map:any = {}, i, roots:Interfaces.MessageLogListEntity[] = [];
  
  //init nodes
  for (i = 0; i < data.length; i += 1) {
    map[data[i].message.headers.messageId.toString()] = i;
    let msgRef = data[i];
    data[i].children = [];
    if(msgRef.messageType){
        let split1 = msgRef.messageType.split(','),
        split2 = split1[0].split('.');
        if(split2 && split2.length > 0){
          msgRef.messageType = split2[split2.length-1];
        }
      }
  }

  //set roots and children
  for (i = 0; i < data.length; i += 1) {
    let messageRef = data[i];
    let toPushToRef:Interfaces.MessageLogListEntity[] = roots;
    //pushing to roots and data, choose one
    
    if (messageRef.message.headers.causationId != null)   {
      let parentRef = data[map[messageRef.message.headers.causationId.toString()]];
      toPushToRef = parentRef.children;
    }
    //children are pushed to last of msg
    let existingMessage = toPushToRef.find(x=> messageRef.message.headers.messageId == x.message.headers.messageId)

    //if duplicate message merge the two
    if(existingMessage != null && messageRef.actor == existingMessage.actor){
      existingMessage.elapsed = messageRef.elapsed;
      existingMessage.status = messageRef.status;
      existingMessage.error = messageRef.error;
      existingMessage.stackTrace = messageRef.stackTrace;
      existingMessage.children = messageRef.children;
    }else{
      toPushToRef.push(messageRef);
    }
  }
  
  //return as value type
  return JSON.parse(JSON.stringify(roots));
}

onMounted(()=>{
  feather.replace();
  //getting trace levels from enum
  traceLevels.value =  Object.keys(Enums.TraceLevel).filter(k => isNaN(Number(k)));
});
onUpdated(()=>{
  feather.replace();
})
</script>

<style lang="scss" scoped>
.btn{
  &.flush{
    justify-content:center;
    svg{
      justify-self: start;
    }
  }
}
.messages{
  overflow-x:auto;
  width: 100%;
  margin-bottom: unset;
  padding-left: unset;
  li{
    -webkit-line-clamp: 1;
    -webkit-box-orient: vertical;
    display: -webkit-box;
    margin-right: 5px;
    word-break: break-all;
    margin-bottom: 10px;
  }
}

.form-check-input {
  width: 1.25em;
  height: 1.25em;
  margin-top: 0.125em;
  vertical-align: top;
  background-color: rgb(138, 229, 115);
  background-repeat: no-repeat;
  background-position: center;
  background-size: contain;
  border: 1px solid rgba(15, 26, 42, 0.25);
  -webkit-appearance: none;
  -moz-appearance: none;
  appearance: none;
  -webkit-print-color-adjust: exact;
  color-adjust: exact;
}
.form-check-label{
  margin-left: 0.2rem;
}
.was-validated .form-check-input:invalid ~ .form-check-label, .form-check-input.is-invalid ~ .form-check-label {
  color: #e57373;
}
.btn {
  padding: 0.75rem 1.5rem !important;
}
.btn-primary {
  color: #ffffff;
  background-color: #0c69f4;
  border-color: #0c69f4; }
.btn-primary:hover {
  color: #ffffff;
  background-color: #0a59cf;
  border-color: #0a54c3; }
.btn-check:focus + .btn-primary, .btn-primary:focus {
  color: #ffffff;
  background-color: #0a59cf;
  border-color: #0a54c3;
  -webkit-box-shadow: 0 0 0 0 rgba(48, 128, 246, 0.5);
  box-shadow: 0 0 0 0 rgba(48, 128, 246, 0.5); }
.btn-check:checked + .btn-primary,
.btn-check:active + .btn-primary, .btn-primary:active, .btn-primary.active,
.show > .btn-primary.dropdown-toggle {
  color: #ffffff;
  background-color: #0a54c3;
  border-color: #094fb7;
}
.btn-check:checked + .btn-primary:focus,
.btn-check:active + .btn-primary:focus, .btn-primary:active:focus, .btn-primary.active:focus,
.show > .btn-primary.dropdown-toggle:focus {
  -webkit-box-shadow: 0 0 0 0 rgba(48, 128, 246, 0.5);
  box-shadow: 0 0 0 0 rgba(48, 128, 246, 0.5);
}
.btn-primary:disabled, .btn-primary.disabled {
  color: #ffffff;
  background-color: #0c69f4;
  border-color: #0c69f4;
}
.margin-bottom-10{
  margin-bottom: 15px;
}
.result-hidden-node{
  margin-top: 5px;
  gap: 35px;
}
.result-node{
  margin-top: 10px;
  display: flex;
  gap: 20px;
}
.hidden-container{
  justify-content: center;
  width: 120px;
  display: flex;
  gap: 20px;
  background-color: #526175;
  bottom: 2px;
  position: relative;
  margin-top: 3px;
}
.no-text{
  position: absolute;
  bottom: -10px;
  background: gray;
  padding-left: 8px;
  padding-right: 8px;
  border-radius: 5px;
  color: white;
  font-size: 0.9em;
  right: 110px;
  z-index: 1;
}
.yes-text{
  position: absolute;
  bottom: -10px;
  background: gray;
  padding-left: 8px;
  padding-right: 8px;
  border-radius: 5px;
  color: white;
  font-size: 0.9em;
  left: 100px;
  z-index: 1;
}
.arrows-img{
  position: absolute;
  width: 278px;
  text-align: center;
  height: 180px;
  margin-top: -74px;
}
input::-webkit-outer-spin-button,
input::-webkit-inner-spin-button {
  -webkit-appearance: none;
  margin: 0;
}
input[type=number] {
  -moz-appearance: textfield;
}
.form-floating > .form-select{
  padding-top: 0.625rem;
  height: calc(3.5rem - 6px) !important;
}


</style>