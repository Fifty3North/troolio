<template lang="pug">
.container
  .row
    .col-md-12
      .card.card-white    
        .title.truncated {{shoppingList.title}}
        .card-body
          .form
            input.form-control.add-task.me-2(type='text' 
              placeholder='Item Name' 
              v-model="itemName" 
              @keydown.enter="add" )
            input.form-control.add-task.me-2.quantity(type='text' 
              placeholder='Quantity' 
              v-model="quantity" 
              @keydown.enter="add" )
            button.btn.btn-primary(:disabled="!itemName" v-on:click="add") Add
          .todo-list
            .todo-item(v-for="item in shoppingList?.items" 
              :class="{'crossed':item.crossedOff}"
              :key="item.id" )
              .checker 
                span
                  input(type='checkbox' 
                    :v-model="!item.crossedOff" 
                    v-on:click="emit('check',item.id)" 
                    :disabled="item.crossedOff" )
              .truncated(:title="item.description")
                span {{item.description}} 
              .quantity.me-2
                span  x {{item.quantity}}
              button.no-style(v-on:click="emit('remove',item.id)")
                i(data-feather="trash")
</template>
<script lang="ts">
export default {
  name:'ShoppingList',
}
</script>

<script lang="ts" setup>
import {ref, onUpdated} from 'vue'
import * as Interfaces from '../Interfaces';
import feather from 'feather-icons';
const props = defineProps({
    shoppingList: {required: true, type: Object as ()=> Interfaces.ShoppingList},
});
const emit= defineEmits(['add','check','remove']);

const itemName = ref('')
const quantityDefault = '1';
const quantity = ref(quantityDefault)
function add(){
  if(itemName.value.trim() && quantity.value.trim()){
    let payload:Interfaces.AddToShoppingListEmit = {
      description:itemName.value,
      quantity:quantity.value
    }
    emit('add',payload)
    itemName.value='';
    quantity.value = quantityDefault
  }
}
onUpdated(()=>{
  feather.replace();
})
</script>
<style lang="scss" scoped>
.no-style{
  border: none;
  background: none;
  color: inherit;
  font-size: inherit;
}

.card{
  .title{
    font-weight: bold;
    font-size: 1.5rem;
  }
  &-body{
    .form{
      display: flex;
      .quantity{
        width:35%
      }
    }
  }
}
.todo-item{
  text-align: start;
  display: flex;
  &.crossed{
    span{
      text-decoration: line-through;
    }
  }
  .quantity{
    margin-left: auto;
  }
  .checker{
    margin-right: 10px;
  }
  span{
    vertical-align: middle;
    max-width: 50%;
  }
}
</style>
<style scoped>
/* body{
    margin-top:20px;
    background: #f8f8f8;
} */

.todo-nav {
    margin-top: 10px
}

.todo-list {
    margin: 10px 0
}

.todo-list .todo-item {
    padding: 15px;
    margin: 5px 0;
    border-radius: 0;
    background: #f7f7f7
}

.todo-list.only-active .todo-item.complete {
    display: none
}

.todo-list.only-active .todo-item:not(.complete) {
    display: block
}

.todo-list.only-complete .todo-item:not(.complete) {
    display: none
}

.todo-list.only-complete .todo-item.complete {
    display: block
}

.todo-list .todo-item.complete span {
    text-decoration: line-through
}

.remove-todo-item {
    color: #ccc;
    visibility: hidden
}

.remove-todo-item:hover {
    color: #5f5f5f
}

.todo-item:hover .remove-todo-item {
    visibility: visible
}

div.checker {
    width: 18px;
    height: 18px
}

div.checker input,
div.checker span {
    width: 18px;
    height: 18px
}

div.checker span {
    display: -moz-inline-box;
    display: inline-block;
    zoom: 1;
    text-align: center;
    background-position: 0 -260px;
}

div.checker, div.checker input, div.checker span {
    width: 19px;
    height: 19px;
}

div.checker, div.radio, div.uploader {
    position: relative;
}

div.button, div.button *, div.checker, div.checker *, div.radio, div.radio *, div.selector, div.selector *, div.uploader, div.uploader * {
    margin: 0;
    padding: 0;
}

div.button, div.checker, div.radio, div.selector, div.uploader {
    display: -moz-inline-box;
    display: inline-block;
    zoom: 1;
    vertical-align: middle;
}

.card {
    padding: 25px;
    margin-bottom: 20px;
    border: initial;
    background: #fff;
    border-radius: calc(.15rem - 1px);
    box-shadow: 0 1px 15px rgba(0,0,0,0.04), 0 1px 6px rgba(0,0,0,45%);
    margin-top: 5px;
}
</style>