<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import { getAllRedemptions } from '@/api/cdkeys'
import type { CdKeyRedeemRecord } from '@/types'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'

const { t } = useI18n()
const router = useRouter()
const toast = useToast()

const loading = ref(true)
const records = ref<CdKeyRedeemRecord[]>([])
const totalRecords = ref(0)
const pageIndex = ref(0)
const pageSize = ref(50)
const keyIdFilter = ref('')
let filterTimeout: ReturnType<typeof setTimeout> | null = null

async function fetchHistory() {
  loading.value = true
  try {
    const cdKeyId = keyIdFilter.value ? parseInt(keyIdFilter.value) : undefined
    const result = await getAllRedemptions({
      pageIndex: pageIndex.value,
      pageSize: pageSize.value,
      cdKeyId: isNaN(cdKeyId as number) ? undefined : cdKeyId,
    })
    records.value = result.items
    totalRecords.value = result.total
  } catch {
    toast.add({ severity: 'error', summary: t('common.error'), detail: t('cdKeyRedemptions.failedToLoad'), life: 3000 })
  } finally {
    loading.value = false
  }
}

function onPage(event: { first: number; rows: number }) {
  pageIndex.value = Math.floor(event.first / event.rows)
  pageSize.value = event.rows
  fetchHistory()
}

function onFilterChange() {
  if (filterTimeout) clearTimeout(filterTimeout)
  filterTimeout = setTimeout(() => {
    pageIndex.value = 0
    fetchHistory()
  }, 400)
}

function formatDate(dateStr: string): string {
  const d = new Date(dateStr + 'Z')
  return d.toLocaleDateString() + ' ' + d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
}

function navigateTo(tab: string) {
  if (tab === 'keys') router.push({ name: 'CdKeys' })
}

watch(keyIdFilter, onFilterChange)
onMounted(fetchHistory)
</script>

<template>
  <div class="cdkeys-view">
    <div class="page-header">
      <h1 class="page-title">{{ t('cdKeys.title') }}</h1>
    </div>

    <!-- Sub-tab navigation -->
    <div class="sub-tabs">
      <button class="sub-tab" @click="navigateTo('keys')">{{ t('cdKeys.keys') }}</button>
      <button class="sub-tab sub-tab--active">{{ t('cdKeys.redemptions') }}</button>
    </div>

    <div class="toolbar">
      <span class="p-input-icon-left search-wrapper">
        <i class="pi pi-search" />
        <InputText v-model="keyIdFilter" :placeholder="t('cdKeyRedemptions.filterPlaceholder')" class="search-input" />
      </span>
      <Button icon="pi pi-refresh" text severity="secondary" @click="fetchHistory" :loading="loading" />
    </div>

    <DataTable
      :value="records"
      :loading="loading"
      stripedRows
      :paginator="true"
      :rows="pageSize"
      :totalRecords="totalRecords"
      :lazy="true"
      :rowsPerPageOptions="[25, 50, 100]"
      @page="onPage"
    >
      <Column field="createdAt" :header="t('cdKeyRedemptions.date')" style="width: 180px">
        <template #body="{ data }">
          <span class="date-text">{{ formatDate(data.createdAt) }}</span>
        </template>
      </Column>

      <Column field="cdKeyId" :header="t('cdKeyRedemptions.keyId')" style="width: 100px">
        <template #body="{ data }">
          <code class="key-id">#{{ data.cdKeyId }}</code>
        </template>
      </Column>

      <Column field="playerName" :header="t('cdKeyRedemptions.player')">
        <template #body="{ data }">
          <div class="player-name">
            <i class="pi pi-user" />
            <span>{{ data.playerName || data.playerId }}</span>
          </div>
        </template>
      </Column>

      <Column field="playerId" :header="t('cdKeyRedemptions.playerId')" style="width: 200px">
        <template #body="{ data }">
          <span class="player-id-text">{{ data.playerId }}</span>
        </template>
      </Column>

      <template #empty>
        <div class="empty-state">
          <i class="pi pi-history" style="font-size: 2rem; color: var(--kc-text-secondary)" />
          <p>{{ t('cdKeyRedemptions.noRecordsYet') }}</p>
        </div>
      </template>
    </DataTable>
  </div>
</template>

<style scoped>
.cdkeys-view { display: flex; flex-direction: column; gap: 1rem; }
.page-header { display: flex; align-items: center; gap: 1rem; }
.page-title { font-size: 1.5rem; font-weight: 600; }

.sub-tabs { display: flex; gap: 0.25rem; border-bottom: 1px solid var(--kc-border); }
.sub-tab { padding: 0.5rem 1rem; border: none; background: none; color: var(--kc-text-secondary); cursor: pointer; border-bottom: 2px solid transparent; font-size: 0.9rem; transition: all 0.15s ease; }
.sub-tab:hover { color: var(--kc-text-primary); }
.sub-tab--active { color: var(--kc-cyan); border-bottom-color: var(--kc-cyan); }

.toolbar { display: flex; align-items: center; gap: 0.5rem; }
.search-wrapper { flex: 1; max-width: 350px; }
.search-input { width: 100%; }

.player-name { display: flex; align-items: center; gap: 0.5rem; }
.date-text { font-size: 0.85rem; color: var(--kc-text-secondary); }
.key-id { font-size: 0.85rem; color: var(--kc-cyan); }
.player-id-text { font-size: 0.85rem; color: var(--kc-text-secondary); font-family: monospace; }
.empty-state { display: flex; flex-direction: column; align-items: center; gap: 0.5rem; padding: 2rem; color: var(--kc-text-secondary); }

@media (max-width: 768px) {
  .toolbar { flex-wrap: wrap; width: 100%; }
  .search-wrapper { max-width: none; flex: 1 1 100%; }
}

@media (max-width: 640px) {
  .sub-tabs { overflow-x: auto; white-space: nowrap; }
}
</style>
