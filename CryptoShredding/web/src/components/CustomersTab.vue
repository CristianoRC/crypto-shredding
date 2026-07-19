<script setup>
import { onMounted, reactive, ref } from 'vue'
import { createCustomer, getCustomer, listCustomers, shredCustomerKey } from '../customersApi'

const customers = ref([])
const decryptedById = reactive({})
const loading = ref(false)
const error = ref('')

const form = reactive({ name: '', document: '', address: '' })

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
  <section class="panel">
    <h2>Criar cliente</h2>
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
    <h2>Clientes</h2>
    <p v-if="error" class="badge badge-danger">{{ error }}</p>
    <p v-if="!loading && customers.length === 0" class="empty">Nenhum cliente cadastrado ainda.</p>
    <table v-else>
      <thead>
        <tr>
          <th>Id</th>
          <th>Cifrado (no banco)</th>
          <th>Decifrado (via chave)</th>
          <th>Chave</th>
          <th>Origem</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="customer in customers" :key="customer.id">
          <td class="cell-mono">{{ customer.id.slice(0, 8) }}</td>
          <td class="cell-mono">
            <span class="field-label">nome</span>{{ customer.encryptedName }}<br />
            <span class="field-label">documento</span>{{ customer.encryptedDocument }}<br />
            <span class="field-label">endereço</span>{{ customer.encryptedAddress }}
          </td>
          <td>
            <template v-if="decryptedById[customer.id]?.status === 'ok'">
              <div>{{ decryptedById[customer.id].decrypted.name }}</div>
              <div class="muted">{{ decryptedById[customer.id].decrypted.document }}</div>
              <div class="muted">{{ decryptedById[customer.id].decrypted.address }}</div>
            </template>
            <span v-else class="muted">🗝️❌ dado esquecido (shredded)</span>
          </td>
          <td>
            <span v-if="customer.keyExists" class="badge badge-ok">🔑 Ativa</span>
            <span v-else class="badge badge-danger">🗝️❌ Shredded</span>
          </td>
          <td>
            <span v-if="decryptedById[customer.id]?.source" class="badge badge-neutral">
              {{ decryptedById[customer.id].source }}
            </span>
          </td>
          <td>
            <button v-if="customer.keyExists" class="danger" @click="onShred(customer.id)">
              Shred (apagar chave)
            </button>
          </td>
        </tr>
      </tbody>
    </table>
  </section>
</template>
