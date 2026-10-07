<template>
  <div class="visualiser-wrapper">
    <header class="visualiser__header">
      <a class="visualiser__logo" href="https://fifty3north.github.io/troolio-docs/" title="Troolio documentation">
        <img src="./images/troolio-logo.svg" alt="Troolio" />
        <span class="h5">Shopping List</span>
      </a>
      <label class="demo-user">Demo account
        <select v-model="selectedUserId" @change="changeUser" :disabled="users.length === 0">
          <option v-for="user in users" :key="user.id" :value="user.id">{{ user.name }}</option>
        </select>
      </label>
      <p class="demo-note">Local demonstration accounts. Use your application's identity provider for deployment.</p>
      <p v-if="startupError" class="request-error" role="alert">{{ startupError }}</p>
      <nav class="tabs" aria-label="Sample views">
        <button :aria-pressed="selectedTab === Tabs.ShoppingLists" @click="selectedTab = Tabs.ShoppingLists">Shopping Lists</button>
        <button :aria-pressed="selectedTab === Tabs.Debug" @click="selectedTab = Tabs.Debug">Tracing</button>
      </nav>
    </header>
  </div>
  <main v-if="selectedUserId && !startupError" class="visualiser" :key="selectedUserId">
    <section class="visualiser__left">
      <div class="visualiser__content" v-show="selectedTab === Tabs.Debug"><Flush @select-msg="selectMessage" /></div>
      <div class="visualiser__content" v-show="selectedTab === Tabs.ShoppingLists"><ShoppingLists /></div>
    </section>
    <section class="visualiser__right" v-if="selectedTab === Tabs.Debug"><MessageVisualiser :message="selectedMessage" /></section>
  </main>
</template>
<script setup lang="ts">
import { onMounted, ref } from 'vue';
import Flush from './components/Flush.vue';
import MessageVisualiser from './components/MessageVisualiser.vue';
import ShoppingLists from './components/ShoppingLists.vue';
import type { MessageLogListEntity } from './Interfaces';
import { LocalVariables, Tabs } from './Enums';
import { http, selectDemoUser, describeError, type DemoUser } from './api';
import './scss/general.scss';
import './style.css';

const users = ref<DemoUser[]>([]);
const selectedUserId = ref('');
const startupError = ref('');
const selectedMessage = ref<MessageLogListEntity>();
const selectedTab = ref(Tabs.ShoppingLists);
function selectMessage(message: MessageLogListEntity) { selectedMessage.value = message; }
function changeUser() {
  const user = users.value.find(user => user.id === selectedUserId.value);
  if (user) selectDemoUser(user);
  selectedMessage.value = undefined;
}
onMounted(async () => {
  try {
    users.value = (await http.get<DemoUser[]>('demo/users')).data;
    if (!users.value.length) throw new Error('No demo accounts are configured.');
    selectedUserId.value = users.value.find(user => user.id === localStorage.getItem(LocalVariables.UserId))?.id || users.value[0].id;
    changeUser();
  } catch (error) { startupError.value = describeError(error); }
});
</script>
<style scoped>
.visualiser-wrapper { position: relative; padding: 1.25rem; width: 100%; }
.visualiser__header { max-width: 70rem; margin: auto; }
.visualiser__logo { display: flex; align-items: center; gap: .7rem; color: inherit; text-decoration: none; }
.visualiser__logo img { width: 2rem; }
.demo-user { display: flex; align-items: center; gap: .75rem; margin-top: 1rem; }
.demo-user select, .tabs button { color: #eee; background: #202431; border: 1px solid #53566a; border-radius: .4rem; padding: .65rem .85rem; }
.demo-note { color: #b5b8c6; font-size: .85rem; margin-top: .65rem; }
.tabs { display: flex; gap: .5rem; }
.tabs button[aria-pressed="true"] { border-color: #83e4da; color: #83e4da; }
.request-error { color: #ffb5b5; }
.visualiser { display: flex; gap: 1rem; padding: 0 1rem 1rem; min-height: 0; height: auto; }
.visualiser__left { flex: 1; min-width: 0; padding-top: 0; }
.visualiser__right { flex: 1; min-width: 0; }
@media (max-width: 760px) { .visualiser { flex-direction: column; } }
</style>
