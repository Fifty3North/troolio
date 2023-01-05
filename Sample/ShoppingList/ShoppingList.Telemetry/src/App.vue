<template lang="pug">
.visualiser-wrapper
  .visualiser__header
    a.visualiser__logo(title="Trool.io")
      img(src="./images/troolio-logo.svg", alt="Trool.io")
      span.h5 Shopping List
  .tabs
    .tab.me-2(:class="selectedClass(Tabs.ShoppingLists)" v-on:click="selectTab(Tabs.ShoppingLists)") 
      h6 Shopping Lists
    .tab(:class="selectedClass(Tabs.Debug)" v-on:click="selectTab(Tabs.Debug)") 
      h6 Debug
.visualiser
  .visualiser__left
    .visualiser__content(v-show="selectedTab === Tabs.Debug")    
      Flush(@selectMsg="selectMessage")

    .visualiser__content(v-show="selectedTab === Tabs.ShoppingLists")
      ShoppingLists
  .visualiser__right
    MessageVisualiser(:message="selectedMessage")
</template>
<script setup lang="ts">
import {computed, onMounted, ref} from 'vue'
import Flush from './components/Flush.vue'
import MessageVisualiser from './components/MessageVisualiser.vue'
import ShoppingLists from './components/ShoppingLists.vue'
import * as Interfaces from './Interfaces';
import './scss/general.scss';
import {Guid} from 'typescript-guid'
import {LocalVariables, Tabs} from './Enums'

const selectedMessage = ref<Interfaces.MessageLogListEntity>();
const selectedTab = ref(Tabs.ShoppingLists);
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
onMounted(()=>{
  if(localStorage.getItem(LocalVariables.UserId) == null){
    let userId = Guid.create().toString();
    localStorage.setItem(LocalVariables.UserId, userId);
  }
  if(localStorage.getItem(LocalVariables.DeviceId) == null){
    let deviceId = Guid.create().toString();
    localStorage.setItem(LocalVariables.DeviceId, deviceId);
  }
});
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
    &__left{
      padding-top: 100px;
    }
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
      top: 1rem;
      left: 1rem;
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