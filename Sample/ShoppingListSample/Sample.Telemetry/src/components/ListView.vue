<template lang="pug">
.visualiser
    .visualiser__left
        .visualiser-wrapper
            .visualiser__header
                a.visualiser__logo(title="Trool.io")
                    img(src="../images/troolio-logo.svg", alt="Trool.io")
                    span.h5 Trool.io 
                //- h6 flush status = {{flushing}}


        .visualiser__content       
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
              button.btn.btn-primary(v-on:click="disableLogging" style="margin-right:10px;  ")
                | Disable Logging

          ul.messages(v-if="data != null && data.length > 0")  
            li(v-for="message in data")
                .row.g-2(v-if="message")
                    .col.col-md-8.subtitle {{  message.messageType }}
                    .col.col-md-4
                        button.btn.btn-sm.btn-outline-light( v-on:click="selectMessage(message)")
                            i(data-feather="eye")
                            
    .visualiser__right
      ul.tree.root(v-if="selectedMessage != null")
        li
          EntityNode(:message="selectedMessage")
</template>
<script setup lang="ts">
import { onMounted, onUpdated, ref} from "vue";
import { Guid } from 'typescript-guid'
import feather from "feather-icons";
import * as Interfaces from '../Interfaces'
import * as Enums from '../Enums'
import {metaEnv} from "../globals";
import axios, { AxiosError } from 'axios'
import {NodeAttributes} from '../nodeAttributes'
import '../scss/general.scss';
import EntityNode from '../components/EntityNode.vue'
const traceLevels = ref<any[]>([]);
const selectedTraceLevel = ref<Enums.TraceLevel|null>(null);
const selectedMessage = ref<Interfaces.MessageLogListEntity>()
const flushing = ref<any>('');

const data = ref<Interfaces.MessageLogListEntity[]>([])
const canFlush = ref(false);
function selectMessage(msg:Interfaces.MessageLogListEntity){
  selectedMessage.value = mapAndformatNodeAndChildren(msg);
}

function mapAndformatNodeAndChildren(msg:Interfaces.MessageLogListEntity){
  let toReturn:Interfaces.MessageLogListEntity = msg;
  let attrId:any = msg.action;
  //if error
  if(msg.error || (msg.elapsed == -1 && msg.status != Enums.MessageLogStatus.Completed)){
    attrId = Enums.EntityType.Error;
  }
  let attr = NodeAttributes.find(attr=>attr.Id == attrId)
  if(attr != null){
    toReturn.entity = {
      id:msg.message.headers.messageId,
      name: msg.messageType,
      iconOfNode:attr.IconOfNode as string,
      typeOfNode:attr.TypeOfNode as string,
      iconColor:attr.IconColor as string,
      subTitle:attr.subTitle,
      type:attr.Type as Enums.EntityType
    }
  }
  toReturn.children?.forEach((child)=>{
    child = mapAndformatNodeAndChildren(child);
  })
  return toReturn;
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
function stopFlush(){
    clearInterval(flushing.value);
    flushing.value = null;
}

function flush(){
  if(!flushing.value){
    flushing.value = setInterval(()=>{
      axios.get(`${metaEnv.VITE_API_URL}Telemetry/flush`).then((response: any) => {
        if(response && response.status === 200){
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
.visualiser {
  &__status{
    color: black;
    background-color:gray;
    &.COMPLETED{
      background-color: green;
    }
    &.STARTED{
      background-color:yellow;
    }
  }
  &__controls{
    margin-top: 60px;
    margin-bottom: 20px;
    display: inline-flex;
    select{
      margin-right:10px;
      width: fit-content;
    }
  }
//   position: absolute !important;
  width: 100%;
  min-height: 100vh;
  display: -webkit-box;
  display: -ms-flexbox;
  display: flex;
  font-weight: 450;
  &__left{
    -webkit-box-flex: 1;
    -ms-flex: 1;
    flex: 1;
    display: -webkit-box;
    display: -ms-flexbox;
    display: flex;
    -webkit-box-orient: vertical;
    -webkit-box-direction: normal;
    -ms-flex-direction: column;
    flex-direction: column;
    -webkit-box-align: center;
    -ms-flex-align: center;
    align-items: center;
    -webkit-box-pack: center;
    -ms-flex-pack: center;
    justify-content: center;
    .visualiser__content{
      text-align: center;
    }
  }
  &__right {
    display: flex !important;
    flex-direction: column;
    align-items: center;
    .root{
      margin-top: auto;
      margin-bottom: auto;
      padding-bottom: 10px;
    }
  }
  &__logo {
    position: absolute;
    top: 2rem;
    left: 2rem;
    display: -webkit-box;
    display: -ms-flexbox;
    display: flex;
    margin-bottom: 2rem;
    text-decoration: none;
    .h5 {
      margin-bottom: 0;
      margin-left: 0.5rem;
    }
  }
  &__name {
    margin-top: 70px;
    display: block;
    max-width: 350px;
  }
  &__content {
    margin-bottom: 50px;
    width: 100%;
    display: contents;
  }
  .visualiser-wrapper {
    max-width: 600px;
    margin: 0 auto;
  }
  .visualiser__header {
    width: 100%;
    margin-bottom: 1rem;
  }
  .subtitle {
    align-self: center;
  }
  .form-label {
    margin-bottom: 0.5rem;
  }
  .form-control {
    display: block;
    width: 100%;
    padding: 0.75rem 0.75rem;
    font-size: 1rem;
    font-weight: 400;
    line-height: 1.5;
    color: #526175;
    background-color: #ffffff;
    background-clip: padding-box;
    border: 1px solid #d6dbe1;
    -webkit-appearance: none;
    -moz-appearance: none;
    appearance: none;
    border-radius: 0.5rem;
    -webkit-transition: border-color 0.15s ease-in-out, -webkit-box-shadow 0.15s ease-in-out;
    transition: border-color 0.15s ease-in-out, -webkit-box-shadow 0.15s ease-in-out;
    transition: border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out;
    transition: border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out, -webkit-box-shadow 0.15s ease-in-out; }
  .form-control[type="file"] {
    overflow: hidden; }
  .form-control[type="file"]:not(:disabled):not([readonly]) {
    cursor: pointer; }
  .form-control:focus {
    color: #526175;
    background-color: #ffffff;
    border-color: #86b4fa;
    outline: 0;
    -webkit-box-shadow: 0 0 0 0 rgba(12, 105, 244, 0);
    box-shadow: 0 0 0 0 rgba(12, 105, 244, 0); }
  .form-control::-webkit-date-and-time-value {
    height: 1.5em; }
  .form-control::-webkit-input-placeholder {
    color: #69778c;
    opacity: 1; }
  .form-control::-moz-placeholder {
    color: #69778c;
    opacity: 1; }
  .form-control:-ms-input-placeholder {
    color: #69778c;
    opacity: 1; }
  .form-control::-ms-input-placeholder {
    color: #69778c;
    opacity: 1; }
  .form-control::placeholder {
    color: #69778c;
    opacity: 1; }
  .form-control:disabled, .form-control[readonly] {
    background-color: #f0f2f5;
    opacity: 1; }
  .form-control::file-selector-button {
    padding: 0.75rem 0.75rem;
    margin: -0.75rem -0.75rem;
    -webkit-margin-end: 0.75rem;
    margin-inline-end: 0.75rem;
    color: #526175;
    background-color: #f0f2f5;
    pointer-events: none;
    border-color: inherit;
    border-style: solid;
    border-width: 0;
    border-inline-end-width: 1px;
    border-radius: 0;
    -webkit-transition: color 0.15s ease-in-out, background-color 0.15s ease-in-out, border-color 0.15s ease-in-out, -webkit-box-shadow 0.15s ease-in-out;
    transition: color 0.15s ease-in-out, background-color 0.15s ease-in-out, border-color 0.15s ease-in-out, -webkit-box-shadow 0.15s ease-in-out;
    transition: color 0.15s ease-in-out, background-color 0.15s ease-in-out, border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out;
    transition: color 0.15s ease-in-out, background-color 0.15s ease-in-out, border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out, -webkit-box-shadow 0.15s ease-in-out;
  }
  .form-control:hover:not(:disabled):not([readonly])::file-selector-button {
    background-color: #e4e6e9; }
  .form-control::-webkit-file-upload-button {
    padding: 0.75rem 0.75rem;
    margin: -0.75rem -0.75rem;
    -webkit-margin-end: 0.75rem;
    margin-inline-end: 0.75rem;
    color: #526175;
    background-color: #f0f2f5;
    pointer-events: none;
    border-color: inherit;
    border-style: solid;
    border-width: 0;
    border-inline-end-width: 1px;
    border-radius: 0;
    -webkit-transition: color 0.15s ease-in-out, background-color 0.15s ease-in-out, border-color 0.15s ease-in-out, -webkit-box-shadow 0.15s ease-in-out;
    transition: color 0.15s ease-in-out, background-color 0.15s ease-in-out, border-color 0.15s ease-in-out, -webkit-box-shadow 0.15s ease-in-out;
    transition: color 0.15s ease-in-out, background-color 0.15s ease-in-out, border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out;
    transition: color 0.15s ease-in-out, background-color 0.15s ease-in-out, border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out, -webkit-box-shadow 0.15s ease-in-out;
  }
  .form-control:hover:not(:disabled):not([readonly])::-webkit-file-upload-button {
    background-color: #e4e6e9;
  }
  .invalid-feedback {
    width: 100%;
    margin-top: 0.25rem;
    font-size: 0.875em;
    color: #e57373;
  }
  .invalid-field{
    width: 100%;
    margin-top: 0.25rem;
    font-size: 0.875em;
    color: #e57373;
  }
  .mb-3 {
    margin-bottom: 1rem !important;
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

@media (min-width: 768px) {
  .visualiser__right {
    display: block;
    -webkit-box-flex: 1;
    -ms-flex: 1;
    flex: 1;
    border-left: 1px solid var(--tr-border-color);
    background-image: url('../images/light-pattern-bg.jpg');
    background-repeat: repeat;
    background-size: 10px 10px;

    overflow:auto;
    
  }
  .visualiser__form {
    width: 350px;
  }
}
</style>

<style lang="scss">

.visualiser__right {
  ul{
    &.tree{
      list-style: none;
      margin: 0;
      padding: 0;
      ul {
        list-style: none;
        margin: 0;
        padding: 0;
        margin-left: 1.0em;
        &.tree li:before {
          width: 0.9em;
          height: 0.6em;
          margin-right: 0.1em;
          vertical-align: top;
          border-bottom: thin solid #000;
          border-left: thin solid #000;
          content: "";
          display: inline-block;
          position: relative;
          top: -12px;
          height: 35px;
          left: -1px;
        }
      }
      li{
        margin-left: 12px;
        border-left: thin solid #000;
        display: flex;
        margin-top: 0px;
        padding-top: 12px;
        &:last-child {
          border-left: none;
          &:before {
            border-left: thin solid #000;
          }
        }
        &:last-of-type{
          &::before{
            left: 0px!important;
          }
        }
      } 
    }
  }
}

</style>