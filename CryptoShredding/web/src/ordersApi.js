const BASE_URL = '/api/orders'

export async function listOrders() {
  const res = await fetch(BASE_URL)
  if (!res.ok) throw new Error('Falha ao listar pedidos')
  return res.json()
}

export async function createOrder({ customerId, product, amount }) {
  const res = await fetch(BASE_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ customerId, product, amount }),
  })
  if (!res.ok) throw new Error('Falha ao criar pedido')
  return res.json()
}
