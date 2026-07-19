<script setup>
import { onMounted, reactive, ref, watch } from 'vue'
import { getCustomer, listCustomers } from '../customersApi'
import { createOrder, listOrders } from '../ordersApi'

const PRODUCT_PRESETS = [
  { label: 'Notebook Ultra 14"', price: 4899.9 },
  { label: 'Mouse sem fio', price: 79.9 },
  { label: 'Teclado mecânico', price: 349.9 },
  { label: 'Monitor 27" 4K', price: 1899.9 },
  { label: 'Cadeira ergonômica', price: 1299 },
  { label: 'Fone de ouvido Bluetooth', price: 259.9 },
  { label: 'Webcam Full HD', price: 199.9 },
  { label: 'SSD NVMe 1TB', price: 449.9 },
]
const CUSTOM_PRODUCT = '__custom__'
const AMOUNT_SUGGESTIONS = [49.9, 99.9, 149.9, 299.9, 499.9, 999.9, 1999.9]

const props = defineProps({ active: { type: Boolean, default: false } })

const orders = ref([])
const customerOptions = ref([])
const loading = ref(false)
const error = ref('')

const productChoice = ref('')
const form = reactive({ customerId: '', product: '', amount: '' })

watch(productChoice, (value) => {
  if (value === CUSTOM_PRODUCT) {
    form.product = ''
    form.amount = ''
    return
  }
  const preset = PRODUCT_PRESETS.find((p) => p.label === value)
  if (preset) {
    form.product = preset.label
    form.amount = preset.price
  }
})

function formatCurrency(amount) {
  return Number(amount).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

function formatDate(value) {
  return new Date(value).toLocaleString('pt-BR')
}

async function loadCustomerOptions() {
  const customers = await listCustomers()
  const withDetail = await Promise.all(
    customers.map(async (customer) => ({ customer, detail: await getCustomer(customer.id) })),
  )
  customerOptions.value = withDetail
    .filter(({ detail }) => detail.status === 'ok')
    .map(({ customer, detail }) => ({ id: customer.id, label: detail.decrypted.name }))
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
    productChoice.value = ''
    form.product = ''
    form.amount = ''
    await loadOrders()
  } catch (err) {
    error.value = err.message
  }
}

onMounted(load)

watch(
  () => props.active,
  (isActive) => {
    if (isActive) load()
  },
)
</script>

<template>
<div class="tab-panel">
  <section class="panel">
    <h2>
      <span class="panel-icon">➕</span>
      Criar pedido
    </h2>
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
        <select v-model="productChoice" required>
          <option value="" disabled>Selecione um produto</option>
          <option v-for="preset in PRODUCT_PRESETS" :key="preset.label" :value="preset.label">
            {{ preset.label }} — {{ formatCurrency(preset.price) }}
          </option>
          <option :value="CUSTOM_PRODUCT">Outro produto…</option>
        </select>
      </div>
      <div class="field" v-if="productChoice === CUSTOM_PRODUCT">
        <label>Nome do produto</label>
        <input v-model="form.product" required placeholder="Digite o nome do produto" />
      </div>
      <div class="field">
        <label>Valor</label>
        <input
          v-model="form.amount"
          type="number"
          min="0"
          step="0.01"
          required
          :readonly="productChoice !== CUSTOM_PRODUCT"
          :list="productChoice === CUSTOM_PRODUCT ? 'amount-suggestions' : null"
        />
        <datalist id="amount-suggestions">
          <option v-for="value in AMOUNT_SUGGESTIONS" :key="value" :value="value" />
        </datalist>
      </div>
      <button class="primary" type="submit">Criar pedido</button>
    </form>
  </section>

  <section class="panel">
    <div class="panel-header">
      <h2>
        <span class="panel-icon">📦</span>
        Pedidos
      </h2>
      <span v-if="orders.length" class="count-badge">{{ orders.length }}</span>
    </div>
    <p v-if="error" class="badge badge-danger">{{ error }}</p>
    <div v-if="loading" class="loading"><span class="spinner"></span>Carregando pedidos…</div>
    <div v-else-if="orders.length === 0" class="empty">
      <span class="empty-icon">📦</span>
      Nenhum pedido cadastrado ainda.
    </div>
    <div v-else class="table-wrap">
      <table>
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
              <template v-else>
                <span class="badge badge-danger">Cliente removido (dado esquecido)</span>
                <div class="cell-mono">{{ order.customerId.slice(0, 8) }}</div>
              </template>
            </td>
            <td class="muted">{{ formatDate(order.createdAt) }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</div>
</template>
