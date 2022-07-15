<template lang="pug">
.visualiser
    .visualiser__left

        .visualiser-wrapper
            .visualiser__header
                a.visualiser__logo(title="Trool.io")
                    img(src="../images/troolio-logo.svg", alt="Trool.io")
                    span.h5 Trool.io 
                h6 flush status = {{flushing}}
                .disabled(v-if="!canFlush")
                  select.form-select( aria-label="Field" v-model="selectedTraceLevel")
                    option(v-for="level in traceLevels" :value="level") {{level}}
                  button.btn.btn-primary(v-on:click="enableLogging")
                    | Enable Logging  
                .enabled(v-else)
                  h6  {{selectedTraceLevel}}
                  button.btn.btn-primary(v-on:click="disableLogging" style="margin-right:10px;  ")
                    | Disable Logging

                  button.btn.btn-primary(v-on:click="flush")
                    span(v-if="!flushing")
                      | Flush
                    span(v-else)
                      | Cancel
            //- .visualiser__name
            //-     h3 Data Visualiser
            //-     span.subtitle Lorem ipsum dolor sit amet consectetur adipisicing elit. Deserunt, aspernatur.
        .visualiser__content
          //- .data {{data}}
            //- form.visualiser__form            
            //-     .form-group.mb-3
            //-         label.form-label(for="name") Trace File
            //-         input(type="file" ref="doc" @change="readFile()")
            //-     i.ms-2(data-feather="arrow-right")
          //- .data {{data}}
          ul.messages(v-if="data != null && data.length > 0")  
            li(v-for="message in data" )
                .row.g-2(v-if="message && message.correlationId")
                    .col.col-md-6
                        .form-floating
                            span.subtitle {{ titleFromMessage(message) }}
                    .col.col-md-2
                        //- button.btn.btn-secondary()
                        //-     i.ms-2(data-feather="eye")
</template>
<script setup lang="ts">
import { computed, onMounted, onUpdated, ref} from "vue";
import { Guid } from 'typescript-guid'
import feather from "feather-icons";
import * as Interfaces from '../Interfaces'
import * as Enums from '../Enums'
import {metaEnv} from "../globals";
import axios, { AxiosError } from 'axios'

const traceLevels = ref<any[]>([]);
const selectedTraceLevel = ref<Enums.TraceLevel>(Enums.TraceLevel.Tracing);
const canFlush = ref(false);
const messagesToShow = ref<Interfaces.MessageCorrelationEntity[]>([]);
const entitiesToShow = ref([]);

const flushing = ref<any>('');

const data = ref<Interfaces.MessageCorrelationEntity[]>([])

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
  axios.post(`${metaEnv.VITE_API_URL}Telemetry/disablelogging`,selectedTraceLevel.value).then((response: any) => {
      if(response && response.status === 200){
        canFlush.value = false;
      }
  },(error) => {
      console.log('error',error)
  })
}
function flush(){
  if(!flushing.value && canFlush.value){
    flushing.value = setInterval(()=>{
      axios.get(`${metaEnv.VITE_API_URL}Telemetry/flush`).then((response: any) => {
        if(response && response.status === 200){
          console.log('response',response)
          data.value = [...data.value,...pushToList(response?.data)]
        }
      },(error) => {
        console.log('error',error)
      })
    },5000);
  }else{
    clearInterval(flushing.value);
    flushing.value = null;
  }
}

function pushToList(data:Interfaces.MessageLogListEntity[]){
  let newList:Interfaces.MessageCorrelationEntity[] = [];

  let map:any = {}, node:Interfaces.MessageLogListEntity, roots = [], i;
  
  for (i = 0; i < data.length; i += 1) {
    map[data[i].message.headers.messageId.toString()] = i;
    data[i].children = [];
  }
  for (i = 0; i < data.length; i += 1) {
    node = data[i];
    if (node.message.headers.causationId)   {
      data[map[node.message.headers.causationId.toString()]].children.push(node);
    } else {
      roots.push(node);
    }
  }

  roots.forEach((root:Interfaces.MessageLogListEntity) =>{
    let correlationList = newList.find( li => li.correlationId == root.message.headers.correlationId)
    if(!correlationList){
      correlationList = {
        correlationId:root.message.headers.correlationId,
        children: [],
      };
      newList.push(correlationList);
    }
    newList?.find((li:Interfaces.MessageCorrelationEntity)=>li.correlationId == root.message.headers.correlationId)?.children.push(root);
  });


  return newList;
}

function titleFromMessage(message:Interfaces.MessageCorrelationEntity){
  let toReturn:any = 'Not found';
  let firstCommand = message?.children.find(x=>x.action == Enums.MessageLogAction.HandleCommand && x.status == Enums.MessageLogStatus.Started);
  if(firstCommand){
    toReturn = Enums.MessageLogAction[firstCommand?.action].toString();
  }
  return toReturn;
}

onMounted(()=>{
  //getting trace levels from enum
  traceLevels.value =  Object.keys(Enums.TraceLevel).filter(k => isNaN(Number(k)));
});
</script>

<style lang="scss" scoped>
.messages{
  overflow-x:auto;
  li{
    -webkit-line-clamp: 1;
    -webkit-box-orient: vertical;
    display: -webkit-box;
    margin-right: 5px;
    word-break: break-all;
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
  }
  &__right {
    display: flex !important;
    flex-direction: column;
    justify-content: center;
    align-items: center;
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
    display: block;
    margin-bottom: 1rem;
    color: var(--tr-subheadings-color);
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
  }
  .visualiser__form {
    width: 350px;
  }
}
</style>