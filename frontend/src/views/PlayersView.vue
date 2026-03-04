<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { usePlayersStore } from '@/stores/players'
import { getOnlinePlayers, kickPlayer, banPlayer } from '@/api/players'
import { usePermissions } from '@/composables/usePermissions'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Tag from 'primevue/tag'
import ProgressBar from 'primevue/progressbar'
import type { PlayerInfo } from '@/types'

const { t } = useI18n()
const router = useRouter()
const toast = useToast()
const confirm = useConfirm()
const playersStore = usePlayersStore()
const { canKickPlayers, canBanPlayers } = usePermissions()
const loading = ref(true)
const searchFilter = ref('')

const filteredPlayers = computed(() => {
  const search = searchFilter.value.toLowerCase()
  if (!search) return playersStore.playerList
  return playersStore.playerList.filter(
    (p) => p.playerName.toLowerCase().includes(search) || p.playerId?.toLowerCase().includes(search)
  )
})

async function fetchPlayers() {
  try {
    const players = await getOnlinePlayers()
    playersStore.setPlayers(players)
  } catch {
    toast.add({ severity: 'error', summary: t('common.error'), detail: t('players.failedToFetch'), life: 3000 })
  } finally {
    loading.value = false
  }
}

function viewPlayer(player: PlayerInfo) {
  router.push({ name: 'PlayerDetail', params: { entityId: player.entityId.toString() } })
}

function confirmKick(player: PlayerInfo) {
  confirm.require({
    message: t('players.confirmKickMessage', { name: player.playerName }),
    header: t('players.confirmKickHeader'),
    icon: 'pi pi-exclamation-triangle',
    acceptClass: 'p-button-warning',
    accept: async () => {
      try {
        await kickPlayer(player.entityId)
        toast.add({ severity: 'success', summary: t('players.kicked'), detail: t('players.kickedDetail', { name: player.playerName }), life: 3000 })
      } catch {
        toast.add({ severity: 'error', summary: t('common.error'), detail: t('players.failedToKick'), life: 3000 })
      }
    },
  })
}

function confirmBan(player: PlayerInfo) {
  confirm.require({
    message: t('players.confirmBanMessage', { name: player.playerName }),
    header: t('players.confirmBanHeader'),
    icon: 'pi pi-ban',
    acceptClass: 'p-button-danger',
    accept: async () => {
      try {
        await banPlayer(player.entityId)
        toast.add({ severity: 'success', summary: t('players.banned'), detail: t('players.bannedDetail', { name: player.playerName }), life: 3000 })
      } catch {
        toast.add({ severity: 'error', summary: t('common.error'), detail: t('players.failedToBan'), life: 3000 })
      }
    },
  })
}

function formatPosition(p: PlayerInfo): string {
  return `${Math.round(p.positionX)}, ${Math.round(p.positionY)}, ${Math.round(p.positionZ)}`
}

onMounted(fetchPlayers)
</script>

<template>
  <div class="players-view">
    <div class="page-header">
      <h1 class="page-title">{{ t('players.title') }}</h1>
      <Tag :value="t('players.online', { n: playersStore.onlineCount })" severity="info" />
    </div>

    <div class="toolbar">
      <span class="p-input-icon-left search-wrapper">
        <i class="pi pi-search" />
        <InputText v-model="searchFilter" :placeholder="t('players.searchPlaceholder')" class="search-input" />
      </span>
      <Button icon="pi pi-refresh" text severity="secondary" @click="fetchPlayers" :loading="loading" />
    </div>

    <DataTable
      :value="filteredPlayers"
      :loading="loading"
      stripedRows
      sortField="playerName"
      :sortOrder="1"
      class="players-table"
      :rowHover="true"
      @row-click="(e: any) => viewPlayer(e.data)"
    >
      <Column field="playerName" :header="t('players.name')" sortable>
        <template #body="{ data }">
          <div class="player-name">
            <i class="pi pi-user" />
            <span>{{ data.playerName }}</span>
            <Tag v-if="data.isAdmin" :value="t('players.admin')" severity="warn" class="admin-tag" />
          </div>
        </template>
      </Column>

      <Column field="level" :header="t('players.level')" sortable style="width: 80px" />

      <Column :header="t('players.health')" style="width: 150px">
        <template #body="{ data }">
          <ProgressBar :value="data.health" :showValue="false" class="health-bar" style="height: 8px" />
          <span class="bar-label">{{ Math.round(data.health) }}</span>
        </template>
      </Column>

      <Column :header="t('players.position')" style="width: 180px">
        <template #body="{ data }">
          <span class="position-text">{{ formatPosition(data) }}</span>
        </template>
      </Column>

      <Column field="zombieKills" :header="t('players.zKills')" sortable style="width: 80px" />
      <Column field="playerKills" :header="t('players.pKills')" sortable style="width: 80px" />
      <Column field="deaths" :header="t('players.deaths')" sortable style="width: 80px" />

      <Column v-if="canKickPlayers || canBanPlayers" :header="t('players.actions')" style="width: 120px">
        <template #body="{ data }">
          <div class="action-buttons" @click.stop>
            <Button v-if="canKickPlayers" icon="pi pi-sign-out" text severity="warning" size="small" @click="confirmKick(data)" :title="t('playerDetail.kick')" />
            <Button v-if="canBanPlayers" icon="pi pi-ban" text severity="danger" size="small" @click="confirmBan(data)" :title="t('playerDetail.ban')" />
          </div>
        </template>
      </Column>

      <template #empty>
        <div class="empty-state">
          <i class="pi pi-users" style="font-size: 2rem; color: var(--kc-text-secondary)" />
          <p>{{ t('players.noPlayersOnline') }}</p>
        </div>
      </template>
    </DataTable>
  </div>
</template>

<style scoped>
.players-view { display: flex; flex-direction: column; gap: 1rem; }
.page-header { display: flex; align-items: center; gap: 1rem; }
.page-title { font-size: 1.5rem; font-weight: 600; }
.toolbar { display: flex; align-items: center; gap: 0.5rem; }
.search-wrapper { flex: 1; max-width: 350px; }
.search-input { width: 100%; }
.players-table { cursor: pointer; }
.player-name { display: flex; align-items: center; gap: 0.5rem; }
.admin-tag { font-size: 0.65rem; }
.health-bar { margin-bottom: 2px; }
.bar-label { font-size: 0.75rem; color: var(--kc-text-secondary); }
.position-text { font-family: monospace; font-size: 0.85rem; color: var(--kc-text-secondary); }
.action-buttons { display: flex; gap: 0.25rem; }
.empty-state { display: flex; flex-direction: column; align-items: center; gap: 0.5rem; padding: 2rem; color: var(--kc-text-secondary); }

@media (max-width: 768px) {
  .page-header { flex-wrap: wrap; }
  .toolbar { flex-wrap: wrap; width: 100%; }
  .search-wrapper { max-width: none; flex: 1 1 100%; }
}
</style>
