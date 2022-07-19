<template lang="pug">

.node__container(v-if="message && message.entity") 
    .node(:class="message.entity.typeOfNode", ref="nodeElement")
        .node__header
            .node__icon.round-icon(:class="message.entity.iconColor")
            i(:data-feather="iconOfNode")
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
<style>
.child{
    margin-left:50px;
}
</style>