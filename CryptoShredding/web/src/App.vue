<script setup>
import { ref } from 'vue'
import CustomersTab from './components/CustomersTab.vue'
import OrdersTab from './components/OrdersTab.vue'

const tabs = [
  { id: 'customers', label: 'Clientes', icon: '👤' },
  { id: 'orders', label: 'Pedidos', icon: '📦' },
]

const activeTab = ref('customers')
</script>

<template>
  <div id="app">
    <header class="app-header">
      <span class="eyebrow">Proof of concept</span>
      <h1>Crypto-Shredding POC</h1>
      <p class="subtitle">
        Apague só a chave de um cliente e veja o dado virar irrecuperável em todos os serviços.
      </p>
    </header>

    <nav class="tabs">
      <button
        v-for="tab in tabs"
        :key="tab.id"
        :class="{ active: activeTab === tab.id }"
        type="button"
        @click="activeTab = tab.id"
      >
        <span class="tab-icon">{{ tab.icon }}</span>
        {{ tab.label }}
      </button>
    </nav>

    <CustomersTab v-show="activeTab === 'customers'" />
    <OrdersTab v-show="activeTab === 'orders'" :active="activeTab === 'orders'" />
  </div>
</template>
