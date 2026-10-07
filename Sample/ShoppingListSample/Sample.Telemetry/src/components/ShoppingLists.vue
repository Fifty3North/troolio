<template>
  <section class="shoppingLists" aria-label="Your shopping lists">
    <form class="list-form" @submit.prevent="createList">
      <label for="list-title">New list</label>
      <input id="list-title" v-model="title" placeholder="Weekend groceries" maxlength="120" required />
      <button :disabled="busy || !title.trim()">Create list</button>
      <button type="button" @click="refresh" :disabled="busy">Refresh</button>
    </form>
    <form class="list-form" @submit.prevent="joinList">
      <label for="join-code">Join code</label>
      <input id="join-code" v-model="joinCode" placeholder="Code shared by the owner" required />
      <button :disabled="busy || !joinCode.trim()">Join list</button>
    </form>
    <nav class="list-form" aria-label="Shopping list pages">
      <button type="button" @click="previousPage" :disabled="busy || offset === 0">Previous</button>
      <span>Page {{ Math.floor(offset / pageSize) + 1 }}</span>
      <button type="button" @click="nextPage" :disabled="busy || lists.length < pageSize">Next</button>
    </nav>
    <p v-if="error" class="request-error" role="alert">{{ error }}</p>
    <p v-if="notice" role="status">{{ notice }}</p>
    <p v-if="!lists.length && !busy">No lists yet. Create one or join a shared list.</p>
    <div v-for="list in lists" :key="list.id" class="list-section">
      <p v-if="list.joinCode" class="join-code">Share this join code: <code>{{ list.joinCode }}</code></p>
      <ShoppingList :shopping-list="list" @add="addItem(list.id, $event)" @check="checkItem(list.id, $event)" @remove="removeItem(list.id, $event)" />
    </div>
  </section>
</template>
<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { LocalVariables } from '../Enums';
import type { ShoppingList as ListModel, AddToShoppingListEmit } from '../Interfaces';
import { http, describeError } from '../api';
import ShoppingList from './ShoppingList.vue';
const lists = ref<ListModel[]>([]);
const title = ref('');
const joinCode = ref('');
const busy = ref(false);
const error = ref('');
const notice = ref('');
const userId = localStorage.getItem(LocalVariables.UserId)!;
const offset = ref(0);
const pageSize = 50;
interface CatalogList { id: string; title: string; collaborators: string[]; joinCode?: string; items: { id: string; name: string; status: number; quantity: number }[] }
function nextPage() { offset.value += pageSize; refresh(); }
function previousPage() { offset.value = Math.max(0, offset.value - pageSize); refresh(); }
async function refresh() {
  error.value = '';
  try {
    const catalog = (await http.get<CatalogList[]>(`User/${userId}/MyShoppingLists?skip=${offset.value}&take=${pageSize}`)).data;
    lists.value = catalog.map(list => ({ ...list, items: list.items.map(item => ({
      id: item.id, description: item.name, quantity: String(item.quantity), crossedOff: item.status === 1
    })) }));
  }
  catch (failure) { error.value = describeError(failure); }
}
async function command(action: () => Promise<unknown>) {
  busy.value = true; error.value = ''; notice.value = '';
  try { await action(); notice.value = 'Command accepted. Read models may take a moment to update.'; await refresh(); }
  catch (failure) { error.value = describeError(failure); }
  finally { busy.value = false; }
}
async function createList() {
  await command(async () => { await http.post(`ShoppingList/${crypto.randomUUID()}/CreateNewList`, { title: title.value.trim() }); title.value = ''; });
}
async function joinList() {
  await command(async () => { await http.post('AllShoppingLists/JoinListUsingCode', { joinCode: joinCode.value.trim() }); joinCode.value = ''; });
}
async function addItem(listId: string, item: AddToShoppingListEmit) {
  await command(() => http.post(`ShoppingList/${listId}/AddItemToList`, { ...item, itemId: crypto.randomUUID() }));
}
async function checkItem(listId: string, itemId: string) { await command(() => http.post(`ShoppingList/${listId}/CrossItemOffList`, { itemId })); }
async function removeItem(listId: string, itemId: string) { await command(() => http.post(`ShoppingList/${listId}/RemoveItemFromList`, { itemId })); }
onMounted(refresh);
</script>
<style scoped>
.shoppingLists { max-width: 70rem; margin: auto; }
.list-form { display: flex; flex-wrap: wrap; align-items: center; gap: .65rem; margin: 1rem 0; }
.list-form input { flex: 1; min-width: 12rem; }
.list-form input, .list-form button { padding: .7rem; border: 1px solid #64677a; border-radius: .4rem; }
.list-form input { color: #eee; background: #222635; }
.list-form button { color: #151a24; background: #83e4da; }
.list-form button:disabled { opacity: .5; }
.list-section { margin: 1rem 0; }
.request-error { color: #ffb5b5; }
.join-code { color: #b9c1d8; }
</style>
