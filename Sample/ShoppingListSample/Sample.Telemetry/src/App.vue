<template lang="pug">
.visualiser-wrapper
  .visualiser__header
    a.visualiser__logo(title="Trool.io")
      img(src="./images/troolio-logo.svg", alt="Trool.io")
      span.h5 Trool.io 
  .tabs
    .tab(:class="selectedClass(Tabs.Functions)" v-on:click="selectTab(Tabs.Functions)") 
      h6 Functions
    .tab(:class="selectedClass(Tabs.Debug)" v-on:click="selectTab(Tabs.Debug)") 
      h6 Debug
.visualiser
  .visualiser__left
    .visualiser__content(v-show="selectedTab === Tabs.Debug")    
      ListView(@selectMsg="selectMessage")

    .visualiser__content(v-show="selectedTab === Tabs.Functions")
      Functions
  .visualiser__right
    MessageVisualiser(:message="selectedMessage")
    //- h6 {{selectedMessage}}/
</template>
<script setup lang="ts">
import {computed, ref} from 'vue'
import ListView from './components/ListView.vue'
import MessageVisualiser from './components/MessageVisualiser.vue'
import Functions from './components/Functions.vue'
import * as Interfaces from './Interfaces';
import './scss/general.scss';

enum Tabs{
  Debug=2,
  Functions=3
}
const selectedMessage = ref<Interfaces.MessageLogListEntity>();
const selectedTab = ref(Tabs.Functions);
const selectedClass = computed(()=>{
  return function(tab:Tabs){
    let toReturn = "inactive";
    if(selectedTab.value == tab){
      toReturn = 'active';
    }
    return toReturn;
  }
})

function selectTab(tab:Tabs){
  selectedTab.value = tab;
}
function selectMessage(msg:Interfaces.MessageLogListEntity){
  console.log('selected', msg)
  selectedMessage.value = msg;
}
</script>

<style scoped>
.logo {
  height: 6em;
  padding: 1.5em;
  will-change: filter;
}
.logo:hover {
  filter: drop-shadow(0 0 2em #646cffaa);
}
.logo.vue:hover {
  filter: drop-shadow(0 0 2em #42b883aa);
}
</style>
<style lang="scss" scoped>
  .tabs{
    display:flex;
    position: relative;
    .tab{
      &.active{
        text-decoration: underline;
      }
    }
  }
  .visualiser{
    &__content{
      
        .active{
          display:block;
        }
        .inactive{
          display:none;
        }

      

    }
    &-wrapper {
      max-width: 600px;
      margin: 0 auto;
      position: absolute;
      top: 2rem;
      left: 2rem;
    }
    &__header {
      width: 100%;
      margin-bottom: 1rem;
    }
    &__logo {

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
  }
</style>