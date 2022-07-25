<template lang="pug">
.node__container(v-if="message && message.entity" :class="{'no-children': message.children == null || message.children.length == 0}") 
  .node(:class="message.entity.typeOfNode", ref="nodeElement" :id="message.message.headers.messageId")
      .node__header
          .node__icon.round-icon(:class="message.entity.iconColor")
              i(:data-feather="message.entity.iconOfNode")
          .node__title 
              .node__name {{message.entity.name}}
              .node__type {{message.entity.subTitle}}
          //- .node__actions
              //- .btn.btn-round
              //-     i(data-feather="more-vertical")
          
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
import * as Interfaces from '../Interfaces';

const props = defineProps({
    message: {required: true, type: Object as ()=> Interfaces.MessageLogListEntity},
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
  // &__actions {
  //   opacity: 0;
  //   visibility: hidden;
  //   pointer-events: none;
  // }

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