<template lang="pug">
.visualiser
    .visualiser__left

        .visualiser-wrapper
            .visualiser__header
                a.visualiser__logo(title="Trool.io")
                    img(src="../images/troolio-logo.svg", alt="Trool.io")
                    span.h5 Trool.io
            .visualiser__name
                h3 Data Visualiser
                span.subtitle Lorem ipsum dolor sit amet consectetur adipisicing elit. Deserunt, aspernatur.
        .visualiser__content
            form.visualiser__form            
                .form-group.mb-3
                    label.form-label(for="name") Trace File
                    input(type="file" ref="doc" @change="readFile()")
                //- button.btn.btn-primary(v-on:click="renderEntities")
                //- | Render
                i.ms-2(data-feather="arrow-right")
                ul.messages(v-if="messagesToShow != null && messagesToShow.length > 0")  
                    li(v-for="message in messagesToShow")
                        .row.g-2(v-if="message && message.correlationId")
                            .col.col-md-6
                                .form-floating
                                    span.subtitle {{ titleFromCorrelationId(message.correlationId) }}
                            .col.col-md-2
                                button.btn.btn-secondary()
                                    i.ms-2(data-feather="eye")
</template>
<script setup lang="ts">
import { computed, onMounted, onUpdated, ref} from "vue";
import { Guid } from 'typescript-guid'
import feather from "feather-icons";
enum Action {
  HANDLECOMMAND= "HANDLECOMMAND",
  DISPATCHEVENT = "DISPATCHEVENT",
  ORCHESTRATION = "ORCHESTRATION",
  HANDLEORCHESTRATEDCOMMAND = "HANDLEORCHESTRATEDCOMMAND",
  PROJECTION = "PROJECTION"
}
enum ActionStatus{
  STARTED = "STARTED",
  COMPLETED = "COMPLETED"
}

interface MessageLog {
    Action:any,
    Status:any,
    Item:any,
    Id:any,
    Version:any,
    Silo:any,
    MessageType:any,
    TransactionId:any,
    CorrelationId:any,
    CausationId?:any,
    MessageId:any,
    UserId:any,
    DeviceId:any,
    MessageBody:any,
    Elapsed:any
}
interface MessageLogListEntity extends MessageLog{
    children: MessageLogListEntity[]
}
interface MessageCorrelationEntity {
    correlationId: Guid|string
    children : MessageLogListEntity[]
}
const file = ref(null);
const content = ref([]);

const doc = ref(null);
const messagesToShow = ref<MessageCorrelationEntity[]>([]);
const entitiesToShow = ref([]);
    function readFile() {
      file.value = (doc.value as any).files[0];
      const reader = new FileReader();
      if ((file.value  as any).name.includes(".trace")) {
        reader.onload = (res) => {
          console.log('loaded',res)
          formatData((res.target as any).result);
        };
        reader.onerror = (err) => console.log(err);
        reader.readAsText((file.value as any));
      } else {
        reader.onload = (res) => {
          console.log((res.target as any).result);
        };
        reader.onerror = (err) => console.log(err);
        reader.readAsArrayBuffer(file.value  as any);
      }
      console.log('content',content.value)
    }
    //todo set delimiter
    function formatData(data:any){
        let parsed = JSON.parse(JSON.stringify(data))
        let split = parsed?.split(/\r?\n/).map((temp:any)=>{
        let  _split =temp.split([';']);
        let toReturn:MessageLog = {
            Action:_split[0],
            Status:_split[1],
            Item:_split[2],
            Id:_split[3],
            Version:_split[4],
            Silo:_split[5],
            MessageType:_split[6],
            TransactionId:_split[7],
            CorrelationId:_split[8],
            CausationId:_split[9],
            MessageId:_split[10],
            UserId:_split[11],
            DeviceId:_split[12],
            MessageBody:_split[13],
            Elapsed:_split[14],
          } 
          return toReturn;
        })

          let test:MessageCorrelationEntity[] = list_to_tree(split);
        console.log('test',test)
        console.log('split ',split[0])
        let toPush = split;
        if(split.length > 100){
          toPush = split.slice(0,99);
        }
        console.log('toPush',toPush)
        messagesToShow.value = test;

        content.value = split;
        console.log('messages',messagesToShow.value)
        let messages: MessageLog[] = <MessageLog[]>split;
    }

    function list_to_tree(list:MessageLogListEntity[]):MessageCorrelationEntity[] {
      let newList:MessageCorrelationEntity[] = [];
      let map:any = {}, node:MessageLogListEntity, roots = [], i;
  
      for (i = 0; i < list.length; i += 1) {
        map[list[i].MessageId] = i;
        list[i].children = [];
      }
      for (i = 0; i < list.length; i += 1) {
        node = list[i];
        if (node.CausationId && node.CausationId != 'CausationId')   {
          list[map[node.CausationId]].children.push(node);
        } else {
          roots.push(node);
        }
      }

      roots.forEach((root:MessageLogListEntity) =>{
        let correlationList = newList.find( li => li.correlationId == root.CorrelationId)
        if(!correlationList){
          correlationList = {
            correlationId:root.CorrelationId,
            children: [],
          };
          newList.push(correlationList);
        }
        newList?.find((li:MessageCorrelationEntity)=>li.correlationId == root.CorrelationId)?.children.push(root);
      });


      return newList;
    }
    const titleFromCorrelationId = computed(()=>{
      return function(correlationId:Guid|string){
        const details = (messagesToShow.value as MessageCorrelationEntity[]).find(li => li.correlationId == correlationId);
        let firstCommand = details?.children.find(x=>x.Action == Action.HANDLECOMMAND && x.Status == ActionStatus.STARTED);
        return firstCommand?.MessageType;

      }
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