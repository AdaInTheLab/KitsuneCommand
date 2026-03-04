<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { usePermissions } from '@/composables/usePermissions'
import { usePlayersStore } from '@/stores/players'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import {
  getVipGifts,
  getVipGiftDetail,
  createVipGift,
  updateVipGift,
  deleteVipGift,
  claimVipGift,
} from '@/api/vipgifts'
import { getItemDefinitions, getCommandDefinitions } from '@/api/store'
import type { VipGift, ItemDefinition, CommandDefinition } from '@/types'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Textarea from 'primevue/textarea'
import Dialog from 'primevue/dialog'
import Select from 'primevue/select'
import Tag from 'primevue/tag'
import MultiSelect from 'primevue/multiselect'

const { t } = useI18n()
const toast = useToast()
const confirmService = useConfirm()
const playersStore = usePlayersStore()
const { canManageVipGifts } = usePermissions()

// State
const loading = ref(true)
const gifts = ref<VipGift[]>([])
const totalGifts = ref(0)
const pageIndex = ref(0)
const pageSize = ref(50)
const searchFilter = ref('')
let searchTimeout: ReturnType<typeof setTimeout> | null = null

// Definitions for linking
const itemDefs = ref<ItemDefinition[]>([])
const cmdDefs = ref<CommandDefinition[]>([])

// Gift CRUD dialog
const showGiftDialog = ref(false)
const giftDialogMode = ref<'create' | 'edit'>('create')
const giftForm = ref({
  playerId: '',
  playerName: '',
  name: '',
  description: '',
  claimPeriod: null as string | null,
  itemIds: [] as number[],
  commandIds: [] as number[],
})
const editingGiftId = ref<number | null>(null)
const giftDialogLoading = ref(false)

// Claim dialog
const showClaimDialog = ref(false)
const claimGift = ref<VipGift | null>(null)
const claimLoading = ref(false)

const periodOptions = [
  { label: t('vipGifts.oneTime'), value: null },
  { label: t('vipGifts.daily'), value: 'daily' },
  { label: t('vipGifts.weekly'), value: 'weekly' },
  { label: t('vipGifts.monthly'), value: 'monthly' },
]

async function fetchGifts() {
  loading.value = true
  try {
    const result = await getVipGifts({
      pageIndex: pageIndex.value,
      pageSize: pageSize.value,
      search: searchFilter.value || undefined,
    })
    gifts.value = result.items
    totalGifts.value = result.total
  } catch {
    toast.add({ severity: 'error', summary: t('common.error'), detail: t('vipGifts.failedToLoad'), life: 3000 })
  } finally {
    loading.value = false
  }
}

async function fetchDefinitions() {
  try {
    const [items, cmds] = await Promise.all([getItemDefinitions(), getCommandDefinitions()])
    itemDefs.value = items
    cmdDefs.value = cmds
  } catch {
    // non-critical
  }
}

function onPage(event: { first: number; rows: number }) {
  pageIndex.value = Math.floor(event.first / event.rows)
  pageSize.value = event.rows
  fetchGifts()
}

function onSearch() {
  if (searchTimeout) clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => {
    pageIndex.value = 0
    fetchGifts()
  }, 400)
}

// ─── Gift CRUD ──────────────────────────────────────

function openCreateDialog() {
  giftDialogMode.value = 'create'
  giftForm.value = { playerId: '', playerName: '', name: '', description: '', claimPeriod: null, itemIds: [], commandIds: [] }
  editingGiftId.value = null
  showGiftDialog.value = true
}

async function openEditDialog(gift: VipGift) {
  giftDialogMode.value = 'edit'
  editingGiftId.value = gift.id

  try {
    const detail = await getVipGiftDetail(gift.id)
    giftForm.value = {
      playerId: detail.playerId,
      playerName: detail.playerName || '',
      name: detail.name,
      description: detail.description || '',
      claimPeriod: detail.claimPeriod,
      itemIds: detail.items.map((i) => i.id),
      commandIds: detail.commands.map((c) => c.id),
    }
    showGiftDialog.value = true
  } catch {
    toast.add({ severity: 'error', summary: t('common.error'), detail: t('vipGifts.failedToLoadDetail'), life: 3000 })
  }
}

async function saveGift() {
  if (!giftForm.value.name.trim()) {
    toast.add({ severity: 'warn', summary: t('common.validation'), detail: t('vipGifts.giftNameRequired'), life: 3000 })
    return
  }

  giftDialogLoading.value = true
  try {
    if (giftDialogMode.value === 'create') {
      if (!giftForm.value.playerId.trim()) {
        toast.add({ severity: 'warn', summary: t('common.validation'), detail: t('vipGifts.playerIdRequired'), life: 3000 })
        giftDialogLoading.value = false
        return
      }
      await createVipGift({
        playerId: giftForm.value.playerId.trim(),
        playerName: giftForm.value.playerName.trim() || undefined,
        name: giftForm.value.name.trim(),
        description: giftForm.value.description.trim() || undefined,
        claimPeriod: giftForm.value.claimPeriod,
        itemIds: giftForm.value.itemIds,
        commandIds: giftForm.value.commandIds,
      })
      toast.add({ severity: 'success', summary: t('common.success'), detail: t('vipGifts.giftCreated', { name: giftForm.value.name }), life: 3000 })
    } else {
      await updateVipGift(editingGiftId.value!, {
        name: giftForm.value.name.trim(),
        description: giftForm.value.description.trim() || undefined,
        claimPeriod: giftForm.value.claimPeriod,
        playerName: giftForm.value.playerName.trim() || undefined,
        itemIds: giftForm.value.itemIds,
        commandIds: giftForm.value.commandIds,
      })
      toast.add({ severity: 'success', summary: t('common.success'), detail: t('vipGifts.giftUpdated', { name: giftForm.value.name }), life: 3000 })
    }

    showGiftDialog.value = false
    fetchGifts()
  } catch (err: any) {
    const msg = err?.response?.data?.message || t('vipGifts.failedToSave')
    toast.add({ severity: 'error', summary: t('common.error'), detail: msg, life: 3000 })
  } finally {
    giftDialogLoading.value = false
  }
}

function confirmDeleteGift(gift: VipGift) {
  confirmService.require({
    message: t('vipGifts.confirmDeleteMessage', { name: gift.name, player: gift.playerName || gift.playerId }),
    header: t('common.confirmDelete'),
    icon: 'pi pi-trash',
    acceptClass: 'p-button-danger',
    accept: async () => {
      try {
        await deleteVipGift(gift.id)
        toast.add({ severity: 'success', summary: t('common.success'), detail: t('vipGifts.giftDeleted', { name: gift.name }), life: 3000 })
        fetchGifts()
      } catch {
        toast.add({ severity: 'error', summary: t('common.error'), detail: t('vipGifts.failedToDelete'), life: 3000 })
      }
    },
  })
}

// ─── Claiming ───────────────────────────────────────

function openClaimDialog(gift: VipGift) {
  claimGift.value = gift
  showClaimDialog.value = true
}

async function executeClaim() {
  if (!claimGift.value) return

  claimLoading.value = true
  try {
    const result = await claimVipGift(claimGift.value.id)
    toast.add({ severity: 'success', summary: t('vipGifts.claimed'), detail: result.message, life: 4000 })
    showClaimDialog.value = false
    fetchGifts()
  } catch (err: any) {
    const msg = err?.response?.data?.message || t('vipGifts.failedToClaim')
    toast.add({ severity: 'error', summary: t('common.error'), detail: msg, life: 4000 })
  } finally {
    claimLoading.value = false
  }
}

// ─── Helpers ────────────────────────────────────────

function formatDate(dateStr: string | null): string {
  if (!dateStr) return '—'
  const d = new Date(dateStr + 'Z')
  return d.toLocaleDateString() + ' ' + d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
}

function formatPeriod(period: string | null): string {
  if (!period) return t('vipGifts.oneTime')
  switch (period) {
    case 'daily': return t('vipGifts.daily')
    case 'weekly': return t('vipGifts.weekly')
    case 'monthly': return t('vipGifts.monthly')
    default: return period.charAt(0).toUpperCase() + period.slice(1)
  }
}

function getStatusSeverity(gift: VipGift): string {
  if (gift.isClaimable) return 'success'
  if (gift.claimPeriod) return 'warn'
  return 'secondary'
}

function getStatusLabel(gift: VipGift): string {
  if (gift.isClaimable) return t('vipGifts.ready')
  if (gift.claimPeriod) return t('vipGifts.cooldown')
  return t('vipGifts.claimedStatus')
}

function isPlayerOnline(playerId: string): boolean {
  return playersStore.playerList.some((p) => p.playerId === playerId && p.isOnline)
}

function selectPlayer(player: { playerId: string; playerName: string }) {
  giftForm.value.playerId = player.playerId
  giftForm.value.playerName = player.playerName
}

const onlinePlayers = () => playersStore.playerList.filter((p) => p.isOnline)

onMounted(() => {
  fetchGifts()
  fetchDefinitions()
})
</script>

<template>
  <div class="vipgifts-view">
    <div class="page-header">
      <h1 class="page-title">{{ t('vipGifts.title') }}</h1>
    </div>

    <div class="toolbar">
      <span class="p-input-icon-left search-wrapper">
        <i class="pi pi-search" />
        <InputText v-model="searchFilter" :placeholder="t('vipGifts.searchPlaceholder')" class="search-input" @input="onSearch" />
      </span>
      <Button icon="pi pi-refresh" text severity="secondary" @click="fetchGifts" :loading="loading" />
      <div class="toolbar-spacer" />
      <Button
        v-if="canManageVipGifts"
        :label="t('vipGifts.addVipGift')"
        icon="pi pi-plus"
        severity="info"
        size="small"
        @click="openCreateDialog"
      />
    </div>

    <DataTable
      :value="gifts"
      :loading="loading"
      stripedRows
      :paginator="true"
      :rows="pageSize"
      :totalRecords="totalGifts"
      :lazy="true"
      :rowsPerPageOptions="[25, 50, 100]"
      @page="onPage"
    >
      <Column :header="t('vipGifts.player')" style="width: 200px">
        <template #body="{ data }">
          <div class="player-cell">
            <span class="player-name">{{ data.playerName || data.playerId }}</span>
            <span v-if="data.playerName" class="player-id">{{ data.playerId }}</span>
          </div>
        </template>
      </Column>

      <Column field="name" :header="t('vipGifts.giftName')">
        <template #body="{ data }">
          <span class="gift-name">{{ data.name }}</span>
        </template>
      </Column>

      <Column :header="t('vipGifts.period')" style="width: 110px">
        <template #body="{ data }">
          <Tag :value="formatPeriod(data.claimPeriod)" :severity="data.claimPeriod ? 'info' : 'secondary'" />
        </template>
      </Column>

      <Column :header="t('vipGifts.status')" style="width: 100px">
        <template #body="{ data }">
          <Tag :value="getStatusLabel(data)" :severity="getStatusSeverity(data)" />
        </template>
      </Column>

      <Column :header="t('vipGifts.claims')" style="width: 80px">
        <template #body="{ data }">
          <span>{{ data.totalClaimCount }}</span>
        </template>
      </Column>

      <Column :header="t('vipGifts.lastClaimed')" style="width: 170px">
        <template #body="{ data }">
          <span class="date-text">{{ formatDate(data.lastClaimedAt) }}</span>
        </template>
      </Column>

      <Column :header="t('vipGifts.actions')" style="width: 160px">
        <template #body="{ data }">
          <div class="action-buttons">
            <Button
              v-if="canManageVipGifts && data.isClaimable"
              icon="pi pi-gift"
              text
              severity="success"
              size="small"
              @click="openClaimDialog(data)"
              :title="t('vipGifts.claimVipGift')"
            />
            <Button
              v-if="canManageVipGifts"
              icon="pi pi-pencil"
              text
              severity="secondary"
              size="small"
              @click="openEditDialog(data)"
              :title="t('common.edit')"
            />
            <Button
              v-if="canManageVipGifts"
              icon="pi pi-trash"
              text
              severity="danger"
              size="small"
              @click="confirmDeleteGift(data)"
              :title="t('common.delete')"
            />
          </div>
        </template>
      </Column>

      <template #empty>
        <div class="empty-state">
          <i class="pi pi-gift" style="font-size: 2rem; color: var(--kc-text-secondary)" />
          <p>{{ t('vipGifts.noVipGiftsYet') }}</p>
          <span class="empty-hint">{{ t('vipGifts.createGiftsHint') }}</span>
        </div>
      </template>
    </DataTable>

    <!-- Gift CRUD Dialog -->
    <Dialog
      v-model:visible="showGiftDialog"
      :header="giftDialogMode === 'create' ? t('vipGifts.createVipGift') : t('vipGifts.editVipGift')"
      :modal="true"
      :style="{ width: '550px' }"
    >
      <div class="form-grid">
        <!-- Player selection (only on create) -->
        <div v-if="giftDialogMode === 'create'" class="form-field">
          <label>{{ t('vipGifts.playerLabel') }}</label>
          <div class="player-select-row">
            <Select
              :options="onlinePlayers()"
              optionLabel="playerName"
              :placeholder="t('vipGifts.pickOnlinePlayer')"
              class="player-select"
              filter
              @change="(e: any) => selectPlayer(e.value)"
            />
            <span class="or-text">{{ t('vipGifts.or') }}</span>
          </div>
          <InputText v-model="giftForm.playerId" :placeholder="t('vipGifts.playerIdPlaceholder')" class="w-full" />
          <InputText v-model="giftForm.playerName" :placeholder="t('vipGifts.playerNameOptional')" class="w-full" style="margin-top: 0.25rem" />
        </div>

        <div v-if="giftDialogMode === 'edit'" class="form-field">
          <label>{{ t('vipGifts.displayName') }}</label>
          <InputText v-model="giftForm.playerName" :placeholder="t('vipGifts.displayNamePlaceholder')" class="w-full" />
        </div>

        <div class="form-field">
          <label>{{ t('vipGifts.giftNameLabel') }}</label>
          <InputText v-model="giftForm.name" :placeholder="t('vipGifts.giftNamePlaceholder')" class="w-full" />
        </div>

        <div class="form-field">
          <label>{{ t('vipGifts.claimPeriod') }}</label>
          <Select
            v-model="giftForm.claimPeriod"
            :options="periodOptions"
            optionLabel="label"
            optionValue="value"
            :placeholder="t('vipGifts.selectPeriod')"
            class="w-full"
          />
        </div>

        <div class="form-field">
          <label>{{ t('vipGifts.descriptionLabel') }}</label>
          <Textarea v-model="giftForm.description" rows="2" class="w-full" :placeholder="t('vipGifts.descriptionPlaceholder')" />
        </div>

        <div class="form-field">
          <label>{{ t('vipGifts.linkedItems') }}</label>
          <MultiSelect
            v-model="giftForm.itemIds"
            :options="itemDefs"
            optionLabel="itemName"
            optionValue="id"
            :placeholder="t('vipGifts.selectItems')"
            class="w-full"
            filter
            display="chip"
          />
        </div>

        <div class="form-field">
          <label>{{ t('vipGifts.linkedCommands') }}</label>
          <MultiSelect
            v-model="giftForm.commandIds"
            :options="cmdDefs"
            optionLabel="command"
            optionValue="id"
            :placeholder="t('vipGifts.selectCommands')"
            class="w-full"
            filter
            display="chip"
          />
        </div>
      </div>

      <template #footer>
        <Button :label="t('common.cancel')" text severity="secondary" @click="showGiftDialog = false" />
        <Button
          :label="giftDialogMode === 'create' ? t('common.create') : t('common.save')"
          severity="info"
          @click="saveGift"
          :loading="giftDialogLoading"
        />
      </template>
    </Dialog>

    <!-- Claim Dialog -->
    <Dialog
      v-model:visible="showClaimDialog"
      :header="t('vipGifts.claimVipGift')"
      :modal="true"
      :style="{ width: '420px' }"
    >
      <div class="claim-form" v-if="claimGift">
        <p class="claim-info">
          <strong>{{ t('vipGifts.giftLabel') }}:</strong> {{ claimGift.name }}<br />
          <strong>{{ t('vipGifts.playerLabel2') }}:</strong> {{ claimGift.playerName || claimGift.playerId }}<br />
          <strong>{{ t('vipGifts.periodLabel') }}:</strong> {{ formatPeriod(claimGift.claimPeriod) }}<br />
          <strong>{{ t('vipGifts.totalClaims') }}:</strong> {{ claimGift.totalClaimCount }}
        </p>
        <p v-if="!isPlayerOnline(claimGift.playerId)" class="warn-text">
          <i class="pi pi-exclamation-triangle" /> {{ t('vipGifts.playerNotOnline') }}
        </p>
      </div>

      <template #footer>
        <Button :label="t('common.cancel')" text severity="secondary" @click="showClaimDialog = false" />
        <Button
          :label="t('vipGifts.deliverGift')"
          icon="pi pi-gift"
          severity="success"
          @click="executeClaim"
          :loading="claimLoading"
          :disabled="!claimGift || !isPlayerOnline(claimGift.playerId)"
        />
      </template>
    </Dialog>
  </div>
</template>

<style scoped>
.vipgifts-view { display: flex; flex-direction: column; gap: 1rem; }
.page-header { display: flex; align-items: center; gap: 1rem; }
.page-title { font-size: 1.5rem; font-weight: 600; }

.toolbar { display: flex; align-items: center; gap: 0.5rem; }
.toolbar-spacer { flex: 1; }
.search-wrapper { flex: 0 1 350px; }
.search-input { width: 100%; }

.player-cell { display: flex; flex-direction: column; }
.player-name { font-weight: 500; }
.player-id { font-size: 0.75rem; color: var(--kc-text-secondary); font-family: monospace; }

.gift-name { font-weight: 500; color: var(--kc-cyan); }
.date-text { font-size: 0.85rem; color: var(--kc-text-secondary); }
.action-buttons { display: flex; gap: 0.25rem; }

.empty-state { display: flex; flex-direction: column; align-items: center; gap: 0.5rem; padding: 2rem; color: var(--kc-text-secondary); }
.empty-hint { font-size: 0.85rem; color: var(--kc-text-secondary); }

.form-grid { display: flex; flex-direction: column; gap: 1rem; }
.form-field { display: flex; flex-direction: column; gap: 0.25rem; }
.form-field label { font-size: 0.85rem; color: var(--kc-text-secondary); }
.form-field small { opacity: 0.7; }
.w-full { width: 100%; }

.player-select-row { display: flex; align-items: center; gap: 0.5rem; margin-bottom: 0.25rem; }
.player-select { flex: 1; }
.or-text { font-size: 0.8rem; color: var(--kc-text-secondary); }

.claim-form { display: flex; flex-direction: column; gap: 0.75rem; }
.claim-info { margin: 0; line-height: 1.8; }
.warn-text { color: var(--kc-orange); font-size: 0.85rem; display: flex; align-items: center; gap: 0.5rem; }

@media (max-width: 768px) {
  .toolbar { flex-wrap: wrap; width: 100%; }
  .search-wrapper { flex: 1 1 100%; }
}
</style>
