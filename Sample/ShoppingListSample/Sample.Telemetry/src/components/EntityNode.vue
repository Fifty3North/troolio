<template lang="pug">
.node__container(v-if="message && message.entity" :class="{'no-children': message.children == null || message.children.length == 0}") 
  .node(:class="message.entity.typeOfNode", ref="nodeElement" :id="message.message.headers.messageId")
      .node__header
          .node__icon.round-icon(:class="message.entity.iconColor")
              i(:data-feather="message.entity.iconOfNode")
          .node__title 
              .node__top
                .node__name {{message.entity.name}} 
                .node__elapsed {{message.elapsed}} ms
              .node__type {{message.entity.subTitle}}
          //- .node__actions
              //- .btn.btn-round
              //-     i(data-feather="more-vertical")
      .node__content(v-if="(payload && payload.length > 0) && nodeContentExpanded")
        ul.node__attributes
          li(v-for="property in payload")
            strong {{property.name}}
            span {{property.value}}    
  ul.tree(v-if="message.children && message.children.length > 0")
    li(v-for="child in message.children")
      EntityNode(:message="child")
</template>
<script lang="ts">
export default{
  name:'EntityNode'
}
</script>
<script setup lang="ts">
import {computed, ref} from 'vue'
import * as Interfaces from '../Interfaces';

const nodeContentExpanded = ref(true);
const props = defineProps({
    message: {required: true, type: Object as ()=> Interfaces.MessageLogListEntity},
});
function mapPayload(key:any,value:any){
  if(!key){
    return;
  }
  let toPush:Interfaces.PayloadValue = {
    name: key,
    value:value
  }
  _payload.push(toPush)
}
let _payload:Interfaces.PayloadValue[] = [];

const payload = computed(()=>{
  _payload = [];
  if( props.message?.message?.payload != null ){
    JSON.parse(JSON.stringify(props.message.message.payload),mapPayload)
  }
  return _payload;
});

</script>
<style lang="scss" scoped>
.node {
  $nodeParent: &;
  position: relative;
  width: 240px;
  border-radius: 0.5rem;
  border: 1px solid var(--#{tr-}border-color);
  margin-right: 0.25rem;
  margin-bottom: unset;
  cursor: grab;
  transition: box-shadow 0.2s ease-in-out, border-color 0.2s ease-in-out;
  &--active{
    opacity: 1 !important;
    visibility: visible !important;;
  }
  &__header {
    display: flex;
    flex-flow: row nowrap;
    padding: 0.625rem;
  }
  &__icon {
    margin-right: 1rem;
  }
  &__title {
    display: flex;
    flex-flow: column wrap;
    justify-content: center;
    width: 100%;
  }
  &__name,
  &__elapsed {
    font-size: 1 * 0.775rem;
    white-space: nowrap;
  }
  &__name{
    font-weight: 550;
    color: #0f1a2a;
  }
  &__type {
    color: var(--#{-tr}subheadings-color);
    font-size: 1 * 0.75rem;
  }
  &__top{
    display:flex;
  }
  &__elapsed{
    margin-left: auto;
  }
  // &__actions {
  //   opacity: 0;
  //   visibility: hidden;
  //   pointer-events: none;
  // }
  &__content {
    padding: 0.625rem;
    border-top-width: 1px;
    border-top-style: solid;
    border-top-color: var(--#{-tr}border-color);
    border-bottom-left-radius: 0.5rem;
    border-bottom-right-radius: 0.5rem;
    font-size: 1 * 0.8rem;
    background: #FFFFFF;
    text-align: center;
  }
  &:hover {
    box-shadow: var(--#{-tr}box-shadow);
    border-color: var(--#{tr-}line-colour);
    border-width: 2px;

    // #{$nodeParent}__actions {
    //   opacity: 1;
    //   visibility: visible;
    //   pointer-events: auto;
    // }
  }
  &:active {
    cursor: grabbing;
  }

  &--active {
    border: 1px solid var(--#{-tr}primary) !important;
    box-shadow: var(--#{-tr}box-shadow);
  }
}
</style>