<script setup>
import { ref } from 'vue'
import CustomersTab from './components/CustomersTab.vue'
import OrdersTab from './components/OrdersTab.vue'

const activeTab = ref('customers')
</script>

<template>
  <div id="app">
    <h1>Crypto-Shredding POC</h1>
    <p class="subtitle">
      Apague só a chave de um cliente e veja o dado virar irrecuperável em todos os serviços.
    </p>

    <nav class="tabs">
      <button :class="{ active: activeTab === 'customers' }" @click="activeTab = 'customers'">
        Clientes
      </button>
      <button :class="{ active: activeTab === 'orders' }" @click="activeTab = 'orders'">
        Pedidos
      </button>
    </nav>

    <CustomersTab v-show="activeTab === 'customers'" />
    <OrdersTab v-show="activeTab === 'orders'" />
  </div>
</template>
