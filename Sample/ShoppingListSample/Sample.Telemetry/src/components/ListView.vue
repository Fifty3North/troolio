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
        .visualiser__content
          ul.messages(v-if="data != null && data.length > 0")  
            li(v-for="message in data" v-on:click="selectMessage(message)")
                .row.g-2(v-if="message")
                    .col.col-md-6
                        .form-floating
                            span.subtitle {{  message.action }}
                    //- .col.col-md-2
                    //-     button.btn.btn-secondary()
                    //-         i.ms-2(data-feather="eye")
                            
          //- .data {{data}}
    .visualiser__right(ref="dragContainer" v-if="selectedMessage != null")
      EntityNode(:message="selectedMessage")
</template>
<script setup lang="ts">
import { computed, onMounted, onUpdated, ref} from "vue";
import { Guid } from 'typescript-guid'
import feather from "feather-icons";
import * as Interfaces from '../Interfaces'
import * as Enums from '../Enums'
import {metaEnv} from "../globals";
import axios, { AxiosError } from 'axios'
import {NodeAttributes} from '../nodeAttributes'
import '../scss/general.scss';
import EntityNode from '../components/EntityNode.vue'
import LeaderLine from 'vue3-leaderline';
const traceLevels = ref<any[]>([]);
const selectedTraceLevel = ref<Enums.TraceLevel>(Enums.TraceLevel.Tracing);
const canFlush = ref(false);
const messagesToShow = ref<Interfaces.MessageCorrelationEntity[]>([]);
const entitiesToShow = ref<Interfaces.Entity[]>([]);
const selectedMessage = ref<Interfaces.MessageLogListEntity>()
const flushing = ref<any>('');

const data = ref<Interfaces.MessageLogListEntity[]>([])
const linesToCreate = ref<Interfaces.LinesToCreate[]>([]);
const leaderLineList = ref<Interfaces.LeaderLineListEntity[]>([])

function selectMessage(msg:Interfaces.MessageLogListEntity){
  linesToCreate.value = [];
  leaderLineList.value.forEach(element => {
    element?.LeaderLine.remove();
  });
  leaderLineList.value = [];
  selectedMessage.value = mapAndformatNodeAndChildren(msg);

  console.log('select msg',selectedMessage.value)
  // selectedMessage.value = entitiesFromChildren(message.children);
  setTimeout(()=>{
    drawLines();

  },2000)
}
function drawLines(){
  linesToCreate.value?.forEach((edge:Interfaces.LinesToCreate) => {
    let existingLine = leaderLineList.value.find(line=> line.To == edge.To && line.From == edge.From);
    if(existingLine && existingLine.LeaderLine){
      existingLine?.LeaderLine?.position();
      return;
    }

    let from = document.getElementById(edge.From.toString());
    let to = document.getElementById(edge.To.toString());

    if(!to || !from){
      return;
    }
    // let id = edge?.Id;
    
    var tempLine = new LeaderLine(
      from,
      to, {
      startSocket: 'bottom',
      endSocket: 'left',
      endPlug: 'arrow2',
      color: '#69778C',
      size: 2,
      startSocketGravity: 40,
      endSocketGravity: 40,
      path:'grid'
      // middleLabel:'*'
    });
    let payload:Interfaces.LeaderLineListEntity = {
      Id :'id',
      To:edge.To,
      From: edge.From,
      LeaderLine: tempLine
    };
    leaderLineList.value.push(payload);

  });

}
function mapAndformatNodeAndChildren(msg:Interfaces.MessageLogListEntity){
  let toReturn:Interfaces.MessageLogListEntity = msg;
  console.log('msg',msg.message.headers.causationId)
  if(msg.message.headers.causationId != null)
  {
    linesToCreate.value.push({From:msg.message.headers.causationId, To: msg.message.headers.messageId} as Interfaces.LinesToCreate)
  }
  let attrId:any = msg.action;
  //if error
  if(msg.error || (msg.elapsed == -1 && msg.status != Enums.MessageLogStatus.Completed)){
    attrId = Enums.EntityType.Error;
  }
  let attr = NodeAttributes.find(attr=>attr.Id == attrId)
  if(attr != null){
    toReturn.entity = {
      id:msg.message.headers.messageId,
      name: Enums.MessageLogAction[msg.action].toString() ,
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
          data.value = [...data.value,...pushToList(JSON.parse(JSON.stringify(response?.data)))]
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
  console.log('data',data)
  let map:any = {}, node:Interfaces.MessageLogListEntity, roots:Interfaces.MessageLogListEntity[] = [], i;
  
  //init nodes
  for (i = 0; i < data.length; i += 1) {
    map[data[i].message.headers.messageId.toString()] = i;
    if(data[i].children){
      console.log('already has children', data[i])
    }
    data[i].children = [];
  }
  //set roots and children
  for (i = 0; i < data.length; i += 1) {
    node = data[i];
    let toPushTo:Interfaces.MessageLogListEntity[] = roots;
    
    if (node.message.headers.causationId)   {

      let parent = data[map[node.message.headers.causationId.toString()]];
      console.log('parent',node.message.headers.causationId,parent)
      toPushTo = parent.children;
    }

    let existingMessage = toPushTo.find(x=> node.message.headers.messageId == x.message.headers.messageId)

    //if duplicate message merge the two
    console.log('existingMessage',node,existingMessage)
    if(existingMessage != null && node.actor == existingMessage.actor){
      existingMessage.elapsed = node.elapsed;
      existingMessage.status = node.status;
      existingMessage.error = node.error;
      existingMessage.stackTrace = node.stackTrace;
    }else{
      toPushTo.push(node);
    }
  }
  
  //get children from data
  roots.forEach(root => {
    root.children.push(...data[map[root.message.headers.messageId.toString()]].children)
  });

  console.log('roots',roots)
  return roots;
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
    .visualiser__content{
      text-align: center;
    }
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

    overflow:auto;
    
  }
  .visualiser__form {
    width: 350px;
  }
}
</style>