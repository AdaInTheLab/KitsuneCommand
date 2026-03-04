<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { usePermissions } from '@/composables/usePermissions'
import { usePlayersStore } from '@/stores/players'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import {
  getCities,
  createCity,
  updateCity,
  deleteCity,
  teleportToCity,
} from '@/api/teleport'
import type { CityLocation } from '@/types'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import InputNumber from 'primevue/inputnumber'
import Dialog from 'primevue/dialog'
import Select from 'primevue/select'
import Tag from 'primevue/tag'

const { t } = useI18n()
const router = useRouter()
const toast = useToast()
const confirmService = useConfirm()
const playersStore = usePlayersStore()
const { canManageTeleport, canExecuteTeleport } = usePermissions()

// State
const loading = ref(true)
const cities = ref<CityLocation[]>([])
const totalCities = ref(0)
const pageIndex = ref(0)
const pageSize = ref(50)

// City CRUD dialog
const showCityDialog = ref(false)
const cityDialogMode = ref<'create' | 'edit'>('create')
const cityForm = ref({ cityName: '', pointsRequired: 0, position: '', viewDirection: '' })
const editingCityId = ref<number | null>(null)
const cityDialogLoading = ref(false)

// Teleport dialog
const showTeleportDialog = ref(false)
const teleportCity = ref<CityLocation | null>(null)
const teleportPlayerId = ref('')
const teleportLoading = ref(false)

async function fetchCities() {
  loading.value = true
  try {
    const result = await getCities({ pageIndex: pageIndex.value, pageSize: pageSize.value })
    cities.value = result.items
    totalCities.value = result.total
  } catch {
    toast.add({ severity: 'error', summary: t('common.error'), detail: t('teleportCities.failedToLoad'), life: 3000 })
  } finally {
    loading.value = false
  }
}

function onPage(event: { first: number; rows: number }) {
  pageIndex.value = Math.floor(event.first / event.rows)
  pageSize.value = event.rows
  fetchCities()
}

// ─── City CRUD ──────────────────────────────────────

function openCreateDialog() {
  cityDialogMode.value = 'create'
  cityForm.value = { cityName: '', pointsRequired: 0, position: '', viewDirection: '' }
  editingCityId.value = null
  showCityDialog.value = true
}

function openEditDialog(city: CityLocation) {
  cityDialogMode.value = 'edit'
  cityForm.value = {
    cityName: city.cityName,
    pointsRequired: city.pointsRequired,
    position: city.position,
    viewDirection: city.viewDirection || '',
  }
  editingCityId.value = city.id
  showCityDialog.value = true
}

async function saveCity() {
  if (!cityForm.value.cityName.trim() || !cityForm.value.position.trim()) {
    toast.add({ severity: 'warn', summary: t('common.validation'), detail: t('teleportCities.nameAndPositionRequired'), life: 3000 })
    return
  }

  cityDialogLoading.value = true
  try {
    const data = {
      cityName: cityForm.value.cityName.trim(),
      pointsRequired: cityForm.value.pointsRequired,
      position: cityForm.value.position.trim(),
      viewDirection: cityForm.value.viewDirection.trim() || undefined,
    }

    if (cityDialogMode.value === 'create') {
      await createCity(data)
      toast.add({ severity: 'success', summary: t('common.success'), detail: t('teleportCities.cityCreated', { name: data.cityName }), life: 3000 })
    } else {
      await updateCity(editingCityId.value!, data)
      toast.add({ severity: 'success', summary: t('common.success'), detail: t('teleportCities.cityUpdated', { name: data.cityName }), life: 3000 })
    }

    showCityDialog.value = false
    fetchCities()
  } catch {
    toast.add({ severity: 'error', summary: t('common.error'), detail: t('teleportCities.failedToSave'), life: 3000 })
  } finally {
    cityDialogLoading.value = false
  }
}

function confirmDeleteCity(city: CityLocation) {
  confirmService.require({
    message: t('teleportCities.confirmDeleteMessage', { name: city.cityName }),
    header: t('common.confirmDelete'),
    icon: 'pi pi-trash',
    acceptClass: 'p-button-danger',
    accept: async () => {
      try {
        await deleteCity(city.id)
        toast.add({ severity: 'success', summary: t('common.success'), detail: t('teleportCities.cityDeleted', { name: city.cityName }), life: 3000 })
        fetchCities()
      } catch {
        toast.add({ severity: 'error', summary: t('common.error'), detail: t('teleportCities.failedToDelete'), life: 3000 })
      }
    },
  })
}

// ─── Teleport ───────────────────────────────────────

function openTeleportDialog(city: CityLocation) {
  teleportCity.value = city
  teleportPlayerId.value = ''
  showTeleportDialog.value = true
}

async function executeTeleport() {
  if (!teleportPlayerId.value || !teleportCity.value) return

  teleportLoading.value = true
  try {
    const player = playersStore.playerList.find((p) => p.playerId === teleportPlayerId.value)
    const result = await teleportToCity(teleportCity.value.id, teleportPlayerId.value, player?.playerName)
    toast.add({ severity: 'success', summary: t('teleportCities.teleported'), detail: result.message, life: 4000 })
    showTeleportDialog.value = false
  } catch (err: any) {
    const msg = err?.response?.data?.message || t('teleportCities.failedToTeleport')
    toast.add({ severity: 'error', summary: t('common.error'), detail: msg, life: 4000 })
  } finally {
    teleportLoading.value = false
  }
}

function navigateTo(tab: string) {
  if (tab === 'homes') router.push({ name: 'TeleportHomes' })
  else if (tab === 'history') router.push({ name: 'TeleportHistory' })
}

const onlinePlayers = () => playersStore.playerList.filter((p) => p.isOnline)

onMounted(fetchCities)
</script>

<template>
  <div class="teleport-view">
    <div class="page-header">
      <h1 class="page-title">{{ t('teleport.title') }}</h1>
    </div>

    <!-- Sub-tab navigation -->
    <div class="sub-tabs">
      <button class="sub-tab sub-tab--active">{{ t('teleport.cities') }}</button>
      <button class="sub-tab" @click="navigateTo('homes')">{{ t('teleport.homes') }}</button>
      <button class="sub-tab" @click="navigateTo('history')">{{ t('teleport.history') }}</button>
    </div>

    <div class="toolbar">
      <Button icon="pi pi-refresh" text severity="secondary" @click="fetchCities" :loading="loading" />
      <div class="toolbar-spacer" />
      <Button
        v-if="canManageTeleport"
        :label="t('teleportCities.addCity')"
        icon="pi pi-plus"
        severity="info"
        size="small"
        @click="openCreateDialog"
      />
    </div>

    <DataTable
      :value="cities"
      :loading="loading"
      stripedRows
      :paginator="true"
      :rows="pageSize"
      :totalRecords="totalCities"
      :lazy="true"
      :rowsPerPageOptions="[25, 50, 100]"
      @page="onPage"
    >
      <Column field="cityName" :header="t('teleportCities.cityName')" />

      <Column field="pointsRequired" :header="t('teleportCities.cost')" style="width: 120px">
        <template #body="{ data }">
          <Tag v-if="data.pointsRequired > 0" :value="`${data.pointsRequired} pts`" severity="warn" />
          <span v-else class="free-text">{{ t('common.free') }}</span>
        </template>
      </Column>

      <Column field="position" :header="t('teleportCities.position')" style="width: 220px">
        <template #body="{ data }">
          <code class="position-text">{{ data.position }}</code>
        </template>
      </Column>

      <Column field="viewDirection" :header="t('teleportCities.direction')" style="width: 180px">
        <template #body="{ data }">
          <code v-if="data.viewDirection" class="position-text">{{ data.viewDirection }}</code>
          <span v-else class="empty-text">—</span>
        </template>
      </Column>

      <Column :header="t('teleportCities.actions')" style="width: 160px">
        <template #body="{ data }">
          <div class="action-buttons">
            <Button
              v-if="canExecuteTeleport"
              icon="pi pi-send"
              text
              severity="info"
              size="small"
              @click="openTeleportDialog(data)"
              :title="t('teleportCities.teleportPlayer')"
            />
            <Button
              v-if="canManageTeleport"
              icon="pi pi-pencil"
              text
              severity="secondary"
              size="small"
              @click="openEditDialog(data)"
              :title="t('common.edit')"
            />
            <Button
              v-if="canManageTeleport"
              icon="pi pi-trash"
              text
              severity="danger"
              size="small"
              @click="confirmDeleteCity(data)"
              :title="t('common.delete')"
            />
          </div>
        </template>
      </Column>

      <template #empty>
        <div class="empty-state">
          <i class="pi pi-compass" style="font-size: 2rem; color: var(--kc-text-secondary)" />
          <p>{{ t('teleportCities.noCitiesYet') }}</p>
          <span class="empty-hint">{{ t('teleportCities.createWaypointsHint') }}</span>
        </div>
      </template>
    </DataTable>

    <!-- City CRUD Dialog -->
    <Dialog
      v-model:visible="showCityDialog"
      :header="cityDialogMode === 'create' ? t('teleportCities.addCityLocation') : t('teleportCities.editCityLocation')"
      :modal="true"
      :style="{ width: '450px' }"
    >
      <div class="form-grid">
        <div class="form-field">
          <label>{{ t('teleportCities.cityNameLabel') }}</label>
          <InputText v-model="cityForm.cityName" :placeholder="t('teleportCities.cityNamePlaceholder')" class="w-full" />
        </div>
        <div class="form-field">
          <label>{{ t('teleportCities.pointsCost') }}</label>
          <InputNumber v-model="cityForm.pointsRequired" :min="0" :max="999999" showButtons class="w-full" />
        </div>
        <div class="form-field">
          <label>{{ t('teleportCities.positionLabel') }}</label>
          <InputText v-model="cityForm.position" :placeholder="t('teleportCities.positionPlaceholder')" class="w-full" />
        </div>
        <div class="form-field">
          <label>{{ t('teleportCities.viewDirection') }}</label>
          <InputText v-model="cityForm.viewDirection" :placeholder="t('teleportCities.viewDirectionPlaceholder')" class="w-full" />
        </div>
      </div>

      <template #footer>
        <Button :label="t('common.cancel')" text severity="secondary" @click="showCityDialog = false" />
        <Button
          :label="cityDialogMode === 'create' ? t('common.create') : t('common.save')"
          severity="info"
          @click="saveCity"
          :loading="cityDialogLoading"
        />
      </template>
    </Dialog>

    <!-- Teleport Player Dialog -->
    <Dialog
      v-model:visible="showTeleportDialog"
      :header="t('teleportCities.teleportPlayerToCity')"
      :modal="true"
      :style="{ width: '400px' }"
    >
      <div class="teleport-form" v-if="teleportCity">
        <p class="teleport-info">
          {{ t('teleportCities.destination') }}: <strong>{{ teleportCity.cityName }}</strong>
          <br />
          {{ t('teleportCities.position') }}: <code>{{ teleportCity.position }}</code>
          <br />
          <span v-if="teleportCity.pointsRequired > 0">
            {{ t('teleportCities.cost') }}: <Tag :value="`${teleportCity.pointsRequired} pts`" severity="warn" />
          </span>
          <span v-else>{{ t('teleportCities.cost') }}: <span class="free-text">{{ t('common.free') }}</span></span>
        </p>

        <div class="form-field">
          <label>{{ t('teleportCities.selectPlayerOnline') }}</label>
          <Select
            v-model="teleportPlayerId"
            :options="onlinePlayers()"
            optionLabel="playerName"
            optionValue="playerId"
            :placeholder="t('teleportCities.choosePlayer')"
            class="w-full"
            filter
          />
        </div>
      </div>

      <template #footer>
        <Button :label="t('common.cancel')" text severity="secondary" @click="showTeleportDialog = false" />
        <Button
          :label="t('teleport.title')"
          icon="pi pi-send"
          severity="info"
          @click="executeTeleport"
          :loading="teleportLoading"
          :disabled="!teleportPlayerId"
        />
      </template>
    </Dialog>
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
.toolbar-spacer { flex: 1; }

.action-buttons { display: flex; gap: 0.25rem; }
.position-text { font-size: 0.85rem; color: var(--kc-cyan); background: rgba(0, 212, 255, 0.08); padding: 0.15rem 0.5rem; border-radius: 4px; }
.free-text { font-size: 0.85rem; color: var(--kc-text-secondary); }
.empty-text { color: var(--kc-text-secondary); }
.empty-state { display: flex; flex-direction: column; align-items: center; gap: 0.5rem; padding: 2rem; color: var(--kc-text-secondary); }
.empty-hint { font-size: 0.85rem; color: var(--kc-text-secondary); }

.form-grid { display: flex; flex-direction: column; gap: 1rem; }
.form-field { display: flex; flex-direction: column; gap: 0.25rem; }
.form-field label { font-size: 0.85rem; color: var(--kc-text-secondary); }
.form-field small { opacity: 0.7; }
.w-full { width: 100%; }

.teleport-form { display: flex; flex-direction: column; gap: 1rem; }
.teleport-info { margin: 0; line-height: 1.8; }

@media (max-width: 768px) {
  .toolbar { flex-wrap: wrap; width: 100%; }
}

@media (max-width: 640px) {
  .sub-tabs { overflow-x: auto; white-space: nowrap; }
}
</style>
