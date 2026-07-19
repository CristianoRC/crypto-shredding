<script setup>
import { onMounted, reactive, ref } from 'vue'
import { createCustomer, getCustomer, listCustomers, shredCustomerKey } from '../customersApi'

const customers = ref([])
const decryptedById = reactive({})
const loading = ref(false)
const error = ref('')

const form = reactive({ name: '', document: '', address: '' })

const FAKE_FIRST_NAMES = [
  'Ana', 'Bruno', 'Carla', 'Diego', 'Elisa', 'Fábio', 'Gabriela', 'Henrique',
  'Isabela', 'João', 'Larissa', 'Marcos', 'Natália', 'Otávio', 'Patrícia',
  'Rafael', 'Sofia', 'Thiago', 'Vanessa', 'Wesley',
]
const FAKE_LAST_NAMES = [
  'Silva', 'Santos', 'Oliveira', 'Souza', 'Rodrigues', 'Ferreira', 'Almeida',
  'Pereira', 'Lima', 'Gomes', 'Costa', 'Ribeiro', 'Martins', 'Carvalho', 'Barbosa',
]
const FAKE_STREETS = ['Rua das Flores', 'Av. Paulista', 'Rua Sete de Setembro', 'Alameda Santos', 'Rua XV de Novembro']
const FAKE_CITIES = ['São Paulo, SP', 'Rio de Janeiro, RJ', 'Belo Horizonte, MG', 'Curitiba, PR', 'Porto Alegre, RS']

function randomItem(list) {
  return list[Math.floor(Math.random() * list.length)]
}

function randomName() {
  return `${randomItem(FAKE_FIRST_NAMES)} ${randomItem(FAKE_LAST_NAMES)}`
}

function randomDocument() {
  const digits = Array.from({ length: 11 }, () => Math.floor(Math.random() * 10))
  return `${digits.slice(0, 3).join('')}.${digits.slice(3, 6).join('')}.${digits.slice(6, 9).join('')}-${digits.slice(9, 11).join('')}`
}

function randomAddress() {
  const number = Math.floor(Math.random() * 2000) + 1
  return `${randomItem(FAKE_STREETS)}, ${number} - ${randomItem(FAKE_CITIES)}`
}

function fillFakeData() {
  form.name = randomName()
  form.document = randomDocument()
  form.address = randomAddress()
}

function truncate(value, length = 18) {
  if (!value) return ''
  return value.length > length ? `${value.slice(0, length)}…` : value
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    customers.value = await listCustomers()
    await Promise.all(
      customers.value.map(async (customer) => {
        decryptedById[customer.id] = await getCustomer(customer.id)
      }),
    )
  } catch (err) {
    error.value = err.message
  } finally {
    loading.value = false
  }
}

async function onSubmit() {
  if (!form.name || !form.document || !form.address) return
  try {
    await createCustomer({ ...form })
    form.name = ''
    form.document = ''
    form.address = ''
    await load()
  } catch (err) {
    error.value = err.message
  }
}

async function onShred(id) {
  try {
    await shredCustomerKey(id)
    await load()
  } catch (err) {
    error.value = err.message
  }
}

onMounted(load)
</script>

<template>
<div class="tab-panel">
  <section class="panel">
    <div class="panel-header">
      <h2>
        <span class="panel-icon">➕</span>
        Criar cliente
      </h2>
      <button type="button" class="secondary" @click="fillFakeData">🎲 Preencher tudo com dados fake</button>
    </div>
    <form class="inline" @submit.prevent="onSubmit">
      <div class="field">
        <label>Nome</label>
        <input v-model="form.name" required />
      </div>
      <div class="field">
        <label>Documento</label>
        <input v-model="form.document" required />
      </div>
      <div class="field">
        <label>Endereço</label>
        <input v-model="form.address" required />
      </div>
      <button class="primary" type="submit">Criar cliente</button>
    </form>
  </section>

  <section class="panel">
    <div class="panel-header">
      <h2>
        <span class="panel-icon">👤</span>
        Clientes
      </h2>
      <span v-if="customers.length" class="count-badge">{{ customers.length }}</span>
    </div>
    <p v-if="error" class="badge badge-danger">{{ error }}</p>
    <div v-if="loading" class="loading"><span class="spinner"></span>Carregando clientes…</div>
    <div v-else-if="customers.length === 0" class="empty">
      <span class="empty-icon">👤</span>
      Nenhum cliente cadastrado ainda.
    </div>
    <div v-else class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>Id</th>
            <th>Cifrado (no banco)</th>
            <th>Decifrado (via chave)</th>
            <th>Chave</th>
            <th>Origem</th>
            <th>Ação</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="customer in customers" :key="customer.id">
            <td class="cell-mono">{{ customer.id.slice(0, 8) }}</td>
            <td class="cell-mono cipher-block">
              <div class="cipher-row">
                <span class="field-label">nome</span>
                <span class="cipher-value" :title="customer.encryptedName">{{ truncate(customer.encryptedName) }}</span>
              </div>
              <div class="cipher-row">
                <span class="field-label">documento</span>
                <span class="cipher-value" :title="customer.encryptedDocument">{{ truncate(customer.encryptedDocument) }}</span>
              </div>
              <div class="cipher-row">
                <span class="field-label">endereço</span>
                <span class="cipher-value" :title="customer.encryptedAddress">{{ truncate(customer.encryptedAddress) }}</span>
              </div>
            </td>
            <td>
              <template v-if="decryptedById[customer.id]?.status === 'ok'">
                <div>{{ decryptedById[customer.id].decrypted.name }}</div>
                <div class="muted">{{ decryptedById[customer.id].decrypted.document }}</div>
                <div class="muted">{{ decryptedById[customer.id].decrypted.address }}</div>
              </template>
              <span v-else class="muted">dado esquecido</span>
            </td>
            <td>
              <span v-if="customer.keyExists" class="badge badge-ok">🔑 Ativa</span>
              <span v-else class="badge badge-danger">🚫 Shredded</span>
            </td>
            <td>
              <span v-if="decryptedById[customer.id]?.source" class="badge badge-neutral">
                {{ decryptedById[customer.id].source }}
              </span>
              <span v-else class="muted">—</span>
            </td>
            <td>
              <button v-if="customer.keyExists" class="danger" @click="onShred(customer.id)">
                Shred (apagar chave)
              </button>
              <span v-else class="muted">—</span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</div>
</template>
