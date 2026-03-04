<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { usePermissions } from '@/composables/usePermissions'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import {
  getSchedules,
  getScheduleDetail,
  createSchedule,
  updateSchedule,
  deleteSchedule,
  runScheduleNow,
  toggleSchedule,
} from '@/api/schedules'
import { getCommandDefinitions } from '@/api/store'
import type { TaskScheduleDetail, CommandDefinition } from '@/types'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import InputNumber from 'primevue/inputnumber'
import Textarea from 'primevue/textarea'
import Dialog from 'primevue/dialog'
import Tag from 'primevue/tag'
import MultiSelect from 'primevue/multiselect'

const { t } = useI18n()
const toast = useToast()
const confirmService = useConfirm()
const { canManageSchedules } = usePermissions()

// State
const loading = ref(true)
const schedules = ref<TaskScheduleDetail[]>([])
const totalSchedules = ref(0)
const pageIndex = ref(0)
const pageSize = ref(50)
const searchFilter = ref('')
let searchTimeout: ReturnType<typeof setTimeout> | null = null

// Definitions for linking
const cmdDefs = ref<CommandDefinition[]>([])

// CRUD dialog
const showDialog = ref(false)
const dialogMode = ref<'create' | 'edit'>('create')
const dialogForm = ref({
  name: '',
  description: '',
  intervalMinutes: 60,
  isEnabled: 1,
  commandIds: [] as number[],
})
const editingId = ref<number | null>(null)
const dialogLoading = ref(false)

// Run now dialog
const showRunDialog = ref(false)
const runResults = ref<string[]>([])
const runMessage = ref('')

// Interval presets
const intervalPresets = [
  { label: t('schedules.preset1min'), value: 1 },
  { label: t('schedules.preset5min'), value: 5 },
  { label: t('schedules.preset10min'), value: 10 },
  { label: t('schedules.preset15min'), value: 15 },
  { label: t('schedules.preset30min'), value: 30 },
  { label: t('schedules.preset1hour'), value: 60 },
  { label: t('schedules.preset2hours'), value: 120 },
  { label: t('schedules.preset4hours'), value: 240 },
  { label: t('schedules.preset6hours'), value: 360 },
  { label: t('schedules.preset12hours'), value: 720 },
  { label: t('schedules.preset24hours'), value: 1440 },
]

async function fetchSchedules() {
  loading.value = true
  try {
    const result = await getSchedules({
      pageIndex: pageIndex.value,
      pageSize: pageSize.value,
      search: searchFilter.value || undefined,
    })
    schedules.value = result.items
    totalSchedules.value = result.total
  } catch {
    toast.add({ severity: 'error', summary: t('common.error'), detail: t('schedules.failedToLoad'), life: 3000 })
  } finally {
    loading.value = false
  }
}

async function fetchDefinitions() {
  try {
    cmdDefs.value = await getCommandDefinitions()
  } catch {
    // non-critical
  }
}

function onPage(event: { first: number; rows: number }) {
  pageIndex.value = Math.floor(event.first / event.rows)
  pageSize.value = event.rows
  fetchSchedules()
}

function onSearch() {
  if (searchTimeout) clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => {
    pageIndex.value = 0
    fetchSchedules()
  }, 400)
}

// ─── CRUD ──────────────────────────────────────────

function openCreateDialog() {
  dialogMode.value = 'create'
  dialogForm.value = { name: '', description: '', intervalMinutes: 60, isEnabled: 1, commandIds: [] }
  editingId.value = null
  showDialog.value = true
}

async function openEditDialog(schedule: TaskScheduleDetail) {
  dialogMode.value = 'edit'
  editingId.value = schedule.id

  try {
    const detail = await getScheduleDetail(schedule.id)
    dialogForm.value = {
      name: detail.name,
      description: detail.description || '',
      intervalMinutes: detail.intervalMinutes,
      isEnabled: detail.isEnabled,
      commandIds: detail.commands.map((c) => c.id),
    }
    showDialog.value = true
  } catch {
    toast.add({ severity: 'error', summary: t('common.error'), detail: t('schedules.failedToLoadDetail'), life: 3000 })
  }
}

async function saveSchedule() {
  if (!dialogForm.value.name.trim()) {
    toast.add({ severity: 'warn', summary: t('common.validation'), detail: t('schedules.taskNameRequired'), life: 3000 })
    return
  }
  if (dialogForm.value.intervalMinutes < 1) {
    toast.add({ severity: 'warn', summary: t('common.validation'), detail: t('schedules.intervalMinRequired'), life: 3000 })
    return
  }

  dialogLoading.value = true
  try {
    if (dialogMode.value === 'create') {
      await createSchedule({
        name: dialogForm.value.name.trim(),
        description: dialogForm.value.description.trim() || undefined,
        intervalMinutes: dialogForm.value.intervalMinutes,
        commandIds: dialogForm.value.commandIds,
      })
      toast.add({ severity: 'success', summary: t('common.success'), detail: t('schedules.taskCreated', { name: dialogForm.value.name }), life: 3000 })
    } else {
      await updateSchedule(editingId.value!, {
        name: dialogForm.value.name.trim(),
        description: dialogForm.value.description.trim() || undefined,
        intervalMinutes: dialogForm.value.intervalMinutes,
        isEnabled: dialogForm.value.isEnabled,
        commandIds: dialogForm.value.commandIds,
      })
      toast.add({ severity: 'success', summary: t('common.success'), detail: t('schedules.taskUpdated', { name: dialogForm.value.name }), life: 3000 })
    }

    showDialog.value = false
    fetchSchedules()
  } catch (err: any) {
    const msg = err?.response?.data?.message || t('schedules.failedToSave')
    toast.add({ severity: 'error', summary: t('common.error'), detail: msg, life: 3000 })
  } finally {
    dialogLoading.value = false
  }
}

function confirmDelete(schedule: TaskScheduleDetail) {
  confirmService.require({
    message: t('schedules.confirmDeleteMessage', { name: schedule.name }),
    header: t('common.confirmDelete'),
    icon: 'pi pi-trash',
    acceptClass: 'p-button-danger',
    accept: async () => {
      try {
        await deleteSchedule(schedule.id)
        toast.add({ severity: 'success', summary: t('common.success'), detail: t('schedules.taskDeleted', { name: schedule.name }), life: 3000 })
        fetchSchedules()
      } catch {
        toast.add({ severity: 'error', summary: t('common.error'), detail: t('schedules.failedToDelete'), life: 3000 })
      }
    },
  })
}

// ─── Special Actions ────────────────────────────────

async function handleRunNow(schedule: TaskScheduleDetail) {
  try {
    const result = await runScheduleNow(schedule.id)
    runMessage.value = result.message
    runResults.value = result.results
    showRunDialog.value = true
    fetchSchedules() // refresh last_run_at
  } catch (err: any) {
    const msg = err?.response?.data?.message || t('schedules.failedToRun')
    toast.add({ severity: 'error', summary: t('common.error'), detail: msg, life: 4000 })
  }
}

async function handleToggle(schedule: TaskScheduleDetail) {
  try {
    const result = await toggleSchedule(schedule.id)
    toast.add({ severity: 'success', summary: t('schedules.toggled'), detail: result.message, life: 3000 })
    fetchSchedules()
  } catch {
    toast.add({ severity: 'error', summary: t('common.error'), detail: t('schedules.failedToToggle'), life: 3000 })
  }
}

// ─── Helpers ────────────────────────────────────────

function formatInterval(minutes: number): string {
  if (minutes < 60) return `${minutes} min`
  const hours = Math.floor(minutes / 60)
  const remainingMinutes = minutes % 60
  if (remainingMinutes === 0) return hours === 1 ? '1 hour' : `${hours} hours`
  return `${hours}h ${remainingMinutes}m`
}

function formatDate(dateStr: string | null): string {
  if (!dateStr) return '—'
  const d = new Date(dateStr + 'Z')
  return d.toLocaleDateString() + ' ' + d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
}

onMounted(() => {
  fetchSchedules()
  fetchDefinitions()
})
</script>

<template>
  <div class="schedules-view">
    <div class="page-header">
      <h1 class="page-title">{{ t('schedules.title') }}</h1>
    </div>

    <div class="toolbar">
      <span class="p-input-icon-left search-wrapper">
        <i class="pi pi-search" />
        <InputText v-model="searchFilter" :placeholder="t('schedules.searchPlaceholder')" class="search-input" @input="onSearch" />
      </span>
      <Button icon="pi pi-refresh" text severity="secondary" @click="fetchSchedules" :loading="loading" />
      <div class="toolbar-spacer" />
      <Button
        v-if="canManageSchedules"
        :label="t('schedules.addTask')"
        icon="pi pi-plus"
        severity="info"
        size="small"
        @click="openCreateDialog"
      />
    </div>

    <DataTable
      :value="schedules"
      :loading="loading"
      stripedRows
      :paginator="true"
      :rows="pageSize"
      :totalRecords="totalSchedules"
      :lazy="true"
      :rowsPerPageOptions="[25, 50, 100]"
      @page="onPage"
    >
      <Column field="name" :header="t('schedules.name')">
        <template #body="{ data }">
          <span class="task-name">{{ data.name }}</span>
        </template>
      </Column>

      <Column :header="t('schedules.interval')" style="width: 120px">
        <template #body="{ data }">
          <code class="interval-text">{{ formatInterval(data.intervalMinutes) }}</code>
        </template>
      </Column>

      <Column :header="t('schedules.status')" style="width: 110px">
        <template #body="{ data }">
          <Tag :value="data.isEnabled ? t('schedules.enabled') : t('schedules.disabled')" :severity="data.isEnabled ? 'success' : 'secondary'" />
        </template>
      </Column>

      <Column :header="t('schedules.lastRun')" style="width: 180px">
        <template #body="{ data }">
          <span class="date-text">{{ formatDate(data.lastRunAt) }}</span>
        </template>
      </Column>

      <Column :header="t('schedules.nextRun')" style="width: 180px">
        <template #body="{ data }">
          <span class="date-text">{{ data.nextRunAt || '—' }}</span>
        </template>
      </Column>

      <Column field="description" :header="t('schedules.description')">
        <template #body="{ data }">
          <span class="desc-text">{{ data.description || '—' }}</span>
        </template>
      </Column>

      <Column :header="t('schedules.actions')" style="width: 180px">
        <template #body="{ data }">
          <div class="action-buttons">
            <Button
              v-if="canManageSchedules"
              icon="pi pi-play"
              text
              severity="success"
              size="small"
              @click="handleRunNow(data)"
              :title="t('schedules.runNow')"
            />
            <Button
              v-if="canManageSchedules"
              :icon="data.isEnabled ? 'pi pi-pause' : 'pi pi-power-off'"
              text
              :severity="data.isEnabled ? 'warn' : 'info'"
              size="small"
              @click="handleToggle(data)"
              :title="data.isEnabled ? t('schedules.disable') : t('schedules.enable')"
            />
            <Button
              v-if="canManageSchedules"
              icon="pi pi-pencil"
              text
              severity="secondary"
              size="small"
              @click="openEditDialog(data)"
              :title="t('common.edit')"
            />
            <Button
              v-if="canManageSchedules"
              icon="pi pi-trash"
              text
              severity="danger"
              size="small"
              @click="confirmDelete(data)"
              :title="t('common.delete')"
            />
          </div>
        </template>
      </Column>

      <template #empty>
        <div class="empty-state">
          <i class="pi pi-clock" style="font-size: 2rem; color: var(--kc-text-secondary)" />
          <p>{{ t('schedules.noTasksYet') }}</p>
          <span class="empty-hint">{{ t('schedules.createTasksHint') }}</span>
        </div>
      </template>
    </DataTable>

    <!-- Create / Edit Dialog -->
    <Dialog
      v-model:visible="showDialog"
      :header="dialogMode === 'create' ? t('schedules.createScheduledTask') : t('schedules.editScheduledTask')"
      :modal="true"
      :style="{ width: '550px' }"
    >
      <div class="form-grid">
        <div class="form-field">
          <label>{{ t('schedules.taskNameLabel') }}</label>
          <InputText v-model="dialogForm.name" :placeholder="t('schedules.taskNamePlaceholder')" class="w-full" />
        </div>

        <div class="form-field">
          <label>{{ t('schedules.intervalLabel') }}</label>
          <div class="interval-picker">
            <InputNumber v-model="dialogForm.intervalMinutes" :min="1" :max="10080" suffix=" min" showButtons class="interval-input" />
            <div class="interval-presets">
              <Button
                v-for="preset in intervalPresets"
                :key="preset.value"
                :label="preset.label"
                size="small"
                :severity="dialogForm.intervalMinutes === preset.value ? 'info' : 'secondary'"
                :text="dialogForm.intervalMinutes !== preset.value"
                @click="dialogForm.intervalMinutes = preset.value"
              />
            </div>
          </div>
        </div>

        <div class="form-field">
          <label>{{ t('schedules.descriptionLabel') }}</label>
          <Textarea v-model="dialogForm.description" rows="2" class="w-full" :placeholder="t('schedules.descriptionPlaceholder')" />
        </div>

        <div class="form-field">
          <label>{{ t('schedules.linkedCommands') }}</label>
          <MultiSelect
            v-model="dialogForm.commandIds"
            :options="cmdDefs"
            optionLabel="command"
            optionValue="id"
            :placeholder="t('schedules.selectCommands')"
            class="w-full"
            filter
            display="chip"
          />
          <small class="form-hint">{{ t('schedules.commandsHint') }}</small>
        </div>
      </div>

      <template #footer>
        <Button :label="t('common.cancel')" text severity="secondary" @click="showDialog = false" />
        <Button
          :label="dialogMode === 'create' ? t('common.create') : t('common.save')"
          severity="info"
          @click="saveSchedule"
          :loading="dialogLoading"
        />
      </template>
    </Dialog>

    <!-- Run Results Dialog -->
    <Dialog
      v-model:visible="showRunDialog"
      :header="t('schedules.taskExecutionResults')"
      :modal="true"
      :style="{ width: '550px' }"
    >
      <div class="run-results">
        <p class="run-message">{{ runMessage }}</p>
        <div v-if="runResults.length" class="results-list">
          <code v-for="(line, i) in runResults" :key="i" class="result-line">{{ line }}</code>
        </div>
        <p v-else class="empty-text">{{ t('schedules.noCommandOutput') }}</p>
      </div>

      <template #footer>
        <Button :label="t('common.close')" severity="secondary" @click="showRunDialog = false" />
      </template>
    </Dialog>
  </div>
</template>

<style scoped>
.schedules-view { display: flex; flex-direction: column; gap: 1rem; }
.page-header { display: flex; align-items: center; gap: 1rem; }
.page-title { font-size: 1.5rem; font-weight: 600; }

.toolbar { display: flex; align-items: center; gap: 0.5rem; }
.toolbar-spacer { flex: 1; }
.search-wrapper { flex: 0 1 350px; }
.search-input { width: 100%; }

.action-buttons { display: flex; gap: 0.25rem; }
.task-name { font-weight: 500; }
.interval-text { font-size: 0.9rem; color: var(--kc-cyan); background: rgba(0, 212, 255, 0.08); padding: 0.15rem 0.5rem; border-radius: 4px; font-family: monospace; }
.date-text { font-size: 0.85rem; color: var(--kc-text-secondary); }
.desc-text { font-size: 0.85rem; color: var(--kc-text-secondary); }
.empty-text { color: var(--kc-text-secondary); }
.empty-state { display: flex; flex-direction: column; align-items: center; gap: 0.5rem; padding: 2rem; color: var(--kc-text-secondary); }
.empty-hint { font-size: 0.85rem; color: var(--kc-text-secondary); text-align: center; max-width: 400px; }

.form-grid { display: flex; flex-direction: column; gap: 1rem; }
.form-field { display: flex; flex-direction: column; gap: 0.25rem; }
.form-field label { font-size: 0.85rem; color: var(--kc-text-secondary); }
.form-field small { opacity: 0.7; }
.form-hint { font-size: 0.75rem; color: var(--kc-text-secondary); margin-top: 0.25rem; }
.w-full { width: 100%; }

.interval-picker { display: flex; flex-direction: column; gap: 0.5rem; }
.interval-input { width: 100%; }
.interval-presets { display: flex; flex-wrap: wrap; gap: 0.25rem; }

.run-results { display: flex; flex-direction: column; gap: 0.75rem; }
.run-message { margin: 0; font-weight: 500; }
.results-list { display: flex; flex-direction: column; gap: 0.25rem; }
.result-line { display: block; font-size: 0.85rem; padding: 0.5rem 0.75rem; background: rgba(0, 0, 0, 0.2); border-radius: 6px; white-space: pre-wrap; word-break: break-all; }

@media (max-width: 768px) {
  .toolbar { flex-wrap: wrap; width: 100%; }
  .search-wrapper { flex: 1 1 100%; }
}
</style>
