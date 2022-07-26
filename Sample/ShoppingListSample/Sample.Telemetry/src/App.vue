<script setup lang="ts">
// This starter template is using Vue 3 <script setup> SFCs
// Check out https://vuejs.org/api/sfc-script-setup.html#script-setup
import HelloWorld from './components/HelloWorld.vue'
import ListView from './components/ListView.vue'
</script>

<template lang="pug">
.visualiser-wrapper
  .visualiser__header
    a.visualiser__logo(title="Trool.io")
      img(src="./images/troolio-logo.svg", alt="Trool.io")
      span.h5 Trool.io 
ListView
</template>

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
  .visualiser{
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