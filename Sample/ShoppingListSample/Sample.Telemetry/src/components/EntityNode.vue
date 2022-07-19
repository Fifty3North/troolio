<template lang="pug">

.node__container(v-if="message && message.entity") 
    .node(:class="message.entity.typeOfNode", ref="nodeElement" :id="message.message.headers.messageId")
        .node__header
            .node__icon.round-icon(:class="message.entity.iconColor")
                i(:data-feather="message.entity.iconOfNode")
            .node__title 
                .node__name {{message.entity.name}}
                .node__type {{message.entity.subTitle}}
            .node__actions
                .btn.btn-round
                    i(data-feather="more-vertical")
    .children(v-if="message.children && message.children.length > 0")
        .child(v-for="child in message.children")
            EntityNode(:message="child")
</template>
<script lang="ts">
import { onMounted } from '@vue/runtime-core';
import feather from 'feather-icons'
export default{
  name:'EntityNode'
}
</script>
<script setup lang="ts">
import * as Interfaces from '../Interfaces';
const props = defineProps({
    message: {required: true, type: Object as ()=> Interfaces.MessageLogListEntity},
});

onMounted(()=>{
    feather.replace();
});

</script>
<style lang="scss" scoped>
.children{
    margin-left:50px;
}
.node {
  $nodeParent: &;
  position: relative;
  width: 240px;
  border-radius: 0.5rem;
  border: 1px solid var(--#{tr-}border-color);
  margin-bottom: 0.5rem;
  margin-right: 0.25rem;
  cursor: grab;
  transition: box-shadow 0.2s ease-in-out, border-color 0.2s ease-in-out;
  &--active{
    opacity: 1 !important;
    visibility: visible !important;;
  }
  &__connector {
    position: absolute;
    // top: 100%;
    // left: 50%;
    width: 2rem;
    height: 2rem;
    -webkit-transform-origin: center center;
            transform-origin: center center;
    visibility: hidden;
    opacity: 0;
    z-index: 3; 

    &::before {
      content: "";
      position: absolute;
      top: 50%;
      left: 50%;
      -webkit-transform-origin: center center;
              transform-origin: center center;
      -webkit-transform: translate(-50%, -50%);
              transform: translate(-50%, -50%);
      width: 11px;
      height: 11px;
      border-color: var(--#{tr-}line-colour);
      background-color: #fff;
      border-width: 1px;
      border-style: solid;
      border-radius: 999px;
      -webkit-box-shadow: 0 0 0 10 rgba(12, 105, 244, 0.25);
              box-shadow: 0 0 0 10 rgba(12, 105, 244, 0.25);
    }
  }
  &__header {
    display: flex;
    flex-flow: row nowrap;
    padding: 0.625rem;
  }
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
  &__icon {
    margin-right: 1rem;
  }
  &__title {
    display: flex;
    flex-flow: column wrap;
    justify-content: center;
    width: 100%;
  }
  &__name {
    font-weight: 550;
    color: #0f1a2a;
    font-size: 1 * 0.775rem;
    white-space: nowrap;
  }
  &__type {
    color: var(--#{-tr}subheadings-color);
    font-size: 1 * 0.75rem;
  }
  &__actions {
    opacity: 0;
    visibility: hidden;
    pointer-events: none;
  }

  &:hover {
    box-shadow: var(--#{-tr}box-shadow);
    border-color: var(--#{tr-}line-colour);
    border-width: 2px;
    #{$nodeParent}__connector {
      visibility: visible;
      opacity: 1;
    }

    #{$nodeParent}__actions {
      opacity: 1;
      visibility: visible;
      pointer-events: auto;
    }
  }
  &:active {
    cursor: grabbing;
  }

  &--active {
    border: 1px solid var(--#{-tr}primary) !important;
    box-shadow: var(--#{-tr}box-shadow);
  }
}
.draggable{
  &__container{
    width: fit-content;  
  }
}
</style>