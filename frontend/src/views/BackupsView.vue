<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useToast } from 'primevue/usetoast'
import {
  getBackups, createBackup, restoreBackup, deleteBackup,
  getBackupSettings, saveBackupSettings,
  type BackupRecord, type BackupSettings,
} from '@/api/backups'
import Button from 'primevue/button'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'
import Dialog from 'primevue/dialog'
import InputNumber from 'primevue/inputnumber'
import InputText from 'primevue/inputtext'
import ToggleSwitch from 'primevue/toggleswitch'
import Card from 'primevue/card'

const { t } = useI18n()
const toast = useToast()

const loading = ref(true)
const backups = ref<BackupRecord[]>([])
const settings = ref<BackupSettings>({ enabled: false, intervalMinutes: 60, maxBackups: 10, backupPath: 'KitsuneBackups' })
const creating = ref(false)
const savingSettings = ref(false)
const restoreDialogVisible = ref(false)
const deleteDialogVisible = ref(false)
const selectedBackup = ref<BackupRecord | null>(null)

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  if (bytes < 1024 * 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
  return `${(bytes / (1024 * 1024 * 1024)).toFixed(2)} GB`
}

function formatDate(iso: string): string {
  try {
    return new Date(iso).toLocaleString()
  } catch {
    return iso
  }
}

async function loadData() {
  loading.value = true
  try {
    const [backupList, backupSettings] = await Promise.all([getBackups(), getBackupSettings()])
    backups.value = backupList ?? []
    if (backupSettings) settings.value = backupSettings
  } catch {
    toast.add({ severity: 'error', summary: t('common.error'), detail: t('backups.failedToLoad'), life: 4000 })
  } finally {
    loading.value = false
  }
}

async function handleCreateBackup() {
  creating.value = true
  try {
    await createBackup()
    toast.add({ severity: 'success', summary: t('common.success'), detail: t('backups.created'), life: 4000 })
    await loadData()
  } catch {
    toast.add({ severity: 'error', summary: t('common.error'), detail: t('backups.failedToCreate'), life: 4000 })
  } finally {
    creating.value = false
  }
}

function showRestoreDialog(backup: BackupRecord) {
  selectedBackup.value = backup
  restoreDialogVisible.value = true
}

async function handleRestore() {
  if (!selectedBackup.value) return
  restoreDialogVisible.value = false
  try {
    await restoreBackup(selectedBackup.value.id)
    toast.add({ severity: 'success', summary: t('common.success'), detail: t('backups.restored'), life: 6000 })
    await loadData()
  } catch {
    toast.add({ severity: 'error', summary: t('common.error'), detail: t('backups.failedToRestore'), life: 4000 })
  }
}

function showDeleteDialog(backup: BackupRecord) {
  selectedBackup.value = backup
  deleteDialogVisible.value = true
}

async function handleDelete() {
  if (!selectedBackup.value) return
  deleteDialogVisible.value = false
  try {
    await deleteBackup(selectedBackup.value.id)
    toast.add({ severity: 'success', summary: t('common.success'), detail: t('backups.deleted'), life: 3000 })
    await loadData()
  } catch {
    toast.add({ severity: 'error', summary: t('common.error'), detail: t('backups.failedToDelete'), life: 4000 })
  }
}

async function handleSaveSettings() {
  savingSettings.value = true
  try {
    await saveBackupSettings(settings.value)
    toast.add({ severity: 'success', summary: t('common.success'), detail: t('backups.settingsSaved'), life: 3000 })
  } catch {
    toast.add({ severity: 'error', summary: t('common.error'), detail: t('backups.failedToSaveSettings'), life: 4000 })
  } finally {
    savingSettings.value = false
  }
}

onMounted(loadData)
</script>

<template>
  <div class="backups-view">
    <div class="page-header">
      <h1 class="page-title">{{ t('backups.title') }}</h1>
      <Button
        :label="t('backups.createNow')"
        icon="pi pi-download"
        severity="info"
        :loading="creating"
        :disabled="creating"
        @click="handleCreateBackup"
      />
    </div>

    <!-- Schedule Settings -->
    <Card class="settings-card">
      <template #title>{{ t('backups.schedule') }}</template>
      <template #content>
        <div class="settings-grid">
          <div class="setting-item">
            <label>{{ t('backups.autoBackup') }}</label>
            <ToggleSwitch v-model="settings.enabled" />
          </div>
          <div class="setting-item">
            <label>{{ t('backups.interval') }}</label>
            <InputNumber v-model="settings.intervalMinutes" :min="5" :max="1440" suffix=" min" />
          </div>
          <div class="setting-item">
            <label>{{ t('backups.maxBackups') }}</label>
            <InputNumber v-model="settings.maxBackups" :min="1" :max="100" />
          </div>
          <div class="setting-item">
            <label>{{ t('backups.backupPath') }}</label>
            <InputText v-model="settings.backupPath" />
          </div>
        </div>
        <Button
          :label="t('common.save')"
          icon="pi pi-save"
          severity="info"
          size="small"
          :loading="savingSettings"
          @click="handleSaveSettings"
          class="save-settings-btn"
        />
      </template>
    </Card>

    <!-- Backups list -->
    <DataTable
      :value="backups"
      :loading="loading"
      stripedRows
      :paginator="backups.length > 15"
      :rows="15"
    >
      <Column field="filename" :header="t('backups.filename')" sortable />
      <Column field="worldName" :header="t('backups.world')" sortable style="width: 130px" />
      <Column field="sizeBytes" :header="t('backups.size')" sortable style="width: 100px">
        <template #body="{ data }">{{ formatSize(data.sizeBytes) }}</template>
      </Column>
      <Column field="createdAt" :header="t('backups.date')" sortable style="width: 180px">
        <template #body="{ data }">{{ formatDate(data.createdAt) }}</template>
      </Column>
      <Column field="backupType" :header="t('backups.type')" style="width: 110px">
        <template #body="{ data }">
          <Tag
            :value="data.backupType"
            :severity="data.backupType === 'manual' ? 'info' : data.backupType === 'scheduled' ? 'success' : 'warn'"
          />
        </template>
      </Column>
      <Column :header="t('common.actions')" style="width: 120px">
        <template #body="{ data }">
          <div class="action-buttons">
            <Button
              icon="pi pi-replay"
              text rounded size="small"
              severity="warn"
              @click="showRestoreDialog(data)"
              :title="t('backups.restore')"
            />
            <Button
              icon="pi pi-trash"
              text rounded size="small"
              severity="danger"
              @click="showDeleteDialog(data)"
              :title="t('common.delete')"
            />
          </div>
        </template>
      </Column>
      <template #empty>
        <div class="empty-state">
          <i class="pi pi-database" style="font-size: 2rem; color: var(--kc-text-secondary)" />
          <p>{{ t('backups.noBackups') }}</p>
        </div>
      </template>
    </DataTable>

    <!-- Restore confirmation -->
    <Dialog v-model:visible="restoreDialogVisible" :header="t('backups.confirmRestore')" modal :style="{ width: '460px' }">
      <div class="restore-warning">
        <i class="pi pi-exclamation-triangle" style="font-size: 2rem; color: #f59e0b" />
        <p>{{ t('backups.restoreWarning') }}</p>
        <p class="restore-filename">{{ selectedBackup?.filename }}</p>
      </div>
      <template #footer>
        <Button :label="t('common.cancel')" severity="secondary" text @click="restoreDialogVisible = false" />
        <Button :label="t('backups.restore')" severity="warn" @click="handleRestore" />
      </template>
    </Dialog>

    <!-- Delete confirmation -->
    <Dialog v-model:visible="deleteDialogVisible" :header="t('common.confirmDelete')" modal :style="{ width: '400px' }">
      <p>{{ t('backups.deleteConfirm', { name: selectedBackup?.filename }) }}</p>
      <template #footer>
        <Button :label="t('common.cancel')" severity="secondary" text @click="deleteDialogVisible = false" />
        <Button :label="t('common.delete')" severity="danger" @click="handleDelete" />
      </template>
    </Dialog>
  </div>
</template>

<style scoped>
.backups-view {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.page-title {
  font-size: 1.5rem;
  font-weight: 600;
}

.settings-card {
  background: var(--kc-bg-secondary);
  border: 1px solid var(--kc-border);
}

.settings-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 1rem;
  margin-bottom: 1rem;
}

.setting-item {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.setting-item label {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--kc-text-secondary);
}

.save-settings-btn {
  margin-top: 0.5rem;
}

.action-buttons {
  display: flex;
  gap: 0.25rem;
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  padding: 2rem;
  color: var(--kc-text-secondary);
}

.restore-warning {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
  text-align: center;
}

.restore-filename {
  font-family: monospace;
  font-size: 0.85rem;
  color: var(--kc-text-secondary);
}

@media (max-width: 768px) {
  .settings-grid { grid-template-columns: 1fr; }
}
</style>
