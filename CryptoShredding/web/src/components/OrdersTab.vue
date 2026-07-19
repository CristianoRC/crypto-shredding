<script setup>
import { onMounted, reactive, ref } from 'vue'
import { getCustomer, listCustomers } from '../customersApi'
import { createOrder, listOrders } from '../ordersApi'

const orders = ref([])
const customerOptions = ref([])
const loading = ref(false)
const error = ref('')

const form = reactive({ customerId: '', product: '', amount: '' })

function formatCurrency(amount) {
  return Number(amount).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

function formatDate(value) {
  return new Date(value).toLocaleString('pt-BR')
}

async function loadCustomerOptions() {
  const customers = await listCustomers()
  customerOptions.value = await Promise.all(
    customers.map(async (customer) => {
      const detail = await getCustomer(customer.id)
      const label =
        detail.status === 'ok'
          ? detail.decrypted.name
          : `Cliente removido (${customer.id.slice(0, 8)})`
      return { id: customer.id, label }
    }),
  )
}

async function loadOrders() {
  orders.value = await listOrders()
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    await Promise.all([loadCustomerOptions(), loadOrders()])
  } catch (err) {
    error.value = err.message
  } finally {
    loading.value = false
  }
}

async function onSubmit() {
  if (!form.customerId || !form.product || !form.amount) return
  try {
    await createOrder({ customerId: form.customerId, product: form.product, amount: Number(form.amount) })
    form.product = ''
    form.amount = ''
    await loadOrders()
  } catch (err) {
    error.value = err.message
  }
}

onMounted(load)
</script>

<template>
  <section class="panel">
    <h2>Criar pedido</h2>
    <form class="inline" @submit.prevent="onSubmit">
      <div class="field">
        <label>Cliente</label>
        <select v-model="form.customerId" required>
          <option value="" disabled>Selecione um cliente</option>
          <option v-for="option in customerOptions" :key="option.id" :value="option.id">
            {{ option.label }}
          </option>
        </select>
      </div>
      <div class="field">
        <label>Produto</label>
        <input v-model="form.product" required />
      </div>
      <div class="field">
        <label>Valor</label>
        <input v-model="form.amount" type="number" min="0" step="0.01" required />
      </div>
      <button class="primary" type="submit">Criar pedido</button>
    </form>
  </section>

  <section class="panel">
    <h2>Pedidos</h2>
    <p v-if="error" class="badge badge-danger">{{ error }}</p>
    <p v-if="!loading && orders.length === 0" class="empty">Nenhum pedido cadastrado ainda.</p>
    <table v-else>
      <thead>
        <tr>
          <th>Produto</th>
          <th>Valor</th>
          <th>Cliente</th>
          <th>Criado em</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="order in orders" :key="order.id">
          <td>{{ order.product }}</td>
          <td>{{ formatCurrency(order.amount) }}</td>
          <td>
            <template v-if="order.customerStatus === 'ok' && order.customer">
              <div>{{ order.customer.name }}</div>
              <div class="muted">{{ order.customer.document }}</div>
            </template>
            <span v-else class="badge badge-danger">Cliente removido (dado esquecido)</span>
          </td>
          <td class="muted">{{ formatDate(order.createdAt) }}</td>
        </tr>
      </tbody>
    </table>
  </section>
</template>
