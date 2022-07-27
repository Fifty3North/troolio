<template lang="pug">
.canvas__view-tools
  .btn-group.btn-group-sm
      button#nodeSizeToggler.btn.btn-white(type="button" data-bs-toggle="tooltip" data-bs-placement="bottom" title="Minimize nodes" v-on:click="toggleNodeContent")
          i(data-feather="minimize-2")
ul.tree.root(v-if="mappedMessage != null")
    li
        EntityNode(:message="mappedMessage" :showNodeContent="showNodeContent")
</template>

<script lang="ts">
export default {
    name:'MessageVisualiser'
}
</script>
<script lang="ts" setup>
import EntityNode from './EntityNode.vue'
import * as Interfaces from '../Interfaces'
import * as Enums from '../Enums';
import {NodeAttributes} from '../nodeAttributes';
import {computed, onMounted, ref} from 'vue'
const props = defineProps({
    message: {required: false, type: Object as ()=> Interfaces.MessageLogListEntity},
});

const mappedMessage = computed(()=>{
    if(props.message != null){
        return  mapAndformatNodeAndChildren(props.message);
    }
});
const showNodeContent = ref(false);
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

function toggleNodeContent(){
    showNodeContent.value = !showNodeContent.value
}

</script>
<style>

</style>