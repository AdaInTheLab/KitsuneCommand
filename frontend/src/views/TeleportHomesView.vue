<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { usePermissions } from '@/composables/usePermissions'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import { getHomes, deleteHome, teleportToHome } from '@/api/teleport'
import type { HomeLocation } from '@/types'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'

const { t } = useI18n()
const router = useRouter()
const toast = useToast()
const confirmService = useConfirm()
const { canManageTeleport, canExecuteTeleport } = usePermissions()

const loading = ref(true)
const homes = ref<HomeLocation[]>([])
const totalHomes = ref(0)
const pageIndex = ref(0)
const pageSize = ref(50)
const searchFilter = ref('')
let searchTimeout: ReturnType<typeof setTimeout> | null = null

async function fetchHomes() {
  loading.value = true
  try {
    const result = await getHomes({
      pageIndex: pageIndex.value,
      pageSize: pageSize.value,
      search: searchFilter.value || undefined,
    })
    homes.value = result.items
    totalHomes.value = result.total
  } catch {
    toast.add({ severity: 'error', summary: t('common.error'), detail: t('teleportHomes.failedToLoad'), life: 3000 })
  } finally {
    loading.value = false
  }
}

function onPage(event: { first: number; rows: number }) {
  pageIndex.value = Math.floor(event.first / event.rows)
  pageSize.value = event.rows
  fetchHomes()
}

function onSearch() {
  if (searchTimeout) clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => {
    pageIndex.value = 0
    fetchHomes()
  }, 400)
}

function confirmDeleteHome(home: HomeLocation) {
  confirmService.require({
    message: t('teleportHomes.confirmDeleteMessage', { name: home.homeName, player: home.playerName || home.playerId }),
    header: t('common.confirmDelete'),
    icon: 'pi pi-trash',
    acceptClass: 'p-button-danger',
    accept: async () => {
      try {
        await deleteHome(home.id)
        toast.add({ severity: 'success', summary: t('common.success'), detail: t('teleportHomes.homeDeleted', { name: home.homeName }), life: 3000 })
        fetchHomes()
      } catch {
        toast.add({ severity: 'error', summary: t('common.error'), detail: t('teleportHomes.failedToDelete'), life: 3000 })
      }
    },
  })
}

async function handleTeleportOwner(home: HomeLocation) {
  try {
    const result = await teleportToHome(home.id)
    toast.add({ severity: 'success', summary: t('teleportHomes.teleported'), detail: result.message, life: 4000 })
  } catch (err: any) {
    const msg = err?.response?.data?.message || t('teleportHomes.failedToTeleport')
    toast.add({ severity: 'error', summary: t('common.error'), detail: msg, life: 4000 })
  }
}

function navigateTo(tab: string) {
  if (tab === 'cities') router.push({ name: 'TeleportCities' })
  else if (tab === 'history') router.push({ name: 'TeleportHistory' })
}

watch(searchFilter, onSearch)
onMounted(fetchHomes)
</script>

<template>
  <div class="teleport-view">
    <div class="page-header">
      <h1 class="page-title">{{ t('teleport.title') }}</h1>
    </div>

    <!-- Sub-tab navigation -->
    <div class="sub-tabs">
      <button class="sub-tab" @click="navigateTo('cities')">{{ t('teleport.cities') }}</button>
      <button class="sub-tab sub-tab--active">{{ t('teleport.homes') }}</button>
      <button class="sub-tab" @click="navigateTo('history')">{{ t('teleport.history') }}</button>
    </div>

    <div class="toolbar">
      <span class="p-input-icon-left search-wrapper">
        <i class="pi pi-search" />
        <InputText v-model="searchFilter" :placeholder="t('teleportHomes.searchPlaceholder')" class="search-input" />
      </span>
      <Button icon="pi pi-refresh" text severity="secondary" @click="fetchHomes" :loading="loading" />
    </div>

    <DataTable
      :value="homes"
      :loading="loading"
      stripedRows
      :paginator="true"
      :rows="pageSize"
      :totalRecords="totalHomes"
      :lazy="true"
      :rowsPerPageOptions="[25, 50, 100]"
      @page="onPage"
    >
      <Column field="playerName" :header="t('teleportHomes.player')">
        <template #body="{ data }">
          <div class="player-name">
            <i class="pi pi-user" />
            <span>{{ data.playerName || data.playerId }}</span>
          </div>
        </template>
      </Column>

      <Column field="homeName" :header="t('teleportHomes.homeName')" />

      <Column field="position" :header="t('teleportHomes.position')" style="width: 220px">
        <template #body="{ data }">
          <code class="position-text">{{ data.position }}</code>
        </template>
      </Column>

      <Column :header="t('common.actions')" style="width: 120px">
        <template #body="{ data }">
          <div class="action-buttons">
            <Button
              v-if="canExecuteTeleport"
              icon="pi pi-send"
              text
              severity="info"
              size="small"
              @click="handleTeleportOwner(data)"
              :title="t('teleportHomes.teleportOwnerToHome')"
            />
            <Button
              v-if="canManageTeleport"
              icon="pi pi-trash"
              text
              severity="danger"
              size="small"
              @click="confirmDeleteHome(data)"
              :title="t('common.delete')"
            />
          </div>
        </template>
      </Column>

      <template #empty>
        <div class="empty-state">
          <i class="pi pi-home" style="font-size: 2rem; color: var(--kc-text-secondary)" />
          <p>{{ t('teleportHomes.noHomesYet') }}</p>
          <span class="empty-hint">{{ t('teleportHomes.homesHint') }}</span>
        </div>
      </template>
    </DataTable>
  </div>
</template>

<style scoped>
.teleport-view { display: flex; flex-direction: column; gap: 1rem; }
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
.action-buttons { display: flex; gap: 0.25rem; }
.position-text { font-size: 0.85rem; color: var(--kc-cyan); background: rgba(0, 212, 255, 0.08); padding: 0.15rem 0.5rem; border-radius: 4px; }
.empty-state { display: flex; flex-direction: column; align-items: center; gap: 0.5rem; padding: 2rem; color: var(--kc-text-secondary); }
.empty-hint { font-size: 0.85rem; color: var(--kc-text-secondary); }

@media (max-width: 768px) {
  .toolbar { flex-wrap: wrap; width: 100%; }
  .search-wrapper { max-width: none; flex: 1 1 100%; }
}

@media (max-width: 640px) {
  .sub-tabs { overflow-x: auto; white-space: nowrap; }
}
</style>
