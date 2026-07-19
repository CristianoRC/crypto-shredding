# Crypto-Shredding

POC didática de **crypto-shredding**: cada cliente tem uma chave AES própria. "Esquecer" o cliente
(GDPR/LGPD) = apagar só a chave. O dado cifrado continua no banco para sempre, mas irrecuperável.

- `Customers`: dono do PII. Cifra nome/documento/endereço e guarda em `postgres-customers`. A chave (DEK)
  de cada cliente vai num banco separado, `postgres-customers-keys` (o cofre) — embrulhada por uma master
  key (KEK), ver [Envelope encryption](#envelope-encryption-a-chave-que-cifra-as-outras) abaixo. Um Redis
  cacheia o resultado decifrado (TTL 10min).
- `Orders`: "outro sistema". Guarda só `CustomerId` + dados de negócio, nunca PII, e busca o cliente via
  HTTP na API de `Customers`.

**O ponto central da POC**: apagar a chave em um único lugar (o cofre de `Customers`) torna o dado
irrecuperável em toda a arquitetura — `Orders` não precisa fazer nada para "esquecer" o cliente também.

## Arquitetura

```mermaid
flowchart TD
    Browser -->|http://localhost| Ingress
    Ingress -->|/| Web["web (Vue + nginx)"]
    Ingress -->|/api/customers| CustomersApi["customers-api (.NET 10)"]
    Ingress -->|/api/orders| OrdersApi["orders-api (.NET 10)"]
    OrdersApi -->|"GET /api/customers/{id}"| CustomersApi

    CustomersApi --> PgCustomers[("postgres-customers\ncustomersdb.Customers\nEncryptedName/Document/Address")]
    CustomersApi -->|"wrap/unwrap da DEK"| MasterKey["MasterKey (KEK)\nappsettings hoje\nAzure Key Vault / HashiCorp Vault em produção"]
    CustomersApi --> PgKeys[("postgres-customers-keys\nkeyvault.EncryptionKeys\nCOFRE — DEK embrulhada pela KEK")]
    CustomersApi --> Redis[("redis\ncache do decifrado, TTL 10min")]
    OrdersApi --> PgOrders[("postgres-orders\nordersdb.Orders\nId, CustomerId, Product, Amount")]
```

Shred = `DELETE` na linha de `EncryptionKeys` + invalida o Redis. `postgres-customers` e `postgres-orders`
não mudam em nada.

## Envelope encryption: a chave que cifra as outras

Cada cliente tem sua própria DEK (*data encryption key*, AES-128 + IV) gerada na hora do `POST`. Só que
essa DEK não é gravada em claro no cofre: ela é embrulhada (cifrada) por uma **master key** (KEK,
*key-encryption-key*) antes de ir para `postgres-customers-keys`. Quem olhar a tabela `EncryptionKeys`
direto no banco (um dump, um backup vazado) vê só `WrappedKeyMaterial` — um blob AES-CBC sem nenhum
significado sem a KEK.

- `CryptoService.CreateAsync` gera a DEK, embrulha com a KEK (`WrapKeyMaterial`) e só a versão embrulhada
  vai para o `EncryptionKeys`.
- `CryptoService.TryDecryptAsync` lê o blob embrulhado, desembrulha com a KEK (`UnwrapKeyMaterial`) para
  recuperar a DEK e só então decifra nome/documento/endereço — igual antes.
- O shred **não muda**: continua sendo o `DELETE` da linha. A DEK embrulhada, sem a linha, não serve pra
  nada — e mesmo que sobrevivesse, sem a KEK ela também é inútil.

Hoje a KEK é só um valor em `appsettings.json` / variável de ambiente (`MasterKey:Key` / `MasterKey:Iv`,
ver [MasterKeyOptions.cs](CryptoShredding/Customers/Customers.Core/Services/CryptoService/MasterKeyOptions.cs)) —
suficiente pra POC, mas é o ponto único de falha da arquitetura: quem lê o appsettings ou as env vars do
pod lê a KEK. Em produção esse valor sairia do código/config e moraria num vault de verdade:

- **Azure Key Vault**: a KEK vira uma `Key` gerenciada lá (possivelmente HSM-backed), e o `wrap`/`unwrap`
  passam a ser chamadas ao vault (`WrapKey`/`UnwrapKey`) em vez de AES local. Como só existe **uma** KEK
  (não uma por cliente), o custo fica em centavos (tier Standard, `$0.03`/10k operações, sem custo de
  armazenamento) — o padrão certo pra escala, ao contrário de guardar uma secret por cliente lá.
- **HashiCorp Vault (Transit engine)**: equivalente open source, roda no mesmo cluster kind — `datakey`/
  `encrypt`/`decrypt` fazem o mesmo papel do Key Vault, sem sair do ambiente 100% local do projeto.

Nos dois casos a mudança fica isolada em `WrapKeyMaterial`/`UnwrapKeyMaterial` (troca a chamada de AES
local por uma chamada HTTP ao vault) — o resto do fluxo (DEK por cliente, shred = delete da linha) não
muda.

## Estrutura

```text
CryptoShredding/
  Customers/Customers.Core/   # entidades, DbContexts, CryptoService (cifra/decifra/shred)
  Customers/Customers.Api/    # controllers HTTP
  Orders/Orders.Api/          # "outro sistema", consome Customers via HTTP
  web/                        # frontend Vue 3 (abas Clientes / Pedidos)
  docker-compose.yml          # alternativa sem k8s
k8s/                          # manifests (namespace, postgres x3, redis, apis, web, ingress)
kind-config.yaml
scripts/
  up.sh                  # cria o cluster kind, instala ingress-nginx, builda e aplica os manifests
  build.sh               # builda as imagens e carrega no cluster kind
  down.sh                # apaga o cluster kind
  port-forward-dbs.sh    # expõe os bancos/Redis em localhost para um client (DBeaver, RedisInsight...)
```

## Pré-requisitos

Docker, [kind](https://kind.sigs.k8s.io/), kubectl. .NET 10 SDK é opcional (só para rodar fora de containers).

## Rodando

```bash
./scripts/up.sh
```

Acesse **<http://localhost>**:

1. Aba **Clientes**: crie um cliente. O badge `source` mostra `database` na primeira leitura e `cache`
   nas seguintes (TTL 10min).
2. Aba **Pedidos**: crie um pedido para esse cliente. A tabela mostra o nome do cliente (buscado ao vivo
   na `customers-api`).
3. Clique em **Shred (apagar chave)** na aba Clientes.
4. Volte para **Pedidos** e recarregue: o mesmo pedido agora mostra **"Cliente removido (dado esquecido)"**
   — sem que `Orders` tenha feito nada.

Para inspecionar os bancos direto:

```bash
kubectl -n crypto-shredding exec deploy/postgres-customers -- psql -U postgres -d customersdb -c 'SELECT * FROM "Customers";'
kubectl -n crypto-shredding exec deploy/postgres-customers-keys -- psql -U postgres -d keyvault -c 'SELECT * FROM "EncryptionKeys";'
kubectl -n crypto-shredding exec deploy/redis -- redis-cli KEYS 'customer:*'
kubectl -n crypto-shredding exec deploy/postgres-orders -- psql -U postgres -d ordersdb -c 'SELECT * FROM "Orders";'
```

Ou, para usar um client gráfico (DBeaver, TablePlus, RedisInsight) via `localhost`:

```bash
./scripts/port-forward-dbs.sh
```

Para encerrar:

```bash
./scripts/down.sh
```

## Sem Kubernetes (docker-compose)

```bash
cd CryptoShredding
docker-compose up --build
```

- Frontend: <http://localhost:8081>
- API de clientes: <http://localhost:5231/api/customers>
- API de pedidos: <http://localhost:5122/api/orders>
