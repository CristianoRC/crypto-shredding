const BASE_URL = '/api/customers'

export async function listCustomers() {
  const res = await fetch(BASE_URL)
  if (!res.ok) throw new Error('Falha ao listar clientes')
  return res.json()
}

export async function getCustomer(id) {
  const res = await fetch(`${BASE_URL}/${id}`)
  if (!res.ok) throw new Error('Falha ao buscar cliente')
  return res.json()
}

export async function createCustomer({ name, document, address }) {
  const res = await fetch(BASE_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name, document, address }),
  })
  if (!res.ok) throw new Error('Falha ao criar cliente')
  return res.json()
}

export async function shredCustomerKey(id) {
  const res = await fetch(`${BASE_URL}/${id}/key`, { method: 'DELETE' })
  if (!res.ok && res.status !== 404) throw new Error('Falha ao fazer shred da chave')
}
